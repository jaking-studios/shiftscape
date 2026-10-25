using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    /// <summary>Base for puzzles that gate a room exit. Call Solve() to open the gate.</summary>
    public abstract class Puzzle : MonoBehaviour
    {
        public ExitGate gate;
        public bool Solved { get; private set; }
        protected void Solve()
        {
            if (Solved) return;
            Solved = true;
            if (gate != null) gate.open = true;
            GameAudio.Play(Sfx.Unlock);
            GameManager.I?.PuzzleSolved();
            FxBurst.Spawn(transform.position + Vector3.up, Mat.Hex("#59e3b0"), 16);
        }
    }

    /// <summary>Four pads light up in a sequence; step on them in the same order. Grows one step each round.</summary>
    public class MemoryPuzzle : Puzzle
    {
        public List<Transform> pads = new List<Transform>();
        public List<Material> padMats = new List<Material>();
        public Color[] colors = { Mat.Hex("#ff5e3a"), Mat.Hex("#ffd34d"), Mat.Hex("#59e3b0"), Mat.Hex("#6ad1ff") };
        public int rounds = 3;
        List<int> seq = new List<int>();
        int round = 1, inputIdx; float showTimer; int showIdx = -1; bool showing = true; float lockout;
        System.Random rng;
        TextMesh label;
        readonly bool[] standing = new bool[4];

        public void Init(System.Random r, TextMesh lbl) { rng = r; label = lbl; for (int i = 0; i < rounds + 1; i++) seq.Add(rng.Next(4)); BeginShow(); }

        void BeginShow() { showing = true; showIdx = -1; showTimer = 0.6f; inputIdx = 0; if (label) label.text = "WATCH"; }

        void Update()
        {
            if (Solved) return;
            if (showing)
            {
                showTimer -= Time.deltaTime;
                if (showTimer <= 0)
                {
                    showIdx++;
                    if (showIdx >= round + 1) { showing = false; showIdx = -1; if (label) label.text = "REPEAT  " + (round) + "/" + rounds; }
                    else { showTimer = 0.55f; GameAudio.Play(Sfx.Pad0 + seq[showIdx]); }
                }
            }
            lockout -= Time.deltaTime;
            for (int i = 0; i < pads.Count; i++)
            {
                bool lit = showing && showIdx >= 0 && showIdx < seq.Count && seq[showIdx] == i && showTimer > 0.15f;
                Color target = lit || standing[i] ? colors[i] : Color.Lerp(colors[i], Color.black, 0.7f);
                padMats[i].color = Color.Lerp(padMats[i].color, target, Time.deltaTime * 12f);
            }
        }

        public void PadEntered(int i)
        {
            standing[i] = true;
            if (Solved || showing || lockout > 0) return;
            GameAudio.Play(Sfx.Pad0 + i);
            if (seq[inputIdx] == i)
            {
                inputIdx++;
                if (inputIdx >= round + 1)
                {
                    round++;
                    if (round > rounds) { if (label) label.text = "OPEN"; Solve(); }
                    else { lockout = 0.8f; showTimer = 0.9f; showing = true; showIdx = -1; inputIdx = 0; if (label) label.text = "GOOD"; }
                }
            }
            else
            {
                GameAudio.Play(Sfx.Wrong); lockout = 1f; if (label) label.text = "NOPE";
                CameraRig.I?.Shake(0.4f);
                showTimer = 1.0f; showing = true; showIdx = -1; inputIdx = 0;
            }
        }
        public void PadExited(int i) { standing[i] = false; }
    }

    public class PadTrigger : MonoBehaviour
    {
        public MemoryPuzzle owner; public int index;
        void OnTriggerEnter(Collider o) { if (o.GetComponent<PlayerMotor>() != null) owner.PadEntered(index); }
        void OnTriggerExit(Collider o) { if (o.GetComponent<PlayerMotor>() != null) owner.PadExited(index); }
    }

    /// <summary>Find the 3 digits hidden on glowing plaques around the room, then dial them into the vault.</summary>
    public class CodeVaultPuzzle : Puzzle
    {
        public int[] code = new int[3];
        public int[] dials = new int[3];
        public List<TextMesh> dialLabels = new List<TextMesh>();
        public TextMesh status;
        public void Spin(int dial)
        {
            if (Solved) return;
            dials[dial] = (dials[dial] + 1) % 10;
            dialLabels[dial].text = dials[dial].ToString();
            GameAudio.Play(Sfx.Click, 1f, 0.9f + 0.1f * dial);
            bool ok = true; for (int i = 0; i < 3; i++) if (dials[i] != code[i]) ok = false;
            if (ok) { if (status) status.text = "UNLOCKED"; Solve(); }
        }
    }

    /// <summary>Push every crate onto a pressure plate.</summary>
    public class CratePuzzle : Puzzle
    {
        public List<PressurePlate> plates = new List<PressurePlate>();
        public TextMesh status;
        void Update()
        {
            if (Solved) return;
            int n = 0; foreach (var p in plates) if (p.Pressed) n++;
            if (status) status.text = n + " / " + plates.Count;
            if (n >= plates.Count) Solve();
        }
    }

    public class PressurePlate : MonoBehaviour
    {
        public bool Pressed { get; private set; }
        int inside; Material m; Color baseC;
        void Start() { m = GetComponent<Renderer>().material; baseC = m.color; }
        void OnTriggerEnter(Collider o) { if (o.GetComponent<PushCrate>() != null) inside++; }
        void OnTriggerExit(Collider o) { if (o.GetComponent<PushCrate>() != null) inside = Mathf.Max(0, inside - 1); }
        void Update()
        {
            bool p = inside > 0;
            if (p && !Pressed) GameAudio.Play(Sfx.Checkpoint, 0.6f);
            Pressed = p;
            m.color = Color.Lerp(m.color, Pressed ? Mat.Hex("#59e3b0") : baseC, Time.deltaTime * 8f);
        }
    }

    public class PushCrate : MonoBehaviour
    {
        Rigidbody rb; Vector3 home;
        void Awake()
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 2.5f; rb.linearDamping = 2.5f; rb.angularDamping = 10f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            home = transform.position;
        }
        void FixedUpdate()
        {
            // Player pushes by walking into it (collision) — add a little extra shove in their move direction.
            if (PlayerMotor.I != null)
            {
                Vector3 d = transform.position - PlayerMotor.I.transform.position;
                d.y = 0;
                if (d.magnitude < 1.05f && GameInput.Move.sqrMagnitude > 0.1f)
                    rb.AddForce(d.normalized * 18f, ForceMode.Acceleration);
            }
            if (transform.position.y < home.y - 15f) { rb.linearVelocity = Vector3.zero; transform.position = home; } // fell off
        }
    }

    /// <summary>Flip the lever: toggles gravity in the room, or opens the gate — set via onToggle.</summary>
    public class Lever : MonoBehaviour
    {
        public System.Action<bool> onToggle; public bool on; Transform handle;
        public void Setup(Transform h) { handle = h; }
        public void Toggle() { on = !on; GameAudio.Play(Sfx.Click); onToggle?.Invoke(on); }
        void Update() { if (handle) handle.localRotation = Quaternion.Slerp(handle.localRotation, Quaternion.Euler(on ? -40 : 40, 0, 0), Time.deltaTime * 10f); }
    }
}
