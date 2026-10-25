using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    public enum RoomKind
    {
        PlatformHop,
        SpinnerGauntlet,
        CrumbleBridge,
        GravityHall,
        CrateAndPlate,
        MemoryPads,
        CodeVault,
        BounceTowers
    }

    /// <summary>
    /// A world ("Shift") definition. Create more in the editor via
    /// Assets > Create > Shiftscape > World Theme and drop them into a Resources/Worlds folder,
    /// or add them to WorldCatalog.BuiltIn(). No core code changes needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Shiftscape/World Theme", fileName = "WorldTheme")]
    public class WorldTheme : ScriptableObject
    {
        public string id = "world";
        public string displayName = "World";
        [TextArea] public string tagline = "";
        public int starsToUnlock = 0;
        public int roomCount = 5;
        [Range(0, 1)] public float difficulty = 0.2f;

        [Header("Colours")]
        public Color sky = Color.cyan;
        public Color fog = Color.white;
        public Color ground = Color.gray;
        public Color accent = Color.yellow;
        public Color hazard = Color.red;
        public Color trim = Color.white;
        public Color decor = Color.gray;
        public Color voidColor = new Color(0.15f, 0f, 0.3f, 0.85f);

        [Header("Feel")]
        [Tooltip("1 = normal grip, lower = icy")] public float grip = 1f;
        [Tooltip("Metres per second the Void advances")] public float voidSpeed = 1.0f;
        public float fogStart = 30f;
        public float fogEnd = 140f;
        public int musicRoot = 57; // MIDI note

        [Header("Room mix (weights)")]
        public List<RoomKind> rooms = new List<RoomKind>();
        public List<float> weights = new List<float>();

        public RoomKind PickRoom(System.Random rng, RoomKind? avoid)
        {
            float total = 0f;
            for (int i = 0; i < rooms.Count; i++)
                if (avoid == null || rooms[i] != avoid.Value) total += i < weights.Count ? weights[i] : 1f;
            double r = rng.NextDouble() * total;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (avoid != null && rooms[i] == avoid.Value) continue;
                r -= i < weights.Count ? weights[i] : 1f;
                if (r <= 0) return rooms[i];
            }
            return rooms.Count > 0 ? rooms[0] : RoomKind.PlatformHop;
        }
    }

    public static class WorldCatalog
    {
        static List<WorldTheme> worlds;

        public static List<WorldTheme> All
        {
            get
            {
                if (worlds == null)
                {
                    worlds = BuiltIn();
                    // Designer-made extra worlds (optional): Resources/Worlds/*.asset
                    var extra = Resources.LoadAll<WorldTheme>("Worlds");
                    foreach (var w in extra)
                        if (worlds.Find(x => x.id == w.id) == null) worlds.Add(w);
                }
                return worlds;
            }
        }

        public static WorldTheme Get(string id)
        {
            var w = All.Find(x => x.id == id);
            return w != null ? w : All[0];
        }

        static WorldTheme Make(string id, string name, string tag, int stars, int roomCount, float diff,
            string sky, string fog, string ground, string accent, string hazard, string trim, string decor, string voidC,
            float grip, float voidSpeed, int music, params (RoomKind kind, float w)[] mix)
        {
            var t = ScriptableObject.CreateInstance<WorldTheme>();
            t.id = id; t.displayName = name; t.tagline = tag; t.starsToUnlock = stars;
            t.roomCount = roomCount; t.difficulty = diff;
            t.sky = Mat.Hex(sky); t.fog = Mat.Hex(fog); t.ground = Mat.Hex(ground); t.accent = Mat.Hex(accent);
            t.hazard = Mat.Hex(hazard); t.trim = Mat.Hex(trim); t.decor = Mat.Hex(decor);
            var v = Mat.Hex(voidC); v.a = 0.88f; t.voidColor = v;
            t.grip = grip; t.voidSpeed = voidSpeed; t.musicRoot = music;
            foreach (var m in mix) { t.rooms.Add(m.kind); t.weights.Add(m.w); }
            return t;
        }

        static List<WorldTheme> BuiltIn()
        {
            return new List<WorldTheme>
            {
                Make("foundry", "Sky Foundry", "A floating factory coming apart in the clouds.", 0, 5, 0.15f,
                    "#8fd3ff", "#cfeaff", "#5b6573", "#ffb347", "#ff5e3a", "#e8edf2", "#9aa7b5", "#2a0f45",
                    1f, 0.85f, 57,
                    (RoomKind.PlatformHop, 1.2f), (RoomKind.SpinnerGauntlet, 1f), (RoomKind.CrateAndPlate, 1f),
                    (RoomKind.BounceTowers, 0.8f), (RoomKind.MemoryPads, 0.8f), (RoomKind.GravityHall, 0.6f)),

                Make("frost", "Frost Observatory", "Icy floors, frozen telescopes, slippery decisions.", 2, 6, 0.3f,
                    "#bfe6ff", "#eef8ff", "#9ec9e6", "#6ad1ff", "#ff6b9a", "#ffffff", "#d7ecfa", "#1d2a5c",
                    0.35f, 0.9f, 62,
                    (RoomKind.CrumbleBridge, 1.2f), (RoomKind.CodeVault, 1f), (RoomKind.PlatformHop, 1f),
                    (RoomKind.GravityHall, 0.8f), (RoomKind.MemoryPads, 0.8f), (RoomKind.BounceTowers, 0.6f)),

                Make("neon", "Neon Arcade", "A dead arcade cabinet that swallowed its players.", 5, 6, 0.45f,
                    "#120a24", "#1d1036", "#2b1f4a", "#2bf0ff", "#ff2bd6", "#b98bff", "#3d2a6b", "#6b00ff",
                    1f, 1.05f, 60,
                    (RoomKind.MemoryPads, 1.3f), (RoomKind.SpinnerGauntlet, 1.1f), (RoomKind.GravityHall, 1.1f),
                    (RoomKind.BounceTowers, 1f), (RoomKind.CodeVault, 0.8f), (RoomKind.PlatformHop, 0.8f)),

                Make("temple", "Sunken Temple", "Old stone, older puzzles, and the water is coming.", 8, 7, 0.6f,
                    "#5fc4b8", "#9fe0d6", "#8a7a5c", "#ffd34d", "#e84a5f", "#f3e7c9", "#6f8f72", "#062b33",
                    0.9f, 1.1f, 55,
                    (RoomKind.CodeVault, 1.3f), (RoomKind.CrateAndPlate, 1.2f), (RoomKind.CrumbleBridge, 1f),
                    (RoomKind.PlatformHop, 1f), (RoomKind.MemoryPads, 0.8f), (RoomKind.GravityHall, 0.7f)),

                Make("spire", "Clockwork Spire", "Gears, pendulums and gravity that runs on a timer.", 11, 8, 0.8f,
                    "#ffcf8a", "#ffe8c7", "#7a5a3a", "#ffcc33", "#d6452b", "#f7e1b5", "#b08550", "#2b0a00",
                    1f, 1.2f, 53,
                    (RoomKind.SpinnerGauntlet, 1.4f), (RoomKind.GravityHall, 1.2f), (RoomKind.BounceTowers, 1f),
                    (RoomKind.CrumbleBridge, 1f), (RoomKind.CodeVault, 0.8f), (RoomKind.CrateAndPlate, 0.8f)),
            };
        }
    }
}
