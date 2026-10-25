using System;
using UnityEngine;

namespace Shiftscape
{
    /// <summary>All screens: main menu, world select, workshop, wardrobe, settings, HUD, pause, results.</summary>
    public class GameUI : MonoBehaviour
    {
        enum Screen_ { Home, Worlds, Workshop, Wardrobe, Settings, Achievements }
        Screen_ screen = Screen_.Home;
        int wardrobeTab;
        float fps, fpsAcc; int fpsN;
        Vector2 scroll;

        void Update()
        {
            fpsAcc += Time.unscaledDeltaTime; fpsN++;
            if (fpsAcc > 0.5f) { fps = fpsN / fpsAcc; fpsAcc = 0; fpsN = 0; }
        }

        void OnGUI()
        {
            UIK.Init();
            var gm = GameManager.I;
            if (gm == null) return;
            switch (gm.State)
            {
                case GameState.Menu: DrawMenu(); break;
                case GameState.Playing: DrawHud(gm); break;
                case GameState.Paused: DrawHud(gm); DrawPause(gm); break;
                case GameState.Won: DrawResults(gm, true); break;
                case GameState.Lost: DrawResults(gm, false); break;
            }
            if (SaveSystem.Data.showFps) GUI.Label(new Rect(8, 4, 200, 30), $"{fps:0} fps", UIK.Label(14, Color.white));
        }

        // ------------------------------------------------------------------ Menu

