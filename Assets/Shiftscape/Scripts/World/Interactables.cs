using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 a, b;
        public float period = 3f, phase;
        public bool rotate;
        public float spin = 60f;
        public Vector3 Velocity { get; private set; }
        Rigidbody rb;
        Vector3 last;

        void Awake()
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            last = transform.position;
        }

        void FixedUpdate()
        {
            float t = Mathf.PingPong((Time.time + phase) / period, 1f);
            t = t * t * (3 - 2 * t);
            Vector3 p = Vector3.Lerp(a, b, t);
            rb.MovePosition(p);
            if (rotate) rb.MoveRotation(Quaternion.Euler(0, Time.time * spin, 0));
            Velocity = (p - last) / Time.fixedDeltaTime;
            last = p;
        }
    }

    public class Spinner : MonoBehaviour
    {
        public float speed = 90f;
        public Vector3 axis = Vector3.up;
        Rigidbody rb;
        void Awake() { rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate; }
        void FixedUpdate() { rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(speed * Time.fixedDeltaTime, axis)); }
    }

    /// <summary>Hurts the player on contact; respawns at checkpoint if hearts remain.</summary>
    public class Hazard : MonoBehaviour
    {
        public bool instantKill;
        void OnTriggerEnter(Collider o) { Hit(o); }
        void OnCollisionEnter(Collision c) { Hit(c.collider); }
        void OnTriggerStay(Collider o) { Hit(o); }
        void Hit(Collider o)
        {
            if (o.GetComponent<PlayerMotor>() != null) GameManager.I?.PlayerHurt(instantKill, transform.position);
        }
    }

    public class CrumbleTile : MonoBehaviour
    {
        public float delay = 0.7f, respawn = 4f;
        float timer = -1; bool falling;
        Vector3 home; Renderer r; Collider c; Color baseC; Material m;
        void Start() { home = transform.position; r = GetComponent<Renderer>(); c = GetComponent<Collider>(); m = r.material; baseC = m.color; }
        public void Touch() { if (timer < 0 && !falling) { timer = delay; GameAudio.Play(Sfx.Crumble, 0.5f); } }
        void Update()
        {
            if (timer >= 0 && !falling)
            {
                timer -= Time.deltaTime;
                transform.position = home + Random.insideUnitSphere * 0.03f;
                m.color = Color.Lerp(baseC, Color.black, 0.5f * (1 - timer / delay));
                if (timer < 0) { falling = true; timer = respawn; c.enabled = false; }
            }
            else if (falling)
            {
                transform.position += Vector3.down * 9f * Time.deltaTime;
                transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, Time.deltaTime * 1.5f);
                timer -= Time.deltaTime;
                if (timer < 0) ResetTile();
            }
        }
        void ResetTile()
        {
            falling = false; timer = -1; transform.position = home; c.enabled = true;
            transform.localScale = originalScale; m.color = baseC;
        }
        Vector3 originalScale;
        void Awake() { originalScale = transform.localScale; }
    }

    public class BouncePad : MonoBehaviour
    {
        public float power = 16f;
        Transform top; float anim;
        void Start() { top = transform.childCount > 0 ? transform.GetChild(0) : null; }
        void OnTriggerEnter(Collider o)
        {
            var pm = o.GetComponent<PlayerMotor>();
            if (pm != null && Vector3.Dot(pm.Velocity, pm.Up) < 2f) { pm.Bounce(power); anim = 1; FxBurst.Spawn(transform.position, Color.white, 6); }
        }
        void Update() { if (top != null) { anim = Mathf.MoveTowards(anim, 0, Time.deltaTime * 4); top.localScale = new Vector3(1, 1 - 0.5f * anim, 1); } }
    }

    public class GravityFlipper : MonoBehaviour
    {
        public Vector3 dir = Vector3.up;
        public bool oneShot;
        bool used;
        void OnTriggerEnter(Collider o)
        {
            if (oneShot && used) return;
            var pm = o.GetComponent<PlayerMotor>();
            if (pm != null) { pm.SetGravity(dir); used = true; FxBurst.Spawn(pm.transform.position, Mat.Hex("#b98bff"), 14); }
        }
    }

    public class Checkpoint : MonoBehaviour
    {
        public int index;
        public Vector3 facing = Vector3.forward;
        public bool done;
        void OnTriggerEnter(Collider o)
        {
            if (done) return;
            if (o.GetComponent<PlayerMotor>() != null) { done = true; GameManager.I?.ReachCheckpoint(this); }
        }
    }

    public class Shard : MonoBehaviour
    {
        public int value = 1;
        float t; bool taken; Vector3 home;
        void Start() { home = transform.localPosition; t = Random.value * 6; }
        void Update()
        {
            t += Time.deltaTime;
            transform.localPosition = home + Vector3.up * Mathf.Sin(t * 2.5f) * 0.12f;
            transform.Rotate(0, 90 * Time.deltaTime, 0, Space.World);
            int mag = SaveSystem.UpgradeLevel(Catalog.Magnet);
            if (mag > 0 && PlayerMotor.I != null && !taken)
            {
                float r = 2.2f + 1.5f * mag;
                Vector3 d = PlayerMotor.I.transform.position - transform.position;
                if (d.sqrMagnitude < r * r) transform.position += d.normalized * Time.deltaTime * 9f;
            }
        }
        void OnTriggerEnter(Collider o)
        {
            if (taken) return;
            if (o.GetComponent<PlayerMotor>() != null)
            {
                taken = true;
                GameManager.I?.CollectShard(value);
                FxBurst.Spawn(transform.position, Mat.Hex("#7cf7ff"), 7);
                Destroy(gameObject);
            }
        }
    }

    public class ExitGate : MonoBehaviour
    {
        public bool open;
        Transform bars; Material m;
        public void Setup(Transform b, Material mat) { bars = b; m = mat; }
        void Update()
        {
            if (bars != null) bars.localScale = Vector3.Lerp(bars.localScale, open ? new Vector3(1, 0.02f, 1) : Vector3.one, Time.deltaTime * 5f);
            if (m != null) m.color = Color.Lerp(m.color, open ? Mat.Hex("#59e3b0") : Mat.Hex("#e84a5f"), Time.deltaTime * 5f);
        }
        void OnTriggerEnter(Collider o)
        {
            if (open && o.GetComponent<PlayerMotor>() != null) GameManager.I?.RoomCleared();
        }
    }

    public class Interactable : MonoBehaviour
    {
        public string prompt = "Use";
        public System.Action onUse;
        public bool enabledForUse = true;
        public float radius = 2.4f;
        public void Use() { if (enabledForUse) onUse?.Invoke(); }
    }

    /// <summary>Simple particle burst using pooled cubes. No ParticleSystem asset needed.</summary>
    public class FxBurst : MonoBehaviour
    {
        static readonly Stack<FxBurst> pool = new Stack<FxBurst>();
        static Transform rootT;
        Vector3[] vel = new Vector3[16]; Transform[] bits = new Transform[16]; int n; float life; Material m;

        public static void Spawn(Vector3 pos, Color c, int count)
        {
            if (SaveSystem.Data != null && !SaveSystem.Data.highQuality) count = Mathf.Max(2, count / 2);
            FxBurst b;
            if (pool.Count > 0) b = pool.Pop();
            else
            {
                if (rootT == null) rootT = new GameObject("FX").transform;
                var g = new GameObject("Burst"); g.transform.SetParent(rootT);
                b = g.AddComponent<FxBurst>();
                b.m = Mat.GlowInstance(Color.white);
                for (int i = 0; i < 16; i++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(q.GetComponent<Collider>());
                    q.transform.SetParent(g.transform, false);
                    q.GetComponent<Renderer>().sharedMaterial = b.m;
                    b.bits[i] = q.transform;
                }
            }
            b.gameObject.SetActive(true);
            b.m.color = c;
            b.n = Mathf.Min(16, count); b.life = 0.55f;
            b.transform.position = pos;
            for (int i = 0; i < 16; i++)
            {
                bool on = i < b.n;
                b.bits[i].gameObject.SetActive(on);
                if (!on) continue;
                b.bits[i].localPosition = Vector3.zero;
                b.bits[i].localScale = Vector3.one * Random.Range(0.08f, 0.2f);
                b.vel[i] = Random.onUnitSphere * Random.Range(2f, 5f) + Vector3.up * 2f;
            }
        }

        void Update()
        {
            life -= Time.deltaTime;
            for (int i = 0; i < n; i++)
            {
                vel[i] += Vector3.down * 9f * Time.deltaTime;
                bits[i].localPosition += vel[i] * Time.deltaTime;
                bits[i].localScale = Vector3.one * Mathf.Max(0, life / 0.55f) * 0.16f;
            }
            if (life <= 0) { gameObject.SetActive(false); pool.Push(this); }
        }
    }

    /// <summary>Slow bob + spin for decorative things.</summary>
    public class Float : MonoBehaviour
    {
        public float amp = 0.3f, speed = 1f, spin = 20f;
        Vector3 home; float t;
        void Start() { home = transform.localPosition; t = Random.value * 10; }
        void Update() { t += Time.deltaTime; transform.localPosition = home + Vector3.up * Mathf.Sin(t * speed) * amp; transform.Rotate(0, spin * Time.deltaTime, 0); }
    }
}
