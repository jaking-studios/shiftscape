using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    /// <summary>
    /// Rigidbody character with arbitrary gravity direction (for gravity flips), coyote time,
    /// jump buffering, dash, optional double jump + glide, moving-platform carry and a 3-second Rewind.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        public static PlayerMotor I { get; private set; }

        public float moveSpeed = 7.2f;
        public float airControl = 0.55f;
        public float jumpSpeed = 9.2f;
        public float gravity = 26f;
        public float maxFall = 30f;
        public float dashSpeed = 17f;
        public float dashTime = 0.17f;

        public Vector3 GravityDir { get; private set; } = Vector3.down;
        public Vector3 Up => -GravityDir;
        public bool Grounded { get; private set; }
        public bool Rewinding { get; private set; }
        public float DashReady01 => dashCooldown <= 0 ? 1f : 1f - Mathf.Clamp01(dashTimer / dashCooldown);
        public int RewindCharges { get; set; }
        public float Grip = 1f;
        public bool Frozen;
        public Transform Visual;

        Rigidbody rb;
        CapsuleCollider col;
        float coyote, jumpBuffer, dashLeft, dashTimer, dashCooldown = 1.2f, flipLock;
        bool usedAirJump, canDoubleJump, canGlide, wasGrounded;
        Vector3 dashDir, facing = Vector3.forward, platformVel;
        Quaternion bodyRot = Quaternion.identity;
        Transform cam;
        float landSoundCooldown;
        public float GlideAmount { get; private set; }

        struct Snap { public Vector3 pos, vel, grav; public Quaternion rot; }
        readonly List<Snap> history = new List<Snap>();
        const int HistoryMax = 150; // 3 s at 50 Hz
        float rewindBudget;

        public const int PlayerLayer = 2; // Ignore Raycast: keeps our own casts simple
        int groundMask;

        void Awake()
        {
            I = this;
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 1f;
            col.height = 1.7f; col.radius = 0.38f; col.center = Vector3.zero;
            var pm = new PhysicsMaterial("player") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
            col.sharedMaterial = pm;
            gameObject.layer = PlayerLayer;
            groundMask = ~(1 << PlayerLayer);
            bodyRot = transform.rotation;
        }

        public void ApplyUpgrades()
        {
            canDoubleJump = SaveSystem.UpgradeLevel(Catalog.DoubleJump) > 0;
            canGlide = SaveSystem.UpgradeLevel(Catalog.Glider) > 0;
            dashCooldown = 1.2f - 0.22f * SaveSystem.UpgradeLevel(Catalog.DashCooldown);
        }

        public int MaxRewindCharges => 2 + SaveSystem.UpgradeLevel(Catalog.RewindCharges);

        public void SetCamera(Transform c) { cam = c; }

        public void Teleport(Vector3 pos, Vector3 forward)
        {
            GravityDir = Vector3.down;
            rb.position = pos; transform.position = pos;
            rb.linearVelocity = Vector3.zero;
            facing = forward;
            bodyRot = Quaternion.LookRotation(forward, Vector3.up);
            rb.rotation = bodyRot; transform.rotation = bodyRot;
            history.Clear();
            dashLeft = 0; Rewinding = false; platformVel = Vector3.zero;
            if (CameraRig.I != null) CameraRig.I.Snap(forward);
        }

        public void SetGravity(Vector3 dir)
        {
            if (flipLock > 0 || Vector3.Dot(dir.normalized, GravityDir) > 0.99f) return;
            GravityDir = dir.normalized;
            flipLock = 0.35f;
            usedAirJump = false;
            Grounded = false;
            // keep horizontal momentum, kill vertical so the flip feels snappy
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = Vector3.ProjectOnPlane(v, GravityDir) + GravityDir * 2f;
            GameAudio.Play(Sfx.Flip);
            GameManager.I?.OnFlip();
        }

        public void Bounce(float speed)
        {
            Vector3 v = Vector3.ProjectOnPlane(rb.linearVelocity, Up);
            rb.linearVelocity = v + Up * speed;
            Grounded = false; coyote = 0; usedAirJump = false;
            GameAudio.Play(Sfx.Bounce);
        }

        public Vector3 Velocity => rb.linearVelocity;

        void Update()
        {
            if (Frozen) return;
            if (GameInput.JumpDown) jumpBuffer = 0.14f;
            if (GameInput.DashDown && dashTimer <= 0 && dashLeft <= 0 && !Rewinding)
            {
                Vector3 d = WishDir();
                if (d.sqrMagnitude < 0.01f) d = facing;
                dashDir = d.normalized;
                dashLeft = dashTime;
                dashTimer = dashCooldown;
                GameAudio.Play(Sfx.Dash);
                FxBurst.Spawn(transform.position, new Color(1, 1, 1, 0.8f), 8);
            }
        }

        Vector3 WishDir()
        {
            Vector2 m = GameInput.Move;
            if (m.sqrMagnitude < 0.0004f) return Vector3.zero;
            Vector3 f = cam != null ? Vector3.ProjectOnPlane(cam.forward, Up) : Vector3.forward;
            if (f.sqrMagnitude < 0.001f) f = cam != null ? Vector3.ProjectOnPlane(cam.up, Up) : Vector3.forward;
            f.Normalize();
            Vector3 r = Vector3.Cross(Up, f);
            return (f * m.y + r * m.x);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (Frozen) { rb.linearVelocity = Vector3.zero; return; }
            flipLock -= dt; dashTimer -= dt; jumpBuffer -= dt; landSoundCooldown -= dt;

            // ---------- Rewind ----------
            if (GameInput.RewindHeld && (Rewinding || RewindCharges > 0) && history.Count > 2)
            {
                if (!Rewinding)
                {
                    Rewinding = true; RewindCharges--; rewindBudget = 3f;
                    GameAudio.Play(Sfx.Rewind);
                    GameManager.I?.OnRewind();
                }
                rewindBudget -= dt;
                Snap s = default;
                for (int k = 0; k < 2 && history.Count > 0; k++) { s = history[history.Count - 1]; history.RemoveAt(history.Count - 1); }
                rb.position = s.pos; rb.rotation = s.rot; bodyRot = s.rot; GravityDir = s.grav;
                rb.linearVelocity = Vector3.zero;
                if (rewindBudget <= 0 || history.Count <= 1) EndRewind(s);
                return;
            }
            if (Rewinding) EndRewind(history.Count > 0 ? history[history.Count - 1] : new Snap { pos = rb.position, vel = Vector3.zero, grav = GravityDir, rot = rb.rotation });

            // ---------- Ground ----------
            Vector3 up = Up;
            platformVel = Vector3.zero;
            float castDist = col.height * 0.5f - col.radius + 0.12f;
            Grounded = false;
            if (Physics.SphereCast(rb.position + up * 0.05f, col.radius * 0.92f, -up, out RaycastHit hit, castDist + 0.05f, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Dot(hit.normal, up) > 0.55f && Vector3.Dot(rb.linearVelocity, up) < 3.5f)
                {
                    Grounded = true;
                    var mp = hit.collider.GetComponentInParent<MovingPlatform>();
                    if (mp != null) platformVel = mp.Velocity;
                    var cr = hit.collider.GetComponent<CrumbleTile>();
                    if (cr != null) cr.Touch();
                }
            }
            if (Grounded) { coyote = 0.12f; usedAirJump = false; } else coyote -= dt;
            if (Grounded && !wasGrounded && landSoundCooldown <= 0) { GameAudio.Play(Sfx.Land, 0.6f); landSoundCooldown = 0.2f; }
            wasGrounded = Grounded;

            // ---------- Move ----------
            Vector3 v = rb.linearVelocity;
            Vector3 vUp = Vector3.Project(v, up);
            Vector3 vFlat = v - vUp;
            Vector3 wish = WishDir() * moveSpeed;

            if (dashLeft > 0)
            {
                dashLeft -= dt;
                vFlat = dashDir * dashSpeed;
                vUp = Vector3.zero;
            }
            else
            {
                Vector3 rel = vFlat - (Grounded ? Vector3.ProjectOnPlane(platformVel, up) : Vector3.zero);
                float accel = (Grounded ? 60f * Grip : 22f * airControl);
                if (Grounded && wish.sqrMagnitude < 0.01f) accel = 45f * Grip;
                rel = Vector3.MoveTowards(rel, wish, accel * dt);
                vFlat = rel + (Grounded ? Vector3.ProjectOnPlane(platformVel, up) : Vector3.zero);
            }

            // ---------- Jump ----------
            if (jumpBuffer > 0)
            {
                if (coyote > 0)
                {
                    vUp = up * jumpSpeed + Vector3.Project(platformVel, up);
                    jumpBuffer = 0; coyote = 0; Grounded = false;
                    GameAudio.Play(Sfx.Jump);
                }
                else if (canDoubleJump && !usedAirJump)
                {
                    usedAirJump = true;
                    vUp = up * jumpSpeed * 0.92f;
                    jumpBuffer = 0;
                    GameAudio.Play(Sfx.DoubleJump);
                    FxBurst.Spawn(rb.position - up * 0.8f, new Color(0.5f, 0.9f, 1f, 0.9f), 10);
                }
            }

            // ---------- Gravity ----------
            float fallSpeed = -Vector3.Dot(vUp, up);
            GlideAmount = 0f;
            if (dashLeft <= 0)
            {
                if (!Grounded) vUp += GravityDir * gravity * dt * (fallSpeed > 0 ? 1.15f : (GameInput.JumpHeld ? 1f : 1.8f));
                else if (Vector3.Dot(vUp, up) <= 0) vUp = GravityDir * 1.5f; // stick to ground / slopes
                fallSpeed = -Vector3.Dot(vUp, up);
                float cap = maxFall;
                if (canGlide && !Grounded && GameInput.JumpHeld && fallSpeed > 0) { cap = 2.6f; GlideAmount = 1f; }
                if (fallSpeed > cap) vUp = GravityDir * cap;
            }
            rb.linearVelocity = vFlat + vUp;

            // ---------- Facing ----------
            Vector3 flatMove = Vector3.ProjectOnPlane(wish, up);
            if (flatMove.sqrMagnitude > 0.05f) facing = flatMove.normalized;
            else facing = Vector3.ProjectOnPlane(facing, up).sqrMagnitude > 0.01f ? Vector3.ProjectOnPlane(facing, up).normalized : Vector3.ProjectOnPlane(transform.forward, up).normalized;
            if (facing.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(facing, up);
                bodyRot = Quaternion.RotateTowards(bodyRot, target, 720f * dt);
                rb.MoveRotation(bodyRot);
            }

            // ---------- History ----------
            history.Add(new Snap { pos = rb.position, vel = rb.linearVelocity, grav = GravityDir, rot = bodyRot });
            if (history.Count > HistoryMax) history.RemoveAt(0);
        }

        void EndRewind(Snap s)
        {
            Rewinding = false;
            rb.linearVelocity = s.vel * 0.5f;
            dashLeft = 0;
        }

        public void ClearHistory() { history.Clear(); }
    }
}
