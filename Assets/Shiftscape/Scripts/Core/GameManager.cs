using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    public enum GameState { Menu, Playing, Paused, Won, Lost }
    public enum RunMode { World, Daily, GhostRace }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        public RunMode Mode { get; private set; }
        public WorldTheme World { get; private set; }
        public int Seed { get; private set; }
        public float RunTime { get; private set; }
        public int Hearts { get; private set; }
        public int MaxHearts { get; private set; }
        public int RunShards { get; private set; }
        public int RunShardTotal { get; private set; }
        public int RoomIndex { get; private set; }
        public int RoomCount => level != null ? level.rooms.Count - 1 : 0;
        public bool Flawless { get; private set; }
        public int StarsEarned { get; private set; }
        public float BestBefore { get; private set; }
        public bool NewBest { get; private set; }
        public bool BeatGhost { get; private set; }
        public string Toast { get; private set; } = "";
        public float ToastTime { get; private set; }
        public Interactable NearInteractable { get; private set; }
        public VoidWall Void => voidWall;
        public float VoidDistance => PlayerMotor.I != null && voidWall != null ? PlayerMotor.I.transform.position.z - voidWall.Z : 99f;
        public int VoidWarnings { get; private set; }

        LevelBuilder level;
        VoidWall voidWall;
        PlayerMotor player;
        PlayerVisual visual;
        CameraRig cam;
        Checkpoint lastCp;
        float hurtCooldown;
        GhostRecorder ghost;
        float furthestZ;
        readonly List<Interactable> interactables = new List<Interactable>();

        public static string DailyKey => "daily_" + DateTime.UtcNow.ToString("yyyyMMdd");
        public static int DailySeed { get { var d = DateTime.UtcNow; return d.Year * 10000 + d.Month * 100 + d.Day; } }
        public static WorldTheme DailyWorld { get { var all = WorldCatalog.All; return all[DailySeed % all.Count]; } }

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            SaveSystem.Load();
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Physics.gravity = Vector3.zero; // motor handles gravity
            if (GetComponent<GameInput>() == null) gameObject.AddComponent<GameInput>();
            if (GetComponent<GameAudio>() == null) gameObject.AddComponent<GameAudio>();
            if (GetComponent<GameUI>() == null) gameObject.AddComponent<GameUI>();
            SetupMenuScene();
        }

        // ------------------------------------------------------------ Scene setup

        Transform menuScene;

        void SetupMenuScene()
        {
            ClearWorld();
            State = GameState.Menu;
            GameInput.Enabled = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var t = WorldCatalog.All[Mathf.Min(WorldCatalog.All.Count - 1, Mathf.Max(0, SaveSystem.Data.escapes) % WorldCatalog.All.Count)];
            ApplyAtmosphere(t);
            menuScene = new GameObject("MenuScene").transform;
            var rng = new System.Random(42);
            var m = Mat.Lit(Color.Lerp(t.decor, t.sky, 0.3f), 0.1f);
            for (int i = 0; i < 40; i++)
            {
                var g = Mat.Box(menuScene, new Vector3((float)(rng.NextDouble() * 80 - 40), (float)(rng.NextDouble() * 40 - 25), (float)(rng.NextDouble() * 60 + 10)), new Vector3(2 + (float)rng.NextDouble() * 8, 1 + (float)rng.NextDouble() * 3, 2 + (float)rng.NextDouble() * 8), m, false);
                var f = g.AddComponent<Float>(); f.amp = 0.5f + (float)rng.NextDouble(); f.speed = 0.3f; f.spin = (float)rng.NextDouble() * 10 - 5;
            }
            EnsureCamera();
            cam.target = null;
            cam.transform.position = new Vector3(0, 2, -6);
            cam.transform.rotation = Quaternion.Euler(5, 0, 0);
            cam.gameObject.AddComponent<MenuCamDrift>();
            GameAudio.PlayMusic(t.musicRoot);
        }

        void EnsureCamera()
        {
            if (cam != null) return;
            var existing = Camera.main;
            GameObject cg = existing != null ? existing.gameObject : new GameObject("Main Camera");
            cg.tag = "MainCamera";
            cam = cg.GetComponent<CameraRig>() ?? cg.AddComponent<CameraRig>();
            DontDestroyOnLoad(cg);
        }

        void ApplyAtmosphere(WorldTheme t)
        {
            EnsureCamera();
            cam.Cam.clearFlags = CameraClearFlags.SolidColor;
            cam.Cam.backgroundColor = t.sky;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = t.fog;
            RenderSettings.fogStartDistance = t.fogStart;
            RenderSettings.fogEndDistance = t.fogEnd;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(t.sky, Color.white, 0.4f);
            RenderSettings.ambientEquatorColor = Color.Lerp(t.fog, t.ground, 0.5f);
            RenderSettings.ambientGroundColor = Color.Lerp(t.ground, Color.black, 0.5f);
            var sun = GameObject.Find("Sun");
            if (sun == null) { sun = new GameObject("Sun"); DontDestroyOnLoad(sun); sun.AddComponent<Light>(); }
            var l = sun.GetComponent<Light>();
            l.type = LightType.Directional; l.color = Color.Lerp(Color.white, t.accent, 0.15f); l.intensity = 1.15f;
            l.shadows = SaveSystem.Data.highQuality ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.6f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            QualitySettings.shadowDistance = SaveSystem.Data.highQuality ? 60 : 0;
        }

        void ClearWorld()
        {
            if (menuScene != null) Destroy(menuScene.gameObject);
            if (level != null) Destroy(level.Root.gameObject);
            if (voidWall != null) Destroy(voidWall.gameObject);
            if (player != null) Destroy(player.gameObject);
            if (ghost != null) ghost.Destroy();
            if (cam != null) { var d = cam.GetComponent<MenuCamDrift>(); if (d != null) Destroy(d); }
            level = null; voidWall = null; player = null; ghost = null;
            interactables.Clear();
        }

        // ------------------------------------------------------------ Run flow

        public void StartWorld(WorldTheme t) { Mode = RunMode.World; BeginRun(t, UnityEngine.Random.Range(1, 999999), t.roomCount, 0f); }
        public void StartDaily() { Mode = RunMode.Daily; var t = DailyWorld; BeginRun(t, DailySeed, t.roomCount + 1, 0.1f); }
        public void StartGhostRace(WorldTheme t)
        {
            Mode = RunMode.GhostRace;
            var g = SaveSystem.LoadGhost("world_" + t.id);
            int seed = SaveSystem.GetInt(SaveSystem.Data.upgrades, "ghostseed_" + t.id); // stored seed of best run
            if (g == null || seed == 0) { StartWorld(t); return; }
            BeginRun(t, seed, t.roomCount, 0f);
        }

        void BeginRun(WorldTheme t, int seed, int roomCount, float diffBoost)
        {
            ClearWorld();
            World = t; Seed = seed;
            ApplyAtmosphere(t);
            level = new LevelBuilder(t, seed, roomCount, diffBoost);
            RunShardTotal = level.shardCount;
            RunShards = 0; RunTime = 0; RoomIndex = 0; Flawless = true; VoidWarnings = 0;
            MaxHearts = 3 + SaveSystem.UpgradeLevel(Catalog.ExtraHeart);
            Hearts = MaxHearts;
            NewBest = false; BeatGhost = false; StarsEarned = 0;
            furthestZ = 0;

            var pg = new GameObject("Player");
            pg.AddComponent<Rigidbody>(); pg.AddComponent<CapsuleCollider>();
            player = pg.AddComponent<PlayerMotor>();
            player.ApplyUpgrades();
            player.RewindCharges = player.MaxRewindCharges;
            visual = PlayerVisual.Build(player);
            EnsureCamera();
            cam.target = player.transform;
            player.SetCamera(cam.transform);
            player.Teleport(level.spawn, Vector3.forward);
            player.Grip = t.grip;
            lastCp = level.rooms[0].checkpoint;

            float vspeed = t.voidSpeed * (SaveSystem.Data.calmMode ? 0.55f : 1f) * (Mode == RunMode.Daily ? 1.1f : 1f);
            voidWall = VoidWall.Create(t.voidColor, level.spawn.z - 14f);
            voidWall.speed = vspeed;

            string ghostKey = Mode == RunMode.Daily ? DailyKey : "world_" + t.id;
            BestBefore = SaveSystem.BestTime(ghostKey);
            ghost = new GhostRecorder(ghostKey, SaveSystem.Data.ghostEnabled && (Mode != RunMode.Daily), t);

            foreach (var it in level.Root.GetComponentsInChildren<Interactable>()) interactables.Add(it);

            State = GameState.Playing;
            GameInput.Enabled = true;
            LockCursor(true);
            SaveSystem.Data.runs++;
            GameAudio.PlayMusic(t.musicRoot);
            ShowToast(Mode == RunMode.Daily ? "DAILY SHIFT  ·  " + t.displayName : t.displayName.ToUpper());
        }

        public void LockCursor(bool on)
        {
            if (GameInput.TouchActive) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return; }
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        public void Pause(bool p)
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            State = p ? GameState.Paused : GameState.Playing;
            Time.timeScale = p ? 0f : 1f;
            GameInput.Enabled = !p;
            LockCursor(!p);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            SaveSystem.Save();
            SetupMenuScene();
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            var t = World; var mode = Mode; int seed = Seed;
            if (mode == RunMode.Daily) StartDaily();
            else if (mode == RunMode.GhostRace) StartGhostRace(t);
            else StartWorld(t);
        }

        void Update()
        {
            if (State == GameState.Menu) return;
            if (GameInput.PauseDown)
            {
                if (State == GameState.Playing) Pause(true);
                else if (State == GameState.Paused) Pause(false);
            }
            if (State != GameState.Playing) return;

            RunTime += Time.deltaTime;
            hurtCooldown -= Time.deltaTime;
            ToastTime -= Time.deltaTime;
            ghost?.Tick(player, Time.deltaTime);

            // Room tracking + void pausing in puzzle rooms
            float pz = player.transform.position.z;
            furthestZ = Mathf.Max(furthestZ, pz);
            int ri = 0;
            for (int i = 0; i < level.rooms.Count; i++) if (pz >= level.rooms[i].zStart) ri = i;
            if (ri != RoomIndex) { RoomIndex = ri; player.RewindCharges = Mathf.Max(player.RewindCharges, Mathf.Min(player.MaxRewindCharges, player.RewindCharges + 1)); }
            var room = level.rooms[ri];
            voidWall.paused = room.isPuzzle && pz > room.zStart + 2;
            if (voidWall.paused) voidWall.JumpTo(room.zStart - 6f);
            voidWall.speed = World.voidSpeed * (SaveSystem.Data.calmMode ? 0.55f : 1f) * (1f + 0.06f * ri) * (Mode == RunMode.Daily ? 1.1f : 1f);
            // Void never gets absurdly far behind (keeps pressure on slow players, but fair)
            if (!voidWall.paused && pz - voidWall.Z > 45f) voidWall.SetZ(pz - 45f);

            // Interactables
            NearInteractable = null; float best = 99;
            foreach (var it in interactables)
            {
                if (it == null || !it.enabledForUse) continue;
                float d = Vector3.Distance(it.transform.position, player.transform.position);
                if (d < it.radius && d < best) { best = d; NearInteractable = it; }
            }
            if (NearInteractable != null && GameInput.UseDown) NearInteractable.Use();

            // Fell forever (safety)
            if (player.transform.position.y < -30f) PlayerHurt(false, player.transform.position);
        }

        // ------------------------------------------------------------ Events from world

        public void ReachCheckpoint(Checkpoint cp)
        {
            lastCp = cp;
            GameAudio.Play(Sfx.Checkpoint);
            FxBurst.Spawn(cp.transform.position + Vector3.up * 1.5f, World.accent, 10);
            player.RewindCharges = player.MaxRewindCharges;
            ShowToast("CHECKPOINT");
        }

        public void CollectShard(int v)
        {
            RunShards += v;
            GameAudio.Play(Sfx.Shard, 0.8f, 1f + 0.05f * (RunShards % 5));
        }

        public void PuzzleSolved()
        {
            SaveSystem.Data.puzzlesSolved++;
            ShowToast("GATE OPEN");
            CameraRig.I?.Shake(0.3f);
            Unlock("puzzler", SaveSystem.Data.puzzlesSolved >= 20);
        }

        public void RoomCleared() // finish portal
        {
            if (State != GameState.Playing) return;
            Win();
        }

        public void OnFlip() { SaveSystem.Data.flips++; Unlock("flipper", SaveSystem.Data.flips >= 25); }
        public void OnRewind() { SaveSystem.Data.rewinds++; Unlock("rewinder", SaveSystem.Data.rewinds >= 15); }

        public void PlayerHurt(bool instant, Vector3 from, bool fromVoid = false)
        {
            if (State != GameState.Playing || hurtCooldown > 0 || player.Rewinding) return;
            hurtCooldown = 1.2f;
            Flawless = false;
            Hearts -= instant ? Hearts : 1;
            GameAudio.Play(Sfx.Hurt);
            CameraRig.I?.Shake(0.8f);
            FxBurst.Spawn(player.transform.position, World.hazard, 14);
            if (fromVoid) VoidWarnings++;
            if (Hearts <= 0) { Lose(); return; }
            Respawn();
            ShowToast(fromVoid ? "THE VOID GOT YOU" : "OUCH");
        }

        void Respawn()
        {
            Vector3 p = lastCp.transform.position + Vector3.up * 1.2f;
            player.Teleport(p, lastCp.facing);
            visual.ResetTrail();
            // Give breathing room from the Void
            voidWall.SetZ(Mathf.Min(voidWall.Z, p.z - 18f));
            player.RewindCharges = player.MaxRewindCharges;
        }

        void Win()
        {
            State = GameState.Won;
            GameInput.Enabled = false;
            LockCursor(false);
            GameAudio.Play(Sfx.Success);
            FxBurst.Spawn(player.transform.position + Vector3.up, World.accent, 16);
            player.Frozen = true;

            var d = SaveSystem.Data;
            d.escapes++;
            d.shards += RunShards; d.totalShards += RunShards;
            if (!d.worldsEscaped.Contains(World.id)) d.worldsEscaped.Add(World.id);

            // Stars: 1 escape, +1 flawless, +1 ≥ 80% shards
            int stars = 1 + (Flawless ? 1 : 0) + (RunShardTotal > 0 && RunShards >= RunShardTotal * 0.8f ? 1 : 0);
            StarsEarned = stars;
            string key = Mode == RunMode.Daily ? DailyKey : "world_" + World.id;
            if (Mode != RunMode.Daily)
            {
                int old = SaveSystem.Stars(World.id);
                if (stars > old) SaveSystem.SetInt(d.worldStars, World.id, stars);
            }
            float prev = SaveSystem.BestTime(key);
            if (prev <= 0 || RunTime < prev)
            {
                NewBest = true;
                SaveSystem.SetFloat(d.bestTimes, key, RunTime);
                if (ghost != null && Mode != RunMode.Daily) { ghost.SaveAsBest(RunTime); SaveSystem.SetInt(d.upgrades, "ghostseed_" + World.id, Seed); }
            }
            if (Mode == RunMode.GhostRace && prev > 0 && RunTime < prev) BeatGhost = true;
            if (Mode == RunMode.Daily) { d.lastDailyDate = DateTime.UtcNow.ToString("yyyyMMdd"); d.lastDailyBest = RunTime; }

            // bonus shards
            int bonus = 10 + stars * 5 + (Mode == RunMode.Daily ? 15 : 0);
            d.shards += bonus; d.totalShards += bonus;
            RunShards += bonus;

            Unlock("first", true);
            Unlock("flawless", Flawless);
            Unlock("threestar", stars == 3);
            Unlock("hoarder", d.totalShards >= 300);
            Unlock("daily", Mode == RunMode.Daily);
            Unlock("ghost", BeatGhost);
            Unlock("allworlds", d.worldsEscaped.Count >= 5);
            SaveSystem.Save();
        }

        void Lose()
        {
            State = GameState.Lost;
            GameInput.Enabled = false;
            LockCursor(false);
            GameAudio.Play(Sfx.Fail);
            player.Frozen = true;
            var d = SaveSystem.Data;
            int keep = Mathf.RoundToInt(RunShards * 0.5f);
            d.shards += keep; d.totalShards += keep;
            RunShards = keep;
            SaveSystem.Save();
        }

        public void Unlock(string id, bool cond)
        {
            if (!cond) return;
            var d = SaveSystem.Data;
            if (d.achievements.Contains(id)) return;
            var a = Catalog.Achievements.Find(x => x.id == id);
            if (a == null) return;
            d.achievements.Add(id);
            d.shards += a.reward; d.totalShards += a.reward;
            ShowToast("ACHIEVEMENT: " + a.name + "  +" + a.reward);
            GameAudio.Play(Sfx.Unlock);
        }

        public void ShowToast(string s) { Toast = s; ToastTime = 2.2f; }

        void OnApplicationPause(bool p) { if (p) SaveSystem.Save(); }
        void OnApplicationQuit() { SaveSystem.Save(); }
    }

    public class MenuCamDrift : MonoBehaviour
    {
        void Update() { transform.rotation = Quaternion.Euler(4 + Mathf.Sin(Time.time * 0.3f) * 2, Mathf.Sin(Time.time * 0.2f) * 8, 0); transform.position = new Vector3(Mathf.Sin(Time.time * 0.15f) * 2, 2, -6); }
    }
}
