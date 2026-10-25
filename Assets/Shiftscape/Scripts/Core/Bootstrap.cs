using UnityEngine;

namespace Shiftscape
{
    /// <summary>Creates the GameManager before the first scene loads, so the game works from any scene
    /// (including a completely empty one). The scene only needs a camera, or not even that.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (GameManager.I != null) return;
            var g = new GameObject("Shiftscape");
            g.AddComponent<GameManager>();
        }
    }
}
