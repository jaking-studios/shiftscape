using UnityEngine;

namespace Shiftscape
{
    /// <summary>Builds the blocky astronaut-ish avatar from primitives and animates it procedurally
    /// (run bob, lean, squash on land, glide cape). Applies the equipped suit, hat and trail.</summary>
    public class PlayerVisual : MonoBehaviour
    {
        PlayerMotor motor;
        Transform root, body, head, visor, legL, legR, armL, armR, cape, hat;
        TrailRenderer trail;
        float bob, squash, lastGroundedY;
        bool wasGrounded;
        Vector3 baseScale = Vector3.one;

        public static PlayerVisual Build(PlayerMotor m)
        {
            var g = new GameObject("Visual");
            g.transform.SetParent(m.transform, false);
            var pv = g.AddComponent<PlayerVisual>();
            pv.motor = m;
            m.Visual = g.transform;
            pv.Construct();
            return pv;
        }

        void Construct()
        {
            var save = SaveSystem.Data;
            var suit = Catalog.Suit(save.suit);
            var mBody = Mat.Lit(suit.body, 0.45f);
            var mVisor = Mat.Glow(suit.visor);
            var mDark = Mat.Lit(Color.Lerp(suit.body, Color.black, 0.45f), 0.3f);

            root = new GameObject("Root").transform; root.SetParent(transform, false);
            body = Mat.Box(root, new Vector3(0, -0.15f, 0), new Vector3(0.62f, 0.7f, 0.42f), mBody, false, "Body").transform;
            Mat.Box(body, new Vector3(0, 0.05f, 0.22f), new Vector3(0.4f, 0.3f, 0.06f), mDark, false, "Chest");
            head = Mat.Box(root, new Vector3(0, 0.5f, 0), new Vector3(0.52f, 0.5f, 0.52f), mBody, false, "Head").transform;
            visor = Mat.Box(head, new Vector3(0, 0.03f, 0.24f), new Vector3(0.38f, 0.22f, 0.08f), mVisor, false, "Visor").transform;
            Mat.Box(root, new Vector3(0, -0.15f, -0.26f), new Vector3(0.4f, 0.5f, 0.14f), mDark, false, "Pack");
            legL = Mat.Box(root, new Vector3(-0.17f, -0.68f, 0), new Vector3(0.24f, 0.4f, 0.28f), mDark, false, "LegL").transform;
            legR = Mat.Box(root, new Vector3(0.17f, -0.68f, 0), new Vector3(0.24f, 0.4f, 0.28f), mDark, false, "LegR").transform;
            armL = Mat.Box(root, new Vector3(-0.42f, -0.12f, 0), new Vector3(0.18f, 0.56f, 0.2f), mBody, false, "ArmL").transform;
            armR = Mat.Box(root, new Vector3(0.42f, -0.12f, 0), new Vector3(0.18f, 0.56f, 0.2f), mBody, false, "ArmR").transform;

            if (SaveSystem.UpgradeLevel(Catalog.Glider) > 0)
            {
                cape = Mat.Box(root, new Vector3(0, -0.05f, -0.35f), new Vector3(0.7f, 0.9f, 0.04f), Mat.Lit(Color.Lerp(suit.visor, Color.white, 0.2f), 0.2f), false, "Cape").transform;
                cape.localScale = new Vector3(0.7f, 0.9f, 0.04f);
            }

            BuildHat(save.hat, suit);
            BuildTrail(save.trail);
        }

