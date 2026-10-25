using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Shiftscape
{
    [Serializable]
    public class KeyInt { public string key; public int value; public KeyInt(string k, int v) { key = k; value = v; } }

    [Serializable]
    public class KeyFloat { public string key; public float value; public KeyFloat(string k, float v) { key = k; value = v; } }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int shards;
        public int totalShards;
        public List<KeyInt> upgrades = new List<KeyInt>();
        public List<KeyInt> worldStars = new List<KeyInt>();
        public List<KeyFloat> bestTimes = new List<KeyFloat>();
        public List<string> owned = new List<string> { "suit:classic", "hat:none", "trail:none" };
        public List<string> achievements = new List<string>();
        public List<string> worldsEscaped = new List<string>();
        public string suit = "classic", hat = "none", trail = "none";
        public int flips, rewinds, puzzlesSolved, runs, escapes;
        public string lastDailyDate = "";
        public float lastDailyBest = 0f;
        public string lastDailyRewardDate = "";

        // settings
        public float lookSensitivity = 1f;
        public bool invertY = false;
        public int touchMode = 0; // 0 auto, 1 on, 2 off
        public bool calmMode = false;
        public float volume = 0.8f;
        public float musicVolume = 0.5f;
        public bool highQuality = true;
        public bool showFps = false;
        public bool ghostEnabled = true;
    }

    /// <summary>Local JSON save in persistentDataPath (works on PC, Android, iOS and WebGL/IndexedDB).
    /// Swap Load/Save bodies for a cloud provider (e.g. Unity Cloud Save) without touching game code.</summary>
    public static class SaveSystem
    {
        public static SaveData Data { get; private set; }
        static string PathFile => Path.Combine(Application.persistentDataPath, "shiftscape_save.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(PathFile))
                {
                    Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFile));
                }
            }
            catch (Exception e) { Debug.LogWarning("Shiftscape: save load failed, starting fresh. " + e.Message); }
            if (Data == null) Data = new SaveData();
            if (Data.owned == null) Data.owned = new List<string>();
            foreach (var d in new[] { "suit:classic", "hat:none", "trail:none" })
                if (!Data.owned.Contains(d)) Data.owned.Add(d);
        }

        public static void Save()
        {
            try { File.WriteAllText(PathFile, JsonUtility.ToJson(Data)); }
            catch (Exception e) { Debug.LogWarning("Shiftscape: save failed " + e.Message); }
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.Save(); // nudges IndexedDB flush on WebGL
#endif
        }

        public static void Wipe() { Data = new SaveData(); Save(); }

        public static int GetInt(List<KeyInt> list, string key) { var e = list.Find(x => x.key == key); return e != null ? e.value : 0; }
        public static void SetInt(List<KeyInt> list, string key, int v) { var e = list.Find(x => x.key == key); if (e != null) e.value = v; else list.Add(new KeyInt(key, v)); }
        public static float GetFloat(List<KeyFloat> list, string key) { var e = list.Find(x => x.key == key); return e != null ? e.value : 0f; }
        public static void SetFloat(List<KeyFloat> list, string key, float v) { var e = list.Find(x => x.key == key); if (e != null) e.value = v; else list.Add(new KeyFloat(key, v)); }

        public static int UpgradeLevel(string id) => GetInt(Data.upgrades, id);
        public static int Stars(string worldId) => GetInt(Data.worldStars, worldId);
        public static int TotalStars { get { int t = 0; foreach (var s in Data.worldStars) t += s.value; return t; } }
        public static float BestTime(string key) => GetFloat(Data.bestTimes, key);
        public static bool Owns(string key) => Data.owned.Contains(key);

        // ---------- Ghost files ----------
        [Serializable]
        public class GhostData { public float interval; public List<Vector3> pos = new List<Vector3>(); public List<float> yaw = new List<float>(); public float time; }

        static string GhostPath(string key) => Path.Combine(Application.persistentDataPath, "ghost_" + key + ".json");

        public static GhostData LoadGhost(string key)
        {
            try { var p = GhostPath(key); if (File.Exists(p)) return JsonUtility.FromJson<GhostData>(File.ReadAllText(p)); }
            catch (Exception e) { Debug.LogWarning("Ghost load failed " + e.Message); }
            return null;
        }

        public static void SaveGhost(string key, GhostData g)
        {
            try { File.WriteAllText(GhostPath(key), JsonUtility.ToJson(g)); }
            catch (Exception e) { Debug.LogWarning("Ghost save failed " + e.Message); }
        }
    }
}