        void DrawMenu()
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            UIK.Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.25f));
            var d = SaveSystem.Data;

            // Top bar
            UIK.Panel(new Rect(16 * s, 12 * s, W - 32 * s, 56 * s), UIK.Ink);
            GUI.Label(new Rect(32 * s, 12 * s, 400 * s, 56 * s), "<b>SHIFTSCAPE</b>", UIK.Label(26, Color.white));
            GUI.Label(new Rect(W - 330 * s, 12 * s, 300 * s, 56 * s), $"◆ {d.shards}     ★ {SaveSystem.TotalStars}", UIK.Label(20, UIK.Gold, TextAnchor.MiddleRight, true));

            if (screen != Screen_.Home)
            {
                if (UIK.Button(new Rect(W / 2 - 330 * s, 80 * s, 110 * s, 48 * s), "‹ Back", UIK.Ink, Color.white, 18)) screen = Screen_.Home;
            }
            switch (screen)
            {
                case Screen_.Home: DrawHome(); break;
                case Screen_.Worlds: DrawWorlds(); break;
                case Screen_.Workshop: DrawWorkshop(); break;
                case Screen_.Wardrobe: DrawWardrobe(); break;
                case Screen_.Settings: DrawSettings(); break;
                case Screen_.Achievements: DrawAchievements(); break;
            }
        }

        void DrawHome()
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            float bw = Mathf.Min(420 * s, W - 40 * s), bh = 64 * s, x = W / 2 - bw / 2;
            float y = Mathf.Max(90 * s, H * 0.18f);
            UIK.Shadowed(new Rect(0, y, W, 90 * s), "SHIFTSCAPE", UIK.Title(64, Color.white));
            GUI.Label(new Rect(0, y + 80 * s, W, 30 * s), "Outrun the Void. Flip the world. Rewind your mistakes.", UIK.Label(16, UIK.Muted, TextAnchor.MiddleCenter));
            y += 125 * s;
            var daily = GameManager.DailyWorld;
            bool dailyDone = SaveSystem.Data.lastDailyDate == DateTime.UtcNow.ToString("yyyyMMdd");
            if (UIK.Button(new Rect(x, y, bw, bh), "▶  PLAY", UIK.Accent, UIK.Ink, 26)) screen = Screen_.Worlds; y += bh + 10 * s;
            if (UIK.Button(new Rect(x, y, bw, bh), (dailyDone ? "✓ " : "") + "DAILY SHIFT  ·  " + daily.displayName + (dailyDone ? "   " + UIK.Time(SaveSystem.Data.lastDailyBest) : ""), UIK.Gold, UIK.Ink, 18)) GameManager.I.StartDaily(); y += bh + 10 * s;
            float half = (bw - 10 * s) / 2;
            if (UIK.Button(new Rect(x, y, half, bh), "WORKSHOP", UIK.Ink, Color.white, 18)) screen = Screen_.Workshop;
            if (UIK.Button(new Rect(x + half + 10 * s, y, half, bh), "WARDROBE", UIK.Ink, Color.white, 18)) screen = Screen_.Wardrobe; y += bh + 10 * s;
            if (UIK.Button(new Rect(x, y, half, bh), "ACHIEVEMENTS", UIK.Ink, Color.white, 16)) screen = Screen_.Achievements;
            if (UIK.Button(new Rect(x + half + 10 * s, y, half, bh), "SETTINGS", UIK.Ink, Color.white, 18)) screen = Screen_.Settings; y += bh + 10 * s;
            GUI.Label(new Rect(0, H - 34 * s, W, 30 * s), "a JaKing Studios game  ·  v1.0", UIK.Label(13, UIK.Muted, TextAnchor.MiddleCenter));
        }

        void DrawWorlds()
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            GUI.Label(new Rect(0, 80 * s, W, 48 * s), "CHOOSE A WORLD", UIK.Title(30, Color.white));
            var all = WorldCatalog.All;
            float cw = Mathf.Min(660 * s, W - 40 * s), ch = 92 * s, x = W / 2 - cw / 2, y = 140 * s;
            int stars = SaveSystem.TotalStars;
            scroll = GUI.BeginScrollView(new Rect(0, y, W, H - y), scroll, new Rect(0, 0, W - 20 * s, all.Count * (ch + 10 * s) + 10 * s));
            float yy = 0;
            foreach (var w in all)
            {
                bool locked = stars < w.starsToUnlock;
                UIK.Panel(new Rect(x, yy, cw, ch), locked ? new Color(0.1f, 0.1f, 0.12f, 0.75f) : UIK.Ink);
                UIK.Fill(new Rect(x + 10 * s, yy + 10 * s, 14 * s, ch - 20 * s), w.accent);
                GUI.Label(new Rect(x + 34 * s, yy + 8 * s, cw * 0.55f, 36 * s), (locked ? "🔒 " : "") + w.displayName, UIK.Label(22, Color.white, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(x + 34 * s, yy + 42 * s, cw * 0.55f, 40 * s), locked ? $"Needs ★ {w.starsToUnlock}  (you have {stars})" : w.tagline, UIK.Label(14, UIK.Muted));
                int my = SaveSystem.Stars(w.id);
                string st = ""; for (int i = 0; i < 3; i++) st += i < my ? "★" : "☆";
                GUI.Label(new Rect(x + cw * 0.58f, yy + 8 * s, cw * 0.2f, 36 * s), st, UIK.Label(22, UIK.Gold, TextAnchor.MiddleLeft));
                float bt = SaveSystem.BestTime("world_" + w.id);
                GUI.Label(new Rect(x + cw * 0.58f, yy + 42 * s, cw * 0.2f, 36 * s), bt > 0 ? "best " + UIK.Time(bt) : $"{w.roomCount} rooms", UIK.Label(14, UIK.Muted));
                if (!locked)
                {
                    if (UIK.Button(new Rect(x + cw - 120 * s, yy + 12 * s, 108 * s, ch - 24 * s), "PLAY", w.accent, UIK.Ink, 20)) GameManager.I.StartWorld(w);
                    if (bt > 0 && UIK.Button(new Rect(x + cw - 230 * s, yy + 12 * s, 100 * s, ch - 24 * s), "GHOST\nRACE", new Color(1, 1, 1, 0.15f), Color.white, 13)) GameManager.I.StartGhostRace(w);
                }
                yy += ch + 10 * s;
            }
            GUI.EndScrollView();
        }

        void DrawWorkshop()
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            var d = SaveSystem.Data;
            GUI.Label(new Rect(0, 80 * s, W, 48 * s), "WORKSHOP", UIK.Title(30, Color.white));
            GUI.Label(new Rect(0, 118 * s, W, 26 * s), "Spend Shards on gadgets. Everything here is earned in-game.", UIK.Label(14, UIK.Muted, TextAnchor.MiddleCenter));
            float cw = Mathf.Min(660 * s, W - 40 * s), ch = 84 * s, x = W / 2 - cw / 2, y = 150 * s;
            scroll = GUI.BeginScrollView(new Rect(0, y, W, H - y), scroll, new Rect(0, 0, W - 20 * s, Catalog.Upgrades.Count * (ch + 10 * s)));
            float yy = 0;
            foreach (var u in Catalog.Upgrades)
            {
                int lvl = SaveSystem.UpgradeLevel(u.id);
                bool maxed = lvl >= u.MaxLevel;
                int cost = maxed ? 0 : u.costs[lvl];
                UIK.Panel(new Rect(x, yy, cw, ch), UIK.Ink);
                GUI.Label(new Rect(x + 20 * s, yy + 8 * s, cw * 0.6f, 34 * s), $"{u.name}  <color=#ffd34d>{new string('●', lvl)}{new string('○', u.MaxLevel - lvl)}</color>", UIK.Label(20, Color.white, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(x + 20 * s, yy + 42 * s, cw * 0.6f, 34 * s), u.desc, UIK.Label(14, UIK.Muted));
                if (maxed) GUI.Label(new Rect(x + cw - 140 * s, yy, 120 * s, ch), "MAXED", UIK.Label(18, UIK.Good, TextAnchor.MiddleCenter, true));
                else if (UIK.Button(new Rect(x + cw - 150 * s, yy + 14 * s, 134 * s, ch - 28 * s), $"◆ {cost}", d.shards >= cost ? UIK.Accent : new Color(1, 1, 1, 0.15f), d.shards >= cost ? UIK.Ink : Color.white, 20, d.shards >= cost))
                {
                    d.shards -= cost; SaveSystem.SetInt(d.upgrades, u.id, lvl + 1); SaveSystem.Save(); GameAudio.Play(Sfx.Unlock);
                }
                yy += ch + 10 * s;
            }
            GUI.EndScrollView();
        }

        void DrawWardrobe()
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            var d = SaveSystem.Data;
            GUI.Label(new Rect(0, 80 * s, W, 48 * s), "WARDROBE", UIK.Title(30, Color.white));
            float cw = Mathf.Min(660 * s, W - 40 * s), x = W / 2 - cw / 2;
            string[] tabs = { "SUITS", "HATS", "TRAILS" };
            float tw = cw / 3;
            for (int i = 0; i < 3; i++)
                if (UIK.Button(new Rect(x + i * tw, 130 * s, tw - 6 * s, 44 * s), tabs[i], wardrobeTab == i ? UIK.Accent : UIK.Ink, wardrobeTab == i ? UIK.Ink : Color.white, 16)) { wardrobeTab = i; scroll = Vector2.zero; }
            float y = 186 * s, ch = 64 * s;
            int count = wardrobeTab == 0 ? Catalog.Suits.Count : wardrobeTab == 1 ? Catalog.Hats.Count : Catalog.Trails.Count;
            scroll = GUI.BeginScrollView(new Rect(0, y, W, H - y), scroll, new Rect(0, 0, W - 20 * s, count * (ch + 8 * s)));
            float yy = 0;
            for (int i = 0; i < count; i++)
            {
                string id, name, prefix; int cost; Color swatch;
                if (wardrobeTab == 0) { var v = Catalog.Suits[i]; id = v.id; name = v.name; cost = v.cost; prefix = "suit"; swatch = v.body; }
                else if (wardrobeTab == 1) { var v = Catalog.Hats[i]; id = v.id; name = v.name; cost = v.cost; prefix = "hat"; swatch = UIK.Gold; }
                else { var v = Catalog.Trails[i]; id = v.id; name = v.name; cost = v.cost; prefix = "trail"; swatch = v.a; }
                string key = prefix + ":" + id;
                bool owned = SaveSystem.Owns(key);
                bool equipped = (prefix == "suit" ? d.suit : prefix == "hat" ? d.hat : d.trail) == id;
                UIK.Panel(new Rect(x, yy, cw, ch), equipped ? new Color(0.1f, 0.35f, 0.4f, 0.9f) : UIK.Ink);
                UIK.Fill(new Rect(x + 14 * s, yy + 14 * s, 36 * s, 36 * s), swatch);
                GUI.Label(new Rect(x + 64 * s, yy, cw * 0.5f, ch), name, UIK.Label(20, Color.white, TextAnchor.MiddleLeft, true));
                if (owned)
                {
                    if (!equipped && UIK.Button(new Rect(x + cw - 150 * s, yy + 10 * s, 134 * s, ch - 20 * s), "EQUIP", UIK.Accent, UIK.Ink, 18))
                    { if (prefix == "suit") d.suit = id; else if (prefix == "hat") d.hat = id; else d.trail = id; SaveSystem.Save(); }
                    else if (equipped) GUI.Label(new Rect(x + cw - 150 * s, yy, 134 * s, ch), "EQUIPPED", UIK.Label(16, UIK.Good, TextAnchor.MiddleCenter, true));
                }
                else if (UIK.Button(new Rect(x + cw - 150 * s, yy + 10 * s, 134 * s, ch - 20 * s), $"◆ {cost}", d.shards >= cost ? UIK.Gold : new Color(1, 1, 1, 0.15f), d.shards >= cost ? UIK.Ink : Color.white, 18, d.shards >= cost))
                { d.shards -= cost; d.owned.Add(key); SaveSystem.Save(); GameAudio.Play(Sfx.Unlock); }
                yy += ch + 8 * s;
            }
            GUI.EndScrollView();
        }

        void DrawSettings()
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            var d = SaveSystem.Data;
            GUI.Label(new Rect(0, 80 * s, W, 48 * s), "SETTINGS", UIK.Title(30, Color.white));
            float cw = Mathf.Min(560 * s, W - 40 * s), x = W / 2 - cw / 2, y = 140 * s, rh = 56 * s;
            UIK.Panel(new Rect(x - 16 * s, y - 12 * s, cw + 32 * s, rh * 9 + 30 * s), UIK.Ink);
            var ls = UIK.Label(17, Color.white);
            GUI.Label(new Rect(x, y, cw * 0.45f, rh), "Look sensitivity", ls); d.lookSensitivity = GUI.HorizontalSlider(new Rect(x + cw * 0.45f, y + rh * 0.4f, cw * 0.55f, 20 * s), d.lookSensitivity, 0.3f, 3f); y += rh;
            GUI.Label(new Rect(x, y, cw * 0.45f, rh), "Sound volume", ls); d.volume = GUI.HorizontalSlider(new Rect(x + cw * 0.45f, y + rh * 0.4f, cw * 0.55f, 20 * s), d.volume, 0f, 1f); y += rh;
            GUI.Label(new Rect(x, y, cw * 0.45f, rh), "Music volume", ls); d.musicVolume = GUI.HorizontalSlider(new Rect(x + cw * 0.45f, y + rh * 0.4f, cw * 0.55f, 20 * s), d.musicVolume, 0f, 1f); y += rh;
            string[] tm = { "Auto", "On", "Off" };
            if (UIK.Button(new Rect(x, y + 6 * s, cw, rh - 12 * s), "Touch controls: " + tm[d.touchMode], new Color(1, 1, 1, 0.12f), Color.white, 16)) d.touchMode = (d.touchMode + 1) % 3; y += rh;
            if (UIK.Button(new Rect(x, y + 6 * s, cw, rh - 12 * s), "Invert look Y: " + (d.invertY ? "On" : "Off"), new Color(1, 1, 1, 0.12f), Color.white, 16)) d.invertY = !d.invertY; y += rh;
            if (UIK.Button(new Rect(x, y + 6 * s, cw, rh - 12 * s), "Calm mode (slower Void): " + (d.calmMode ? "On" : "Off"), new Color(1, 1, 1, 0.12f), Color.white, 16)) d.calmMode = !d.calmMode; y += rh;
            if (UIK.Button(new Rect(x, y + 6 * s, cw, rh - 12 * s), "Graphics: " + (d.highQuality ? "High (shadows)" : "Fast"), new Color(1, 1, 1, 0.12f), Color.white, 16)) d.highQuality = !d.highQuality; y += rh;
            if (UIK.Button(new Rect(x, y + 6 * s, cw, rh - 12 * s), "Show ghost of best run: " + (d.ghostEnabled ? "On" : "Off") + "   ·   FPS: " + (d.showFps ? "On" : "Off"), new Color(1, 1, 1, 0.12f), Color.white, 14)) { if (Event.current.mousePosition.x < x + cw / 2) d.ghostEnabled = !d.ghostEnabled; else d.showFps = !d.showFps; } y += rh;
            if (UIK.Button(new Rect(x, y + 6 * s, cw, rh - 12 * s), "Reset all progress (hold-free: tap twice)", UIK.Danger, Color.white, 14)) { resetTaps++; if (resetTaps >= 2) { SaveSystem.Wipe(); resetTaps = 0; } }
            if (GUI.changed) SaveSystem.Save();
        }
        int resetTaps;

        void DrawAchievements()
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            var d = SaveSystem.Data;
            GUI.Label(new Rect(0, 80 * s, W, 48 * s), $"ACHIEVEMENTS  {d.achievements.Count}/{Catalog.Achievements.Count}", UIK.Title(30, Color.white));
            float cw = Mathf.Min(660 * s, W - 40 * s), ch = 58 * s, x = W / 2 - cw / 2, y = 140 * s;
            scroll = GUI.BeginScrollView(new Rect(0, y, W, H - y), scroll, new Rect(0, 0, W - 20 * s, Catalog.Achievements.Count * (ch + 8 * s) + 60 * s));
            float yy = 0;
            foreach (var a in Catalog.Achievements)
            {
                bool got = d.achievements.Contains(a.id);
                UIK.Panel(new Rect(x, yy, cw, ch), got ? new Color(0.1f, 0.35f, 0.25f, 0.9f) : UIK.Ink);
                GUI.Label(new Rect(x + 16 * s, yy, cw * 0.7f, ch * 0.55f), (got ? "✓ " : "") + a.name, UIK.Label(18, Color.white, TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(x + 16 * s, yy + ch * 0.5f, cw * 0.7f, ch * 0.5f), a.desc, UIK.Label(13, UIK.Muted));
                GUI.Label(new Rect(x + cw - 110 * s, yy, 96 * s, ch), $"◆ {a.reward}", UIK.Label(16, UIK.Gold, TextAnchor.MiddleRight, true));
                yy += ch + 8 * s;
            }
            GUI.Label(new Rect(x, yy, cw, 50 * s), $"Runs {d.runs}  ·  Escapes {d.escapes}  ·  Flips {d.flips}  ·  Rewinds {d.rewinds}  ·  Puzzles {d.puzzlesSolved}  ·  Shards ever {d.totalShards}", UIK.Label(13, UIK.Muted, TextAnchor.MiddleCenter));
            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ HUD

        void DrawHud(GameManager gm)
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            var p = PlayerMotor.I;
            // Top-left: hearts + shards + time
            UIK.Panel(new Rect(12 * s, 12 * s, 260 * s, 74 * s), new Color(0, 0, 0, 0.45f));
            string hearts = ""; for (int i = 0; i < gm.MaxHearts; i++) hearts += i < gm.Hearts ? "♥ " : "♡ ";
            GUI.Label(new Rect(24 * s, 14 * s, 240 * s, 32 * s), hearts, UIK.Label(22, UIK.Danger, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(24 * s, 46 * s, 240 * s, 32 * s), $"◆ {gm.RunShards}/{gm.RunShardTotal}     {UIK.Time(gm.RunTime)}", UIK.Label(16, Color.white));
            // Top-centre: room progress
            string rooms = $"ROOM {Mathf.Min(gm.RoomIndex + 1, gm.RoomCount)} / {gm.RoomCount}";
            UIK.Shadowed(new Rect(0, 14 * s, W, 30 * s), rooms, UIK.Title(16, new Color(1, 1, 1, 0.85f)));
            float prog = gm.RoomCount > 0 ? Mathf.Clamp01((float)gm.RoomIndex / gm.RoomCount) : 0;
            UIK.Fill(new Rect(W / 2 - 120 * s, 46 * s, 240 * s, 6 * s), new Color(0, 0, 0, 0.5f));
            UIK.Fill(new Rect(W / 2 - 120 * s, 46 * s, 240 * s * prog, 6 * s), gm.World.accent);

            // Void proximity meter (bottom centre)
            float vd = gm.VoidDistance;
            if (vd < 28f && !gm.Void.paused)
            {
                float k = 1 - Mathf.Clamp01(vd / 28f);
                UIK.Fill(new Rect(0, 0, W, H), new Color(gm.World.voidColor.r, gm.World.voidColor.g, gm.World.voidColor.b, 0.35f * k * k));
                UIK.Shadowed(new Rect(0, H * 0.3f, W, 40 * s), vd < 8 ? "THE VOID IS HERE" : "VOID  " + vd.ToString("0") + "m", UIK.Title(vd < 8 ? 28 : 18, Color.Lerp(Color.white, UIK.Danger, k)));
            }
            if (gm.Void.paused) UIK.Shadowed(new Rect(0, 60 * s, W, 30 * s), "SAFE ROOM · solve the puzzle", UIK.Title(14, UIK.Good));

            // Gadget bar (bottom-left on desktop, bottom-centre on touch)
            float gx = GameInput.TouchActive ? W / 2 - 150 * s : 12 * s, gy = H - 52 * s;
            UIK.Panel(new Rect(gx, gy, 300 * s, 40 * s), new Color(0, 0, 0, 0.45f));
            float dr = p != null ? p.DashReady01 : 1;
            UIK.Fill(new Rect(gx + 10 * s, gy + 8 * s, 120 * s, 24 * s), new Color(1, 1, 1, 0.15f));
            UIK.Fill(new Rect(gx + 10 * s, gy + 8 * s, 120 * s * dr, 24 * s), dr >= 1 ? UIK.Accent : new Color(0.3f, 0.6f, 0.7f));
            GUI.Label(new Rect(gx + 10 * s, gy + 8 * s, 120 * s, 24 * s), "DASH", UIK.Label(12, UIK.Ink, TextAnchor.MiddleCenter, true));
            string rw = ""; for (int i = 0; i < (p != null ? p.MaxRewindCharges : 0); i++) rw += i < p.RewindCharges ? "⟲ " : "· ";
            GUI.Label(new Rect(gx + 140 * s, gy, 160 * s, 40 * s), "REWIND " + rw, UIK.Label(13, p != null && p.Rewinding ? UIK.Gold : Color.white, TextAnchor.MiddleLeft, true));

            // Interact prompt
            if (gm.NearInteractable != null)
                UIK.Shadowed(new Rect(0, H * 0.62f, W, 36 * s), (GameInput.TouchActive ? "USE: " : "[E] ") + gm.NearInteractable.prompt, UIK.Title(18, UIK.Gold));

            // Toast
            if (gm.ToastTime > 0)
            {
                float a = Mathf.Clamp01(gm.ToastTime / 0.4f);
                UIK.Shadowed(new Rect(0, H * 0.2f, W, 44 * s), gm.Toast, UIK.Title(24, new Color(1, 1, 1, a)));
            }

            // Rewind vignette
            if (p != null && p.Rewinding) UIK.Fill(new Rect(0, 0, W, H), new Color(1f, 0.85f, 0.3f, 0.12f));

            // Desktop hint / pause button
            if (!GameInput.TouchActive)
                GUI.Label(new Rect(W - 420 * s, H - 30 * s, 410 * s, 24 * s), "WASD move · Space jump · Shift dash · R rewind · E use · Esc pause", UIK.Label(11, UIK.Muted, TextAnchor.MiddleRight));
            else DrawTouch();
        }

        void DrawTouch()
        {
            float s = UIK.Scale;
            // Joystick
            if (GameInput.StickActive)
            {
                float r = GameInput.StickRadius;
                var o = GameInput.StickOrigin; var pp = GameInput.StickPos;
                DrawCircle(o, r, new Color(1, 1, 1, 0.18f));
                DrawCircle(pp, r * 0.42f, new Color(1, 1, 1, 0.6f));
            }
            else
            {
                DrawCircle(new Vector2(120 * s, Screen.height - 120 * s), GameInput.StickRadius, new Color(1, 1, 1, 0.08f));
            }
            DrawTouchButton(TouchButton.Jump, "JUMP", UIK.Accent);
            DrawTouchButton(TouchButton.Dash, "DASH", Color.white);
            DrawTouchButton(TouchButton.Use, "USE", UIK.Gold);
            DrawTouchButton(TouchButton.Rewind, "⟲", UIK.Gold);
            DrawTouchButton(TouchButton.Pause, "II", Color.white);
        }

        void DrawTouchButton(TouchButton b, string text, Color c)
        {
            var r = GameInput.ButtonRect(b);
            bool held = GameInput.ButtonHeld[(int)b];
            DrawCircle(r.center, r.width / 2, new Color(c.r, c.g, c.b, held ? 0.6f : 0.25f));
            GUI.Label(r, text, UIK.Label(b == TouchButton.Jump ? 20 : 15, held ? UIK.Ink : Color.white, TextAnchor.MiddleCenter, true));
        }

        static Texture2D circle;
        static void DrawCircle(Vector2 c, float r, Color col)
        {
            if (circle == null)
            {
                int n = 64; circle = new Texture2D(n, n);
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)); circle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - d))); }
                circle.Apply();
            }
            var old = GUI.color; GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2, r * 2), circle);
            GUI.color = old;
        }

        // ------------------------------------------------------------------ Pause / Results

        void DrawPause(GameManager gm)
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            UIK.Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.55f));
            float bw = Mathf.Min(360 * s, W - 40 * s), bh = 60 * s, x = W / 2 - bw / 2, y = H * 0.3f;
            GUI.Label(new Rect(0, y - 70 * s, W, 60 * s), "PAUSED", UIK.Title(40, Color.white));
            if (UIK.Button(new Rect(x, y, bw, bh), "RESUME", UIK.Accent, UIK.Ink, 22)) gm.Pause(false); y += bh + 10 * s;
            if (UIK.Button(new Rect(x, y, bw, bh), "RESTART RUN", UIK.Ink, Color.white, 18)) gm.Retry(); y += bh + 10 * s;
            var d = SaveSystem.Data;
            if (UIK.Button(new Rect(x, y, bw, bh), "Calm mode: " + (d.calmMode ? "On" : "Off"), UIK.Ink, Color.white, 16)) { d.calmMode = !d.calmMode; SaveSystem.Save(); } y += bh + 10 * s;
            if (UIK.Button(new Rect(x, y, bw, bh), "QUIT TO MENU", UIK.Danger, Color.white, 18)) gm.QuitToMenu();
        }

        void DrawResults(GameManager gm, bool won)
        {
            float s = UIK.Scale; float W = Screen.width, H = Screen.height;
            UIK.Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.5f));
            float pw = Mathf.Min(520 * s, W - 30 * s), ph = Mathf.Min(460 * s, H - 30 * s), x = W / 2 - pw / 2, y = H / 2 - ph / 2;
            UIK.Panel(new Rect(x, y, pw, ph), UIK.Ink);
            GUI.Label(new Rect(x, y + 14 * s, pw, 54 * s), won ? "ESCAPED!" : "LOST TO THE VOID", UIK.Title(36, won ? UIK.Good : UIK.Danger));
            float yy = y + 76 * s;
            var ls = UIK.Label(18, Color.white, TextAnchor.MiddleCenter);
            if (won)
            {
                string st = ""; for (int i = 0; i < 3; i++) st += i < gm.StarsEarned ? "★ " : "☆ ";
                GUI.Label(new Rect(x, yy, pw, 44 * s), st, UIK.Title(34, UIK.Gold)); yy += 46 * s;
                GUI.Label(new Rect(x, yy, pw, 28 * s), $"Time {UIK.Time(gm.RunTime)}" + (gm.NewBest ? "   <color=#59e3b0>NEW BEST</color>" : gm.BestBefore > 0 ? $"   (best {UIK.Time(gm.BestBefore)})" : ""), ls); yy += 30 * s;
                GUI.Label(new Rect(x, yy, pw, 28 * s), (gm.Flawless ? "<color=#59e3b0>Flawless</color>" : "Took damage") + "   ·   " + (gm.RunShardTotal > 0 && gm.RunShards >= gm.RunShardTotal * 0.8f ? "<color=#59e3b0>Shard hunter</color>" : "Shards < 80%"), ls); yy += 30 * s;
                if (gm.BeatGhost) { GUI.Label(new Rect(x, yy, pw, 28 * s), "<color=#ffd34d>You beat your ghost!</color>", ls); yy += 30 * s; }
                GUI.Label(new Rect(x, yy, pw, 28 * s), $"◆ {gm.RunShards} shards earned (incl. bonus)", UIK.Label(18, UIK.Gold, TextAnchor.MiddleCenter, true)); yy += 36 * s;
                GUI.Label(new Rect(x + 20 * s, yy, pw - 40 * s, 40 * s), "Stars: 1 for escaping, +1 flawless, +1 for 80% of the shards. Stars unlock new worlds.", UIK.Label(12, UIK.Muted, TextAnchor.MiddleCenter)); yy += 44 * s;
            }
            else
            {
                GUI.Label(new Rect(x, yy, pw, 28 * s), $"Reached room {Mathf.Min(gm.RoomIndex + 1, gm.RoomCount)} of {gm.RoomCount}  ·  {UIK.Time(gm.RunTime)}", ls); yy += 32 * s;
                GUI.Label(new Rect(x, yy, pw, 28 * s), $"Kept half your shards:  ◆ {gm.RunShards}", UIK.Label(18, UIK.Gold, TextAnchor.MiddleCenter, true)); yy += 36 * s;
                GUI.Label(new Rect(x + 20 * s, yy, pw - 40 * s, 60 * s), gm.VoidWarnings >= 2 ? "Tip: the Void pauses in puzzle rooms, and Rewind (R) undoes the last 3 seconds. Calm mode in Settings slows it down." : "Tip: Dash across gaps, and hold Jump to glide once you own the Drift Cape.", UIK.Label(13, UIK.Muted, TextAnchor.MiddleCenter)); yy += 64 * s;
            }
            float bw = (pw - 60 * s) / 2, bh = 56 * s;
            if (UIK.Button(new Rect(x + 20 * s, y + ph - bh - 20 * s, bw, bh), won ? "PLAY AGAIN" : "RETRY", UIK.Accent, UIK.Ink, 20)) gm.Retry();
            if (UIK.Button(new Rect(x + 40 * s + bw, y + ph - bh - 20 * s, bw, bh), "MENU", new Color(1, 1, 1, 0.15f), Color.white, 20)) gm.QuitToMenu();
        }
    }
}