        void BuildHat(string id, SuitDef suit)
        {
            var hatGo = new GameObject("Hat");
            hat = hatGo.transform; hat.SetParent(head, false);
            var gold = Mat.Lit(Mat.Hex("#f4c542"), 0.7f, 0.6f);
            var black = Mat.Lit(Mat.Hex("#1a1a22"), 0.5f);
            switch (id)
            {
                case "antenna":
                    Mat.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.42f, 0), new Vector3(0.05f, 0.18f, 0.05f), black);
                    var bulb = Mat.Prim(PrimitiveType.Sphere, hat, new Vector3(0, 0.62f, 0), Vector3.one * 0.14f, Mat.GlowInstance(suit.visor));
                    bulb.AddComponent<Pulse>();
                    break;
                case "tophat":
                    Mat.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.28f, 0), new Vector3(0.7f, 0.03f, 0.7f), black);
                    Mat.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.5f, 0), new Vector3(0.44f, 0.22f, 0.44f), black);
                    Mat.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.32f, 0), new Vector3(0.46f, 0.03f, 0.46f), Mat.Lit(Mat.Hex("#c0392b"), 0.5f));
                    break;
                case "crown":
                    Mat.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.33f, 0), new Vector3(0.46f, 0.08f, 0.46f), gold);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Mathf.PI * 2;
                        Mat.Box(hat, new Vector3(Mathf.Cos(a) * 0.2f, 0.46f, Mathf.Sin(a) * 0.2f), new Vector3(0.08f, 0.16f, 0.08f), gold, false);
                    }
                    break;
                case "halo":
                    var halo = Mat.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.55f, 0), new Vector3(0.55f, 0.02f, 0.55f), Mat.GlowInstance(new Color(1f, 0.95f, 0.5f, 0.9f)));
                    halo.AddComponent<Pulse>();
                    Mat.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.55f, 0), new Vector3(0.42f, 0.03f, 0.42f), Mat.Lit(new Color(0.3f, 0.3f, 0.3f), 0.1f));
                    break;
                case "horns":
                    var red = Mat.Lit(Mat.Hex("#d6452b"), 0.5f);
                    Mat.Prim(PrimitiveType.Capsule, hat, new Vector3(-0.2f, 0.4f, 0), new Vector3(0.12f, 0.16f, 0.12f), red).transform.localRotation = Quaternion.Euler(0, 0, 25);
                    Mat.Prim(PrimitiveType.Capsule, hat, new Vector3(0.2f, 0.4f, 0), new Vector3(0.12f, 0.16f, 0.12f), red).transform.localRotation = Quaternion.Euler(0, 0, -25);
                    break;
            }
        }

        void BuildTrail(string id)
        {
            if (id == "none") return;
            var t = Catalog.Trail(id);
            var g = new GameObject("Trail");
            g.transform.SetParent(root, false);
            g.transform.localPosition = new Vector3(0, -0.3f, -0.2f);
            trail = g.AddComponent<TrailRenderer>();
            trail.time = 0.45f;
            trail.startWidth = 0.35f; trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.sharedMaterial = Mat.Glow(Color.white);
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(t.a, 0), new GradientColorKey(t.b, 1) },
                         new[] { new GradientAlphaKey(0.8f, 0), new GradientAlphaKey(0f, 1) });
            trail.colorGradient = grad;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void ResetTrail() { if (trail != null) trail.Clear(); }

        void Update()
        {
            if (motor == null) return;
            float dt = Time.deltaTime;
            Vector3 up = motor.Up;
            Vector3 vFlat = Vector3.ProjectOnPlane(motor.Velocity, up);
            float speed = vFlat.magnitude;
            bool grounded = motor.Grounded;

            // Landing squash
            if (grounded && !wasGrounded) squash = 1f;
            wasGrounded = grounded;
            squash = Mathf.MoveTowards(squash, 0, dt * 6f);
            float sy = 1f - 0.25f * squash;
            float sx = 1f + 0.18f * squash;
            if (!grounded) { float stretch = Mathf.Clamp(Mathf.Abs(Vector3.Dot(motor.Velocity, up)) / 20f, 0, 0.25f); sy = 1 + stretch; sx = 1 - stretch * 0.5f; }
            root.localScale = Vector3.Lerp(root.localScale, new Vector3(sx, sy, sx), dt * 14f);

            // Run cycle
            if (grounded && speed > 0.5f) bob += dt * speed * 1.9f;
            float legSwing = grounded ? Mathf.Sin(bob) * Mathf.Clamp01(speed / 6f) * 40f : 12f;
            legL.localRotation = Quaternion.Euler(legSwing, 0, 0);
            legR.localRotation = Quaternion.Euler(-legSwing, 0, 0);
            armL.localRotation = Quaternion.Euler(-legSwing * 0.8f + (grounded ? 0 : -140f), 0, 8);
            armR.localRotation = Quaternion.Euler(legSwing * 0.8f + (grounded ? 0 : -140f), 0, -8);
            root.localPosition = new Vector3(0, grounded ? Mathf.Abs(Mathf.Sin(bob)) * 0.06f * Mathf.Clamp01(speed / 6f) : 0, 0);

            // Lean into movement
            Vector3 localV = motor.transform.InverseTransformDirection(vFlat);
            root.localRotation = Quaternion.Slerp(root.localRotation, Quaternion.Euler(localV.z * 1.6f, 0, -localV.x * 1.6f), dt * 8f);

            if (cape != null)
            {
                float g = motor.GlideAmount;
                cape.localRotation = Quaternion.Slerp(cape.localRotation, Quaternion.Euler(g > 0 ? -80f : (grounded ? 5f : 35f), 0, 0), dt * 6f);
                cape.localScale = Vector3.Lerp(cape.localScale, new Vector3(g > 0 ? 1.6f : 0.7f, 0.9f, 0.04f), dt * 6f);
            }

            if (trail != null) trail.emitting = speed > 4f || !grounded;
        }
    }

    /// <summary>Gentle colour pulse for glow bits.</summary>
    public class Pulse : MonoBehaviour
    {
        Material m; Color baseC; float offset;
        void Start() { m = GetComponent<Renderer>().material; baseC = m.color; offset = Random.value * 10; }
        void Update() { if (m != null) { float k = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f + offset); m.color = new Color(baseC.r * k, baseC.g * k, baseC.b * k, baseC.a); } }
    }
}
