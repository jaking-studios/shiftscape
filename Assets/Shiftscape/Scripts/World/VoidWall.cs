using UnityEngine;

namespace Shiftscape
{
    /// <summary>The Void: a translucent wall that creeps along +Z behind the player. Touch it and you lose a heart
    /// and get pushed to the checkpoint. It pauses while a puzzle is being solved (puzzle rooms are "safe rooms")
    /// so puzzles are about thinking, not panic. Speed scales with world difficulty and room index.</summary>
    public class VoidWall : MonoBehaviour
    {
        public float speed = 1f;
        public bool paused;
        public float Z => transform.position.z;
        Material m; Transform face; float baseAlpha;
        Transform[] motes = new Transform[14];
        Vector3[] moteV = new Vector3[14];

        public static VoidWall Create(Color c, float startZ)
        {
            var g = new GameObject("Void");
            var v = g.AddComponent<VoidWall>();
            v.m = Mat.GlowInstance(c);
            v.baseAlpha = c.a;
            var face = Mat.Box(g.transform, Vector3.zero, new Vector3(140, 90, 0.5f), v.m, false, "Face");
            v.face = face.transform;
            // second layer for depth
            var face2 = Mat.Box(g.transform, new Vector3(0, 0, -2.5f), new Vector3(140, 90, 0.5f), Mat.Glow(new Color(c.r, c.g, c.b, c.a * 0.5f)), false, "Face2");
            var mm = Mat.Glow(new Color(1, 1, 1, 0.5f));
            for (int i = 0; i < v.motes.Length; i++)
            {
                var q = Mat.Prim(PrimitiveType.Cube, g.transform, new Vector3(Random.Range(-14f, 14f), Random.Range(-2f, 10f), Random.Range(0.5f, 3f)), Vector3.one * Random.Range(0.15f, 0.5f), mm);
                v.motes[i] = q.transform; v.moteV[i] = Random.insideUnitSphere;
            }
            var trig = g.AddComponent<BoxCollider>(); trig.isTrigger = true; trig.size = new Vector3(140, 90, 1.2f); trig.center = new Vector3(0, 0, 0.3f);
            g.transform.position = new Vector3(0, 0, startZ);
            return v;
        }

        public void JumpTo(float z) { var p = transform.position; p.z = Mathf.Max(p.z, z); transform.position = p; }
        public void SetZ(float z) { var p = transform.position; p.z = z; transform.position = p; }

        void Update()
        {
            if (!paused && GameManager.I != null && GameManager.I.State == GameState.Playing)
                transform.position += Vector3.forward * speed * Time.deltaTime;
            if (PlayerMotor.I != null)
            {
                var p = transform.position; p.x = PlayerMotor.I.transform.position.x; p.y = PlayerMotor.I.transform.position.y + 20f; transform.position = p;
                float dist = PlayerMotor.I.transform.position.z - Z;
                float a = baseAlpha * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(dist / 30f));
                float pulse = 1 + 0.08f * Mathf.Sin(Time.time * 3f);
                var c = m.color; c.a = Mathf.Clamp01(a * pulse); m.color = c;
            }
            for (int i = 0; i < motes.Length; i++)
            {
                motes[i].localPosition += moteV[i] * Time.deltaTime * 1.5f;
                var lp = motes[i].localPosition;
                if (lp.y > 12) lp.y = -2; if (lp.y < -3) lp.y = 11;
                if (Mathf.Abs(lp.x) > 15) lp.x = -lp.x * 0.9f;
                lp.z = Mathf.Clamp(lp.z, 0.5f, 3f);
                motes[i].localPosition = lp;
                motes[i].Rotate(40 * Time.deltaTime, 60 * Time.deltaTime, 0);
            }
        }

        void OnTriggerStay(Collider o)
        {
            if (o.GetComponent<PlayerMotor>() != null && !paused) GameManager.I?.PlayerHurt(false, transform.position, true);
        }
    }
}
