using UnityEngine;

namespace Shiftscape
{
    /// <summary>Records the player's path at 10 Hz and replays the saved best run as a translucent ghost.</summary>
    public class GhostRecorder
    {
        readonly string key;
        readonly SaveSystem.GhostData rec = new SaveSystem.GhostData { interval = 0.1f };
        readonly SaveSystem.GhostData best;
        float acc, playT;
        Transform ghostT;
        bool enabled;

        public GhostRecorder(string key, bool show, WorldTheme theme)
        {
            this.key = key;
            enabled = show;
            best = SaveSystem.LoadGhost(key);
            if (show && best != null && best.pos.Count > 1)
            {
                var g = new GameObject("Ghost");
                ghostT = g.transform;
                var m = Mat.Glow(new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.35f));
                Mat.Box(ghostT, new Vector3(0, -0.15f, 0), new Vector3(0.62f, 0.7f, 0.42f), m, false);
                Mat.Box(ghostT, new Vector3(0, 0.5f, 0), new Vector3(0.52f, 0.5f, 0.52f), m, false);
                Mat.Box(ghostT, new Vector3(-0.17f, -0.68f, 0), new Vector3(0.24f, 0.4f, 0.28f), m, false);
                Mat.Box(ghostT, new Vector3(0.17f, -0.68f, 0), new Vector3(0.24f, 0.4f, 0.28f), m, false);
                var lbl = Mat.Label3D(ghostT, new Vector3(0, 1.3f, 0), "BEST", theme.accent, 0.1f);
                lbl.gameObject.AddComponent<Billboard>();
            }
        }

        public void Tick(PlayerMotor p, float dt)
        {
            acc += dt; playT += dt;
            if (acc >= rec.interval)
            {
                acc -= rec.interval;
                rec.pos.Add(p.transform.position);
                rec.yaw.Add(p.transform.eulerAngles.y);
            }
            if (ghostT != null && best != null)
            {
                float f = playT / best.interval;
                int i = Mathf.FloorToInt(f);
                if (i >= best.pos.Count - 1) { ghostT.position = best.pos[best.pos.Count - 1]; return; }
                float t = f - i;
                ghostT.position = Vector3.Lerp(best.pos[i], best.pos[i + 1], t);
                ghostT.rotation = Quaternion.Euler(0, Mathf.LerpAngle(best.yaw[i], best.yaw[i + 1], t), 0);
            }
        }

        public void SaveAsBest(float time) { rec.time = time; SaveSystem.SaveGhost(key, rec); }
        public void Destroy() { if (ghostT != null) Object.Destroy(ghostT.gameObject); }
    }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate() { if (Camera.main != null) transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position); }
    }
}
