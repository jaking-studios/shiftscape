using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    public enum TouchButton { Jump, Dash, Use, Rewind, Pause, Count }

    /// <summary>
    /// Unified input for keyboard/mouse and touch. Gameplay code only reads GameInput.*, never Input directly.
    /// Touch: floating joystick on the left half, drag anywhere else to look, buttons bottom-right.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameInput : MonoBehaviour
    {
        public static Vector2 Move;
        public static Vector2 Look;          // degrees this frame
        public static bool JumpDown, JumpHeld, DashDown, UseDown, RewindHeld, PauseDown;
        public static bool TouchActive;      // touch controls being shown
        public static bool Enabled = true;   // false while menus are open

        // Touch state exposed to the HUD for drawing
        public static bool StickActive;
        public static Vector2 StickOrigin, StickPos; // screen pixels, GUI space (y down)
        public static readonly bool[] ButtonHeld = new bool[(int)TouchButton.Count];

        int stickId = int.MinValue, lookId = int.MinValue;
        readonly Dictionary<int, int> fingerButton = new Dictionary<int, int>();
        readonly bool[] prevHeld = new bool[(int)TouchButton.Count];
        bool mouseAxesOk = true;
        Vector3 lastMouse;

        struct Pointer { public int id; public Vector2 pos; public Vector2 delta; public TouchPhase phase; }
        readonly List<Pointer> pointers = new List<Pointer>();

        public static bool ShouldUseTouch()
        {
            var d = SaveSystem.Data;
            int mode = d != null ? d.touchMode : 0;
            if (mode == 1) return true;
            if (mode == 2) return false;
            return Application.isMobilePlatform || Input.touchCount > 0 && !Application.isEditor;
        }

        /// <summary>Button rectangles in GUI pixels (y down). Shared by input hit-testing and HUD drawing.</summary>
        public static Rect ButtonRect(TouchButton b)
        {
            float s = UIK.Scale;
            float W = Screen.width, H = Screen.height;
            float big = 120 * s, med = 92 * s, m = 28 * s;
            switch (b)
            {
                case TouchButton.Jump: return new Rect(W - m - big, H - m - big, big, big);
                case TouchButton.Dash: return new Rect(W - m - big - med - 18 * s, H - m - med, med, med);
                case TouchButton.Use: return new Rect(W - m - med + 6 * s, H - m - big - med - 22 * s, med, med);
                case TouchButton.Rewind: return new Rect(W - m - big - med - 26 * s, H - m - big - med + 4 * s, med, med);
                case TouchButton.Pause: return new Rect(W - 16 * s - 64 * s, 16 * s, 64 * s, 64 * s);
            }
            return Rect.zero;
        }

        public static float StickRadius => 90f * UIK.Scale;

        void Update()
        {
            JumpDown = DashDown = UseDown = PauseDown = false;
            JumpHeld = RewindHeld = false;
            Move = Vector2.zero; Look = Vector2.zero;
            TouchActive = ShouldUseTouch();
            var d = SaveSystem.Data;
            float sens = d != null ? d.lookSensitivity : 1f;
            float inv = d != null && d.invertY ? -1f : 1f;

            // ---------- Keyboard + mouse ----------
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) PauseDown = true;
            if (Enabled)
            {
                Vector2 k = Vector2.zero;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) k.y += 1;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) k.y -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k.x += 1;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k.x -= 1;
                Move = Vector2.ClampMagnitude(k, 1f);
                JumpDown = Input.GetKeyDown(KeyCode.Space);
                JumpHeld = Input.GetKey(KeyCode.Space);
                DashDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetMouseButtonDown(1) && Cursor.lockState == CursorLockMode.Locked;
                UseDown = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F);
                RewindHeld = Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.Q);

                if (!TouchActive)
                {
                    Vector2 md = Vector2.zero;
                    if (mouseAxesOk)
                    {
                        try { md = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 2.2f; }
                        catch { mouseAxesOk = false; }
                    }
                    if (!mouseAxesOk)
                    {
                        Vector3 mp = Input.mousePosition;
                        md = (Vector2)(mp - lastMouse) * 0.15f;
                    }
                    bool looking = Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1);
                    if (looking) Look += new Vector2(md.x, md.y * inv) * sens;
                }
            }
            lastMouse = Input.mousePosition;

            // ---------- Touch ----------
            GatherPointers();
            for (int i = 0; i < ButtonHeld.Length; i++) ButtonHeld[i] = false;
            if (!TouchActive)
            {
                StickActive = false; stickId = lookId = int.MinValue; fingerButton.Clear();
                System.Array.Copy(ButtonHeld, prevHeld, prevHeld.Length);
                return;
            }

            foreach (var p in pointers)
            {
                Vector2 gui = new Vector2(p.pos.x, Screen.height - p.pos.y);
                if (p.phase == TouchPhase.Began)
                {
                    int hit = -1;
                    for (int b = 0; b < (int)TouchButton.Count; b++)
                        if (ButtonRect((TouchButton)b).Contains(gui)) { hit = b; break; }
                    if (hit >= 0) fingerButton[p.id] = hit;
                    else if (gui.x < Screen.width * 0.45f && stickId == int.MinValue && Enabled)
                    {
                        stickId = p.id; StickOrigin = gui; StickPos = gui; StickActive = true;
                    }
                    else if (lookId == int.MinValue) lookId = p.id;
                }

                bool ended = p.phase == TouchPhase.Ended || p.phase == TouchPhase.Canceled;
                if (fingerButton.TryGetValue(p.id, out int btn))
                {
                    if (ended) fingerButton.Remove(p.id);
                    else ButtonHeld[btn] = true;
                }
                if (p.id == stickId)
                {
                    if (ended) { stickId = int.MinValue; StickActive = false; }
                    else
                    {
                        StickPos = gui;
                        Vector2 dv = StickPos - StickOrigin;
                        float r = StickRadius;
                        if (dv.magnitude > r) { StickOrigin = StickPos - dv.normalized * r; dv = dv.normalized * r; }
                        if (Enabled) Move = new Vector2(dv.x / r, -dv.y / r);
                    }
                }
                if (p.id == lookId)
                {
                    if (ended) lookId = int.MinValue;
                    else if (Enabled && p.phase != TouchPhase.Began)
                    {
                        float k = 0.22f * sens * 1080f / Mathf.Max(720f, Screen.height);
                        Look += new Vector2(p.delta.x * k, p.delta.y * k * inv);
                    }
                }
            }

            // Drop fingers that disappeared without an Ended phase
            if (stickId != int.MinValue && !pointers.Exists(x => x.id == stickId)) { stickId = int.MinValue; StickActive = false; }
            if (lookId != int.MinValue && !pointers.Exists(x => x.id == lookId)) lookId = int.MinValue;

            if (Enabled)
            {
                if (ButtonHeld[(int)TouchButton.Jump]) { JumpHeld = true; if (!prevHeld[(int)TouchButton.Jump]) JumpDown = true; }
                if (ButtonHeld[(int)TouchButton.Dash] && !prevHeld[(int)TouchButton.Dash]) DashDown = true;
                if (ButtonHeld[(int)TouchButton.Use] && !prevHeld[(int)TouchButton.Use]) UseDown = true;
                if (ButtonHeld[(int)TouchButton.Rewind]) RewindHeld = true;
            }
            if (ButtonHeld[(int)TouchButton.Pause] && !prevHeld[(int)TouchButton.Pause]) PauseDown = true;
            System.Array.Copy(ButtonHeld, prevHeld, prevHeld.Length);
        }

        Vector3 simLast;
        bool simDown;

        void GatherPointers()
        {
            pointers.Clear();
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                pointers.Add(new Pointer { id = t.fingerId, pos = t.position, delta = t.deltaPosition, phase = t.phase });
            }
            // On desktop with touch controls forced on, the mouse acts as a single finger (handy for testing).
            if (Input.touchCount == 0 && TouchActive && !Application.isMobilePlatform)
            {
                Vector3 mp = Input.mousePosition;
                if (Input.GetMouseButtonDown(0)) { simDown = true; pointers.Add(new Pointer { id = -7, pos = mp, delta = Vector2.zero, phase = TouchPhase.Began }); }
                else if (Input.GetMouseButtonUp(0) && simDown) { simDown = false; pointers.Add(new Pointer { id = -7, pos = mp, delta = mp - simLast, phase = TouchPhase.Ended }); }
                else if (simDown) pointers.Add(new Pointer { id = -7, pos = mp, delta = mp - simLast, phase = TouchPhase.Moved });
                simLast = mp;
            }
        }
    }
}
