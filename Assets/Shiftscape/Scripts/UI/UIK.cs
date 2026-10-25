using UnityEngine;

namespace Shiftscape
{
    /// <summary>Tiny immediate-mode UI kit: DPI-aware scale, rounded panels, big touch-friendly buttons.</summary>
    public static class UIK
    {
        public static float Scale
        {
            get
            {
                float byH = Screen.height / 720f;
                float dpi = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
                float s = Application.isMobilePlatform ? Mathf.Max(byH, dpi * 0.75f) : byH;
                return Mathf.Clamp(s, 0.6f, 3.5f);
            }
        }

        static Texture2D white, round;
        static GUIStyle label, title, button, small, box, big;
        static Font font;

        public static Texture2D White { get { if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); } return white; } }

        static Texture2D Round
        {
            get
            {
                if (round != null) return round;
                int n = 32; round = new Texture2D(n, n);
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - n / 2f + 0.5f) - (n / 2f - 10)), dy = Mathf.Max(0, Mathf.Abs(y - n / 2f + 0.5f) - (n / 2f - 10));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    round.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(10 - d)));
                }
                round.Apply(); round.filterMode = FilterMode.Bilinear;
                return round;
            }
        }

        public static void Init()
        {
            if (label != null) return;
            font = Mat.Font;
            label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = true, richText = true };
            title = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            big = new GUIStyle(title);
            small = new GUIStyle(label);
            button = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, richText = true };
            button.normal.background = Round; button.hover.background = Round; button.active.background = Round; button.focused.background = Round;
            button.border = new RectOffset(12, 12, 12, 12);
            box = new GUIStyle(GUI.skin.box);
            box.normal.background = Round; box.border = new RectOffset(12, 12, 12, 12);
            if (font != null) { label.font = title.font = big.font = small.font = button.font = font; }
        }

        public static GUIStyle Label(int size, Color c, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            Init();
            label.fontSize = Mathf.RoundToInt(size * Scale); label.normal.textColor = c; label.alignment = anchor; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            return label;
        }
        public static GUIStyle Title(int size, Color c) { Init(); title.fontSize = Mathf.RoundToInt(size * Scale); title.normal.textColor = c; return title; }

        public static void Panel(Rect r, Color c)
        {
            Init();
            var old = GUI.color; GUI.color = c;
            GUI.Box(r, GUIContent.none, box);
            GUI.color = old;
        }

        public static void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, White); GUI.color = old; }

        public static bool Button(Rect r, string text, Color bg, Color fg, int size = 20, bool enabled = true)
        {
            Init();
            button.fontSize = Mathf.RoundToInt(size * Scale);
            button.normal.textColor = button.hover.textColor = button.active.textColor = button.focused.textColor = fg;
            var old = GUI.color; GUI.color = enabled ? bg : new Color(bg.r, bg.g, bg.b, bg.a * 0.45f);
            GUI.enabled = enabled;
            bool b = GUI.Button(r, text, button);
            GUI.enabled = true;
            GUI.color = old;
            if (b) GameAudio.Play(Sfx.Click);
            return b;
        }

        public static void Shadowed(Rect r, string text, GUIStyle s)
        {
            var c = s.normal.textColor;
            s.normal.textColor = new Color(0, 0, 0, 0.6f);
            GUI.Label(new Rect(r.x + 2 * Scale, r.y + 2 * Scale, r.width, r.height), text, s);
            s.normal.textColor = c;
            GUI.Label(r, text, s);
        }

        public static string Time(float t) { int m = (int)(t / 60); return m > 0 ? $"{m}:{t % 60:00.00}" : $"{t:0.00}s"; }

        public static readonly Color Ink = new Color(0.08f, 0.09f, 0.14f, 0.92f);
        public static readonly Color Paper = new Color(1, 1, 1, 1);
        public static readonly Color Accent = Mat.Hex("#2bd9ff");
        public static readonly Color Gold = Mat.Hex("#ffd34d");
        public static readonly Color Danger = Mat.Hex("#ff5e3a");
        public static readonly Color Good = Mat.Hex("#59e3b0");
        public static readonly Color Muted = new Color(1, 1, 1, 0.55f);
    }
}
