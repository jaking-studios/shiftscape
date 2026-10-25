using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    /// <summary>Gadget upgrades bought in the Workshop with Shards. Gameplay upgrades are earned only, never sold.</summary>
    public class UpgradeDef
    {
        public string id, name, desc;
        public int[] costs; // one entry per level
        public int MaxLevel => costs.Length;
        public UpgradeDef(string id, string name, string desc, params int[] costs)
        { this.id = id; this.name = name; this.desc = desc; this.costs = costs; }
    }

    public class HatDef
    {
        public string id, name; public int cost;
        public HatDef(string id, string name, int cost) { this.id = id; this.name = name; this.cost = cost; }
    }

    public class SuitDef
    {
        public string id, name; public Color body, visor; public int cost;
        public SuitDef(string id, string name, string body, string visor, int cost)
        { this.id = id; this.name = name; this.body = Mat.Hex(body); this.visor = Mat.Hex(visor); this.cost = cost; }
    }

    public class TrailDef
    {
        public string id, name; public Color a, b; public int cost;
        public TrailDef(string id, string name, string a, string b, int cost)
        { this.id = id; this.name = name; this.a = Mat.Hex(a); this.b = Mat.Hex(b); this.cost = cost; }
    }

    public class AchievementDef
    {
        public string id, name, desc; public int reward;
        public AchievementDef(string id, string name, string desc, int reward)
        { this.id = id; this.name = name; this.desc = desc; this.reward = reward; }
    }

    public static class Catalog
    {
        public const string DoubleJump = "doublejump";
        public const string Glider = "glider";
        public const string DashCooldown = "dashcd";
        public const string RewindCharges = "rewind";
        public const string Magnet = "magnet";
        public const string ExtraHeart = "heart";

        public static readonly List<UpgradeDef> Upgrades = new List<UpgradeDef>
        {
            new UpgradeDef(DoubleJump, "Pulse Boots", "Jump a second time in mid-air.", 60),
            new UpgradeDef(Glider, "Drift Cape", "Hold Jump while falling to glide.", 90),
            new UpgradeDef(DashCooldown, "Overclock", "Dash recharges faster.", 40, 80, 140),
            new UpgradeDef(RewindCharges, "Chrono Coil", "+1 Rewind charge per room.", 50, 120),
            new UpgradeDef(Magnet, "Shard Magnet", "Pull nearby Shards toward you.", 50, 110),
            new UpgradeDef(ExtraHeart, "Spare Core", "Start every run with an extra heart.", 150),
        };

        public static readonly List<SuitDef> Suits = new List<SuitDef>
        {
            new SuitDef("classic", "Classic", "#f2f4f8", "#2bd9ff", 0),
            new SuitDef("ember", "Ember", "#ff6a3d", "#ffe36e", 30),
            new SuitDef("mint", "Mint", "#59e3b0", "#14324a", 30),
            new SuitDef("grape", "Grape", "#8a5cff", "#ffb3f0", 40),
            new SuitDef("night", "Night", "#2a2f45", "#7cf7ff", 60),
            new SuitDef("gold", "Gilded", "#f4c542", "#3a2a00", 120),
            new SuitDef("candy", "Candy", "#ff8fc8", "#ffffff", 60),
            new SuitDef("jaking", "JaKing", "#1c4dff", "#ffd400", 200),
        };

        public static readonly List<HatDef> Hats = new List<HatDef>
        {
            new HatDef("none", "None", 0),
            new HatDef("antenna", "Antenna", 20),
            new HatDef("tophat", "Top Hat", 45),
            new HatDef("crown", "Crown", 150),
            new HatDef("halo", "Halo", 90),
            new HatDef("horns", "Horns", 70),
        };

        public static readonly List<TrailDef> Trails = new List<TrailDef>
        {
            new TrailDef("none", "None", "#ffffff", "#ffffff", 0),
            new TrailDef("comet", "Comet", "#ffffff", "#2bd9ff", 25),
            new TrailDef("flare", "Solar Flare", "#ffe36e", "#ff3d3d", 50),
            new TrailDef("aurora", "Aurora", "#59e3b0", "#8a5cff", 80),
            new TrailDef("void", "Voidwalker", "#6b00ff", "#000000", 120),
        };

        public static readonly List<AchievementDef> Achievements = new List<AchievementDef>
        {
            new AchievementDef("first", "First Shift", "Escape any world.", 20),
            new AchievementDef("flawless", "Flawless", "Escape without losing a heart.", 40),
            new AchievementDef("flipper", "Gravity Guru", "Flip gravity 25 times.", 30),
            new AchievementDef("rewinder", "Second Chances", "Use Rewind 15 times.", 25),
            new AchievementDef("hoarder", "Shard Hoarder", "Collect 300 Shards in total.", 50),
            new AchievementDef("daily", "Daily Shifter", "Finish a Daily Shift.", 30),
            new AchievementDef("ghost", "Ghostbuster", "Beat your own ghost.", 30),
            new AchievementDef("threestar", "Perfect World", "Get 3 stars in any world.", 50),
            new AchievementDef("allworlds", "Realm Walker", "Escape all five worlds.", 100),
            new AchievementDef("puzzler", "Puzzler", "Solve 20 puzzle rooms.", 40),
        };

        public static UpgradeDef Upgrade(string id) => Upgrades.Find(u => u.id == id);
        public static SuitDef Suit(string id) { var s = Suits.Find(x => x.id == id); return s ?? Suits[0]; }
        public static HatDef Hat(string id) { var s = Hats.Find(x => x.id == id); return s ?? Hats[0]; }
        public static TrailDef Trail(string id) { var s = Trails.Find(x => x.id == id); return s ?? Trails[0]; }
    }
}
