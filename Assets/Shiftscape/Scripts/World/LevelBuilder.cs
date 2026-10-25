using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    /// <summary>Output of a built room: where it starts/ends along Z and its checkpoint.</summary>
    public class RoomInfo
    {
        public RoomKind kind;
        public float zStart, zEnd;
        public Checkpoint checkpoint;
        public bool isPuzzle;
        public Transform root;
    }

    /// <summary>
    /// Seeded procedural level generator. A run is a chain of rooms along +Z; each room is one of RoomKind.
    /// The same seed always produces the same level, which is what makes Daily Shift and ghost races fair.
    /// </summary>
    public class LevelBuilder
    {
        readonly WorldTheme theme;
        readonly System.Random rng;
        readonly Transform root;
        readonly float diff;
        Material mGround, mTrim, mAccent, mHazard, mDecor, mGlowAccent, mGlowHazard, mWall;
        public readonly List<RoomInfo> rooms = new List<RoomInfo>();
        public int shardCount;
        public Vector3 spawn;
        public Vector3 finish;
        float z;
        int cpIndex;
        const float W = 12f; // room half-width

        public LevelBuilder(WorldTheme t, int seed, int roomCount, float difficultyBoost = 0f)
        {
            theme = t; rng = new System.Random(seed);
            diff = Mathf.Clamp01(t.difficulty + difficultyBoost);
            root = new GameObject("Level").transform;
            mGround = Mat.Lit(t.ground, 0.2f);
            mTrim = Mat.Lit(t.trim, 0.5f);
            mAccent = Mat.Lit(t.accent, 0.6f);
            mHazard = Mat.Lit(t.hazard, 0.6f);
            mDecor = Mat.Lit(t.decor, 0.3f);
            mWall = Mat.Lit(Color.Lerp(t.ground, t.decor, 0.5f), 0.15f);
            mGlowAccent = Mat.Glow(t.accent);
            mGlowHazard = Mat.Glow(new Color(t.hazard.r, t.hazard.g, t.hazard.b, 0.75f));

            BuildStart();
            RoomKind? last = null;
            for (int i = 0; i < roomCount; i++)
            {
                var k = t.PickRoom(rng, last);
                last = k;
                Build(k, i);
            }
            BuildFinish();
            BuildBackdrop();
        }

        public Transform Root => root;

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        int RI(int a, int b) => rng.Next(a, b); // [a, b)
        bool Chance(float p) => rng.NextDouble() < p;

        GameObject Floor(float zFrom, float zTo, float halfW, float y = 0, Material m = null)
        {
            float len = zTo - zFrom;
            var f = Mat.Box(root, new Vector3(0, y - 0.5f, zFrom + len / 2), new Vector3(halfW * 2, 1f, len), m ?? mGround, true, "Floor");
            return f;
        }

        void Walls(float zFrom, float zTo, float halfW, float h = 6f)
        {
            float len = zTo - zFrom;
            Mat.Box(root, new Vector3(-halfW - 0.5f, h / 2 - 0.5f, zFrom + len / 2), new Vector3(1, h, len), mWall, true, "WallL");
            Mat.Box(root, new Vector3(halfW + 0.5f, h / 2 - 0.5f, zFrom + len / 2), new Vector3(1, h, len), mWall, true, "WallR");
            // trim strip
            Mat.Box(root, new Vector3(-halfW - 0.45f, 0.1f, zFrom + len / 2), new Vector3(0.9f, 0.2f, len), mGlowAccent, false);
            Mat.Box(root, new Vector3(halfW + 0.45f, 0.1f, zFrom + len / 2), new Vector3(0.9f, 0.2f, len), mGlowAccent, false);
        }

        void Pillars(float zFrom, float zTo, float halfW)
        {
            for (float zz = zFrom + 3; zz < zTo - 1; zz += R(5f, 8f))
            {
                float x = Chance(0.5f) ? -halfW + 1.2f : halfW - 1.2f;
                float h = R(3f, 7f);
                var p = Mat.Box(root, new Vector3(x, h / 2, zz), new Vector3(1.2f, h, 1.2f), mDecor, true, "Pillar");
                Mat.Box(root, new Vector3(x, h + 0.2f, zz), new Vector3(1.5f, 0.4f, 1.5f), mTrim, true);
            }
        }

        void PlaceShard(Vector3 p, int value = 1)
        {
            var g = Mat.Prim(PrimitiveType.Cube, root, p, Vector3.one * (value > 1 ? 0.6f : 0.42f), value > 1 ? Mat.Glow(Mat.Hex("#ffd34d")) : Mat.Glow(Mat.Hex("#7cf7ff")), false, "Shard");
            g.transform.rotation = Quaternion.Euler(45, 0, 45);
            var c = g.AddComponent<SphereCollider>(); c.isTrigger = true; c.radius = 1.0f;
            var s = g.AddComponent<Shard>(); s.value = value;
            shardCount += value;
        }

        Checkpoint PlaceCheckpoint(Vector3 pos)
        {
            var g = new GameObject("Checkpoint");
            g.transform.SetParent(root, false); g.transform.position = pos;
            var ring = Mat.Prim(PrimitiveType.Cylinder, g.transform, Vector3.zero, new Vector3(2.6f, 0.06f, 2.6f), Mat.Glow(new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.6f)));
            Mat.Box(g.transform, new Vector3(-1.2f, 1.6f, 0), new Vector3(0.2f, 3.2f, 0.2f), mTrim, true);
            Mat.Box(g.transform, new Vector3(1.2f, 1.6f, 0), new Vector3(0.2f, 3.2f, 0.2f), mTrim, true);
            Mat.Box(g.transform, new Vector3(0, 3.2f, 0), new Vector3(2.6f, 0.2f, 0.2f), mTrim, true);
            var light = Mat.Box(g.transform, new Vector3(0, 3.2f, 0), new Vector3(0.6f, 0.4f, 0.4f), Mat.GlowInstance(theme.accent), false);
            light.AddComponent<Pulse>();
            var tr = g.AddComponent<BoxCollider>(); tr.isTrigger = true; tr.size = new Vector3(5, 4, 1.5f); tr.center = new Vector3(0, 2, 0);
            var cp = g.AddComponent<Checkpoint>(); cp.index = cpIndex++; cp.facing = Vector3.forward;
            return cp;
        }

        void Hazards(Vector3 center, Vector3 size, bool spikes = true)
        {
            var g = Mat.Box(root, center, size, mGlowHazard, false, "Hazard");
            var bc = g.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(1, 1.6f, 1); bc.center = new Vector3(0, 0.3f, 0);
            g.AddComponent<Hazard>();
            if (spikes)
            {
                int n = Mathf.Clamp(Mathf.RoundToInt(size.x * size.z / 1.5f), 2, 24);
                for (int i = 0; i < n; i++)
                {
                    var sp = Mat.Prim(PrimitiveType.Cube, root, center + new Vector3(R(-size.x / 2, size.x / 2), size.y / 2 + 0.15f, R(-size.z / 2, size.z / 2)), new Vector3(0.25f, 0.5f, 0.25f), mHazard, false);
                    sp.transform.rotation = Quaternion.Euler(0, 45, 0);
                }
            }
        }

        void Kill(float zFrom, float zTo, float halfW, float y = -6f)
        {
            // Invisible kill-volume under gaps.
            var g = Mat.Trigger(root, new Vector3(0, y, (zFrom + zTo) / 2), new Vector3(halfW * 2 + 20, 2, zTo - zFrom + 4), "Pit");
            g.AddComponent<Hazard>().instantKill = false;
        }

        // ---------------------------------------------------------------- Rooms

        void BuildStart()
        {
            z = 0;
            Floor(-8, 14, W);
            Walls(-8, 14, W);
            spawn = new Vector3(0, 1.2f, -2);
            Mat.Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.03f, -2), new Vector3(3, 0.06f, 3), mGlowAccent);
            var cp = PlaceCheckpoint(new Vector3(0, 0, 10));
            cp.done = true;
            var tm = Mat.Label3D(root, new Vector3(0, 4.5f, 13.4f), theme.displayName.ToUpper(), theme.trim, 0.2f);
            tm.transform.rotation = Quaternion.Euler(0, 180, 0);
            rooms.Add(new RoomInfo { kind = RoomKind.PlatformHop, zStart = -8, zEnd = 14, checkpoint = cp });
            z = 14;
        }

        void BuildFinish()
        {
            float z0 = z;
            Floor(z0, z0 + 16, W);
            Walls(z0, z0 + 16, W, 8);
            finish = new Vector3(0, 0, z0 + 10);
            // portal
            var g = new GameObject("Portal"); g.transform.SetParent(root, false); g.transform.position = finish;
            var ring = Mat.Prim(PrimitiveType.Cylinder, g.transform, new Vector3(0, 2.4f, 0), new Vector3(4.4f, 0.3f, 4.4f), Mat.GlowInstance(theme.accent));
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0); ring.AddComponent<Pulse>();
            var inner = Mat.Prim(PrimitiveType.Cylinder, g.transform, new Vector3(0, 2.4f, 0), new Vector3(3.6f, 0.1f, 3.6f), Mat.Glow(new Color(1, 1, 1, 0.35f)));
            inner.transform.localRotation = Quaternion.Euler(90, 0, 0);
            inner.AddComponent<Spinner>().speed = 40f;
            var tr = g.AddComponent<BoxCollider>(); tr.isTrigger = true; tr.size = new Vector3(4, 5, 1); tr.center = new Vector3(0, 2.4f, 0);
            var gate = g.AddComponent<ExitGate>(); gate.open = true; gate.Setup(null, null);
            g.AddComponent<FinishMarker>();
            for (int i = 0; i < 4; i++) PlaceShard(new Vector3(-3 + i * 2, 1.2f, z0 + 4), 1);
            var tm = Mat.Label3D(root, new Vector3(0, 6f, z0 + 10f), "ESCAPE", theme.trim, 0.3f);
            tm.transform.rotation = Quaternion.Euler(0, 180, 0);
            z = z0 + 16;
        }

        void Build(RoomKind k, int i)
        {
            var info = new RoomInfo { kind = k, zStart = z };
            var rg = new GameObject("Room_" + i + "_" + k).transform; rg.SetParent(root, false);
            info.root = rg;
            switch (k)
            {
                case RoomKind.PlatformHop: PlatformHop(); break;
                case RoomKind.SpinnerGauntlet: SpinnerGauntlet(); break;
                case RoomKind.CrumbleBridge: CrumbleBridge(); break;
                case RoomKind.GravityHall: GravityHall(); break;
                case RoomKind.CrateAndPlate: CrateAndPlate(info); break;
                case RoomKind.MemoryPads: MemoryPads(info); break;
                case RoomKind.CodeVault: CodeVault(info); break;
                case RoomKind.BounceTowers: BounceTowers(); break;
            }
            // Connector + checkpoint
            Floor(z, z + 8, W);
            Walls(z, z + 8, W);
            info.checkpoint = PlaceCheckpoint(new Vector3(0, 0, z + 4));
            if (Chance(0.5f)) PlaceShard(new Vector3(R(-6, 6), 1.2f, z + 6));
            z += 8;
            info.zEnd = z;
            rooms.Add(info);
        }

        void PlatformHop()
        {
            int n = 6 + Mathf.RoundToInt(diff * 5);
            float gap = Mathf.Lerp(3.2f, 4.6f, diff);
            float z0 = z;
            float x = 0, y = 0;
            Floor(z0, z0 + 3, W);
            z += 3;
            for (int i = 0; i < n; i++)
            {
                float size = Mathf.Lerp(3.2f, 1.8f, diff) + R(-0.3f, 0.3f);
                x = Mathf.Clamp(x + R(-3.5f, 3.5f), -W + 2, W - 2);
                y = Mathf.Clamp(y + R(-1.2f, 1.6f), 0, 4.5f);
                var p = Mat.Box(root, new Vector3(x, y - 0.3f, z + gap), new Vector3(size, 0.6f, size), i % 3 == 2 ? mAccent : mTrim, true, "Plat");
                if (diff > 0.3f && Chance(0.3f * diff))
                {
                    var mp = p.AddComponent<MovingPlatform>();
                    mp.a = p.transform.position; mp.b = mp.a + new Vector3(R(-3, 3), 0, 0); mp.period = R(2f, 3.5f); mp.phase = R(0, 5);
                }
                if (Chance(0.35f)) PlaceShard(new Vector3(x, y + 1.2f, z + gap));
                z += gap;
            }
            Kill(z0, z + 2, W);
            // optional high-road bonus shard
            PlaceShard(new Vector3(R(-6, 6), 6.5f, (z0 + z) / 2), 3);
            z += 2;
        }

        void SpinnerGauntlet()
        {
            float z0 = z; float len = 26 + diff * 10;
            Floor(z0, z0 + len, W);
            Walls(z0, z0 + len, W);
            int n = 3 + Mathf.RoundToInt(diff * 3);
            for (int i = 0; i < n; i++)
            {
                float zz = z0 + 5 + i * (len - 8) / n;
                float x = R(-4, 4);
                var hub = Mat.Prim(PrimitiveType.Cylinder, root, new Vector3(x, 0.6f, zz), new Vector3(1, 1.2f, 1), mDecor, true, "Hub");
                var spin = hub.AddComponent<Spinner>(); spin.speed = (Chance(0.5f) ? 1 : -1) * Mathf.Lerp(70, 150, diff) * R(0.8f, 1.2f);
                float armLen = Mathf.Lerp(6, 9, diff);
                var arm = Mat.Box(hub.transform, new Vector3(0, 0.3f, 0), new Vector3(armLen, 0.4f, 0.5f), mHazard, false, "Arm");
                arm.transform.localScale = new Vector3(armLen, 0.35f, 0.5f);
                var bc = arm.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(1, 1.4f, 1);
                arm.AddComponent<Hazard>();
                if (Chance(0.6f)) PlaceShard(new Vector3(x + R(-2, 2), 1.2f, zz + R(-1, 1)));
            }
            // low barriers to jump
            for (int i = 0; i < 2; i++) Mat.Box(root, new Vector3(R(-6, 6), 0.4f, z0 + R(6, len - 6)), new Vector3(R(3, 6), 0.8f, 0.6f), mTrim, true, "Bar");
            z = z0 + len;
        }

        void CrumbleBridge()
        {
            float z0 = z;
            Floor(z0, z0 + 3, W); z += 3;
            int rows = 7 + Mathf.RoundToInt(diff * 5);
            float tile = 2.2f;
            int lanes = 3;
            for (int r = 0; r < rows; r++)
            {
                for (int l = 0; l < lanes; l++)
                {
                    if (Chance(0.18f + 0.2f * diff) && !(r == 0)) continue; // missing tile
                    float x = (l - 1) * tile * 1.1f + R(-0.2f, 0.2f);
                    var t = Mat.Box(root, new Vector3(x, -0.25f, z + tile / 2), new Vector3(tile, 0.5f, tile), Chance(0.7f) ? mTrim : mAccent, true, "Tile");
                    var c = t.AddComponent<CrumbleTile>(); c.delay = Mathf.Lerp(0.9f, 0.45f, diff) * R(0.8f, 1.2f);
                    if (Chance(0.12f)) PlaceShard(new Vector3(x, 1.2f, z + tile / 2));
                }
                z += tile * 1.05f;
            }
            Kill(z0, z + 2, W);
            z += 2;
        }

        void GravityHall()
        {
            // Walk on the floor, hit a flipper, run on the ceiling past floor hazards, flip back.
            float z0 = z; float len = 30 + diff * 8; float h = 7f;
            Floor(z0, z0 + len, W);
            Walls(z0, z0 + len, W, h + 1);
            // ceiling
            Mat.Box(root, new Vector3(0, h + 0.5f, z0 + len / 2), new Vector3(W * 2, 1, len), mDecor, true, "Ceiling");
            // hazards on floor in middle section
            for (int i = 0; i < 3 + Mathf.RoundToInt(diff * 3); i++)
                Hazards(new Vector3(R(-8, 8), 0.1f, z0 + R(8, len - 8)), new Vector3(R(3, 6), 0.2f, R(2, 4)));
            // ceiling blocks to dodge (they hang down into your head space while inverted)
            for (int i = 0; i < 2 + Mathf.RoundToInt(diff * 3); i++)
                Mat.Box(root, new Vector3(R(-8, 8), h - 0.9f, z0 + R(10, len - 10)), new Vector3(R(2, 4), 1.8f, 1.5f), mWall, true, "Stalactite");
            // flipper pads
            FlipPad(new Vector3(0, 0.1f, z0 + 4), Vector3.up);
            FlipPad(new Vector3(0, h - 0.1f, z0 + len - 4), Vector3.down);
            // shards on ceiling
            for (int i = 0; i < 4; i++) PlaceShard(new Vector3(R(-7, 7), h - 1.3f, z0 + R(8, len - 8)));
            Kill(z0, z0 + len, W, -8);
            z = z0 + len;
        }

        void FlipPad(Vector3 pos, Vector3 dir)
        {
            var g = Mat.Prim(PrimitiveType.Cylinder, root, pos, new Vector3(3.2f, 0.2f, 3.2f), Mat.GlowInstance(Mat.Hex("#b98bff")), false, "FlipPad");
            g.AddComponent<Pulse>();
            var arrow = Mat.Box(g.transform, new Vector3(0, dir.y > 0 ? 2.5f : -2.5f, 0), new Vector3(0.2f, 10f, 0.2f), Mat.Glow(new Color(0.72f, 0.55f, 1f, 0.5f)), false);
            var tr = g.AddComponent<BoxCollider>(); tr.isTrigger = true; tr.size = new Vector3(1, 12, 1);
            var f = g.AddComponent<GravityFlipper>(); f.dir = dir;
        }

        void CrateAndPlate(RoomInfo info)
        {
            info.isPuzzle = true;
            float z0 = z; float len = 24;
            Floor(z0, z0 + len, W); Walls(z0, z0 + len, W);
            var pz = new GameObject("CratePuzzle"); pz.transform.SetParent(root, false); pz.transform.position = new Vector3(0, 0, z0 + len / 2);
            var cp = pz.AddComponent<CratePuzzle>();
            int n = 2 + (diff > 0.5f ? 1 : 0);
            for (int i = 0; i < n; i++)
            {
                var plate = Mat.Box(root, new Vector3(R(-7, 7), 0.06f, z0 + R(len * 0.55f, len - 5)), new Vector3(2.2f, 0.12f, 2.2f), Mat.LitInstance(theme.hazard), false, "Plate");
                var tr = plate.AddComponent<BoxCollider>(); tr.isTrigger = true; tr.size = new Vector3(1, 8, 1);
                cp.plates.Add(plate.AddComponent<PressurePlate>());
                var crate = Mat.Box(root, new Vector3(R(-7, 7), 0.8f, z0 + R(4, len * 0.45f)), new Vector3(1.5f, 1.5f, 1.5f), Mat.Lit(Color.Lerp(theme.accent, theme.decor, 0.4f), 0.4f), true, "Crate");
                crate.AddComponent<PushCrate>();
            }
            // a couple of obstacles
            for (int i = 0; i < 3; i++) Mat.Box(root, new Vector3(R(-9, 9), 0.5f, z0 + R(5, len - 5)), new Vector3(R(1, 2.5f), 1f, R(1, 2.5f)), mDecor, true, "Block");
            cp.status = Mat.Label3D(root, new Vector3(0, 4f, z0 + len - 0.6f), "0 / " + n, theme.trim, 0.2f);
            cp.status.transform.rotation = Quaternion.Euler(0, 180, 0);
            cp.gate = Gate(new Vector3(0, 0, z0 + len - 0.5f));
            z = z0 + len;
        }

        ExitGate Gate(Vector3 pos)
        {
            var g = new GameObject("Gate"); g.transform.SetParent(root, false); g.transform.position = pos;
            var mat = Mat.LitInstance(theme.hazard);
            var bars = new GameObject("Bars").transform; bars.SetParent(g.transform, false);
            for (int i = 0; i < 7; i++) Mat.Box(bars, new Vector3(-9 + i * 3, 3, 0), new Vector3(0.35f, 6, 0.35f), mat, true, "Bar");
            Mat.Box(bars, new Vector3(0, 6.1f, 0), new Vector3(W * 2, 0.4f, 0.4f), mat, true);
            Mat.Box(g.transform, new Vector3(0, 6.4f, 0), new Vector3(W * 2, 0.4f, 0.6f), mTrim, true);
            var tr = g.AddComponent<BoxCollider>(); tr.isTrigger = true; tr.size = new Vector3(W * 2, 6, 1.5f); tr.center = new Vector3(0, 3, 1);
            var gate = g.AddComponent<ExitGate>(); gate.Setup(bars, mat);
            return gate;
        }

        void MemoryPads(RoomInfo info)
        {
            info.isPuzzle = true;
            float z0 = z; float len = 22;
            Floor(z0, z0 + len, W); Walls(z0, z0 + len, W);
            var pz = new GameObject("MemoryPuzzle"); pz.transform.SetParent(root, false); pz.transform.position = new Vector3(0, 0, z0 + len / 2);
            var mp = pz.AddComponent<MemoryPuzzle>();
            mp.rounds = 2 + Mathf.RoundToInt(diff * 2);
            Vector3[] spots = { new Vector3(-6, 0, z0 + 8), new Vector3(-2, 0, z0 + 14), new Vector3(2, 0, z0 + 14), new Vector3(6, 0, z0 + 8) };
            for (int i = 0; i < 4; i++)
            {
                var pad = Mat.Prim(PrimitiveType.Cylinder, root, spots[i] + new Vector3(0, 0.08f, 0), new Vector3(3, 0.16f, 3), Mat.GlowInstance(Color.Lerp(mp.colors[i], Color.black, 0.7f)), false, "Pad" + i);
                var tr = pad.AddComponent<BoxCollider>(); tr.isTrigger = true; tr.size = new Vector3(0.8f, 20, 0.8f);
                var pt = pad.AddComponent<PadTrigger>(); pt.owner = mp; pt.index = i;
                mp.pads.Add(pad.transform); mp.padMats.Add(pad.GetComponent<Renderer>().sharedMaterial);
                // tall beacon so you can see which pad lights from anywhere
                var beacon = Mat.Box(root, spots[i] + new Vector3(0, 3.5f, 0), new Vector3(0.3f, 7, 0.3f), mp.padMats[i], false);
            }
            var lbl = Mat.Label3D(root, new Vector3(0, 4.5f, z0 + len - 0.6f), "WATCH", theme.trim, 0.2f);
            lbl.transform.rotation = Quaternion.Euler(0, 180, 0);
            mp.Init(rng, lbl);
            mp.gate = Gate(new Vector3(0, 0, z0 + len - 0.5f));
            z = z0 + len;
        }

        void CodeVault(RoomInfo info)
        {
            info.isPuzzle = true;
            float z0 = z; float len = 26;
            Floor(z0, z0 + len, W); Walls(z0, z0 + len, W, 7);
            var pz = new GameObject("CodeVault"); pz.transform.SetParent(root, false); pz.transform.position = new Vector3(0, 0, z0 + len / 2);
            var cv = pz.AddComponent<CodeVaultPuzzle>();
            for (int i = 0; i < 3; i++) cv.code[i] = RI(0, 10);
            // Plaques: digit + colour tag (1st red, 2nd yellow, 3rd blue). Hidden up on ledges / behind pillars.
            Color[] tags = { Mat.Hex("#ff5e3a"), Mat.Hex("#ffd34d"), Mat.Hex("#6ad1ff") };
            for (int i = 0; i < 3; i++)
            {
                Vector3 p;
                if (i == 0) { p = new Vector3(R(-9, 9), 3.5f, z0 + R(4, 10)); Mat.Box(root, p + new Vector3(0, -0.9f, 0), new Vector3(2.5f, 0.4f, 2.5f), mTrim, true, "Ledge"); Mat.Box(root, p + new Vector3(0, -2.6f, -1.6f), new Vector3(1.6f, 0.4f, 1.6f), mTrim, true, "Step"); }
                else if (i == 1) { p = new Vector3(R(-9, 9), 1.4f, z0 + R(10, 18)); Mat.Box(root, p + new Vector3(0, -0.2f, 1.2f), new Vector3(2.6f, 3f, 0.4f), mDecor, true, "Screen"); }
                else { p = new Vector3(0, 5.2f, z0 + len - 3); Mat.Box(root, p + new Vector3(0, -0.9f, 0), new Vector3(3, 0.4f, 3), mTrim, true, "HighLedge"); Mat.Box(root, p + new Vector3(3.5f, -2.6f, 0), new Vector3(2, 0.4f, 2), mTrim, true); Mat.Box(root, p + new Vector3(6.5f, -4.0f, 0), new Vector3(2, 0.4f, 2), mTrim, true); }
                var plaque = Mat.Box(root, p, new Vector3(1.2f, 1.2f, 0.15f), Mat.Glow(tags[i]), false, "Plaque");
                var tm = Mat.Label3D(root, p + new Vector3(0, 0, -0.12f), cv.code[i].ToString(), Color.black, 0.25f);
                tm.transform.rotation = Quaternion.Euler(0, 180, 0);
                var tm2 = Mat.Label3D(root, p + new Vector3(0, 0, 0.12f), cv.code[i].ToString(), Color.black, 0.25f);
            }
            // Vault with 3 dials
            float vz = z0 + len - 1.2f;
            Mat.Box(root, new Vector3(0, 2.5f, vz + 0.4f), new Vector3(8, 5, 0.6f), mDecor, true, "VaultFace");
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var dial = Mat.Prim(PrimitiveType.Cylinder, root, new Vector3(-2.4f + i * 2.4f, 2.2f, vz), new Vector3(1.4f, 0.2f, 1.4f), Mat.Lit(tags[i], 0.6f), true, "Dial");
                dial.transform.rotation = Quaternion.Euler(90, 0, 0);
                var lbl = Mat.Label3D(root, new Vector3(-2.4f + i * 2.4f, 2.2f, vz - 0.25f), "0", Color.black, 0.3f);
                lbl.transform.rotation = Quaternion.Euler(0, 180, 0);
                cv.dialLabels.Add(lbl);
                var it = dial.AddComponent<Interactable>(); it.prompt = "Turn dial"; it.onUse = () => cv.Spin(idx);
            }
            cv.status = Mat.Label3D(root, new Vector3(0, 4.3f, vz - 0.3f), "FIND 3 DIGITS", theme.trim, 0.18f);
            cv.status.transform.rotation = Quaternion.Euler(0, 180, 0);
            cv.gate = Gate(new Vector3(0, 0, z0 + len - 0.5f));
            // leave a walkway past the vault face on both sides (gate bars span full width anyway)
            z = z0 + len;
        }

        void BounceTowers()
        {
            float z0 = z;
            Floor(z0, z0 + 4, W); z += 4;
            int n = 4 + Mathf.RoundToInt(diff * 3);
            for (int i = 0; i < n; i++)
            {
                float x = R(-7, 7);
                float h = 3f + i * 1.3f + R(-0.5f, 0.5f);
                // pad on the ground and a tower to land on
                var padBase = Mat.Prim(PrimitiveType.Cylinder, root, new Vector3(x, 0.15f, z + 1), new Vector3(2.4f, 0.3f, 2.4f), mDecor, true, "PadBase");
                var top = Mat.Prim(PrimitiveType.Cylinder, padBase.transform, new Vector3(0, 1f, 0), new Vector3(0.85f, 0.5f, 0.85f), Mat.Glow(theme.accent), false);
                var tr = padBase.AddComponent<BoxCollider>(); tr.isTrigger = true; tr.size = new Vector3(1, 6, 1); tr.center = new Vector3(0, 3, 0);
                var bp = padBase.AddComponent<BouncePad>(); bp.power = Mathf.Sqrt(2f * 26f * (h + 1.5f)) * 1.05f;
                float tz = z + 1 + R(3.5f, 5.5f); float tx = Mathf.Clamp(x + R(-4, 4), -9, 9);
                Mat.Box(root, new Vector3(tx, h / 2, tz), new Vector3(2.6f, h, 2.6f), i % 2 == 0 ? mTrim : mAccent, true, "Tower");
                if (Chance(0.5f)) PlaceShard(new Vector3(tx, h + 1.2f, tz));
                if (i < n - 1) PlaceShard(new Vector3(x, h * 0.6f + 2, z + 2.5f));
                // next pad sits on top of this tower (except last)
                z = tz + 1.3f;
                if (i < n - 1)
                {
                    var pad2 = Mat.Prim(PrimitiveType.Cylinder, root, new Vector3(tx, h + 0.15f, tz), new Vector3(1.8f, 0.3f, 1.8f), mDecor, true, "PadTop");
                    Mat.Prim(PrimitiveType.Cylinder, pad2.transform, new Vector3(0, 1f, 0), new Vector3(0.85f, 0.5f, 0.85f), Mat.Glow(theme.accent), false);
                    var tr2 = pad2.AddComponent<BoxCollider>(); tr2.isTrigger = true; tr2.size = new Vector3(1, 6, 1); tr2.center = new Vector3(0, 3, 0);
                    float nextH = 3f + (i + 1) * 1.3f;
                    pad2.AddComponent<BouncePad>().power = Mathf.Sqrt(2f * 26f * (Mathf.Max(2f, nextH - h) + 2.5f)) * 1.05f;
                }
                else
                {
                    // slide down to connector
                    for (int s = 0; s < 4; s++) Mat.Box(root, new Vector3(tx + (s + 1) * 1.5f * (tx > 0 ? -1 : 1), h - (s + 1) * h / 5, tz + 1.5f * (s + 1)), new Vector3(2.2f, 0.4f, 2.2f), mTrim, true, "Step");
                    z = tz + 8;
                }
            }
            Kill(z0, z + 2, W);
            z += 2;
        }

        void BuildBackdrop()
        {
            // Far-away floating slabs for depth; cheap, no colliders.
            int n = SaveSystem.Data != null && SaveSystem.Data.highQuality ? 60 : 25;
            var m = Mat.Lit(Color.Lerp(theme.decor, theme.sky, 0.4f), 0.1f);
            var m2 = Mat.Lit(Color.Lerp(theme.ground, theme.sky, 0.25f), 0.1f);
            for (int i = 0; i < n; i++)
            {
                float side = Chance(0.5f) ? -1 : 1;
                var g = Mat.Box(root, new Vector3(side * R(22, 70), R(-30, 25), R(-20, z + 20)), new Vector3(R(4, 16), R(1, 6), R(4, 16)), Chance(0.5f) ? m : m2, false, "Backdrop");
                var f = g.AddComponent<Float>(); f.amp = R(0.3f, 1.2f); f.speed = R(0.2f, 0.6f); f.spin = R(-5, 5);
            }
            // Big floor far below so falling looks like something
            Mat.Box(root, new Vector3(0, -40, z / 2), new Vector3(400, 1, z + 200), Mat.Lit(Color.Lerp(theme.fog, theme.ground, 0.6f), 0.05f), false, "Abyss");
        }
    }

    public class FinishMarker : MonoBehaviour { }
}
