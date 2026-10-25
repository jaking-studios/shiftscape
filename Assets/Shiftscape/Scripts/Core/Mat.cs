using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    /// <summary>
    /// Material + primitive factory. Everything in Shiftscape is built from Unity primitives at runtime,
    /// so the project needs no imported art and works on every platform out of the box.
    /// Lit materials clone the built-in Default-Material (Standard shader, always in builds).
    /// Glow materials use Sprites/Default (unlit, supports transparency, always included in builds).
    /// </summary>
    public static class Mat
    {
        static Material litBase;
        static Shader glowShader;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Font font;

        static Material LitBase
        {
            get
            {
                if (litBase == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    litBase = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                    Object.Destroy(tmp);
                }
                return litBase;
            }
        }

        public static Material Lit(Color c, float smooth = 0.25f, float metal = 0f)
        {
            string key = "L" + ColorUtility.ToHtmlStringRGBA(c) + smooth.ToString("0.00") + metal.ToString("0.00");
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitBase);
            m.color = c;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            cache[key] = m;
            return m;
        }

        /// <summary>Unlit, optionally transparent material. Reads as "glowing" next to lit geometry.</summary>
        public static Material Glow(Color c)
        {
            string key = "G" + ColorUtility.ToHtmlStringRGBA(c);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            if (glowShader == null) glowShader = Shader.Find("Sprites/Default");
            m = glowShader != null ? new Material(glowShader) : new Material(LitBase);
            m.color = c;
            cache[key] = m;
            return m;
        }

        /// <summary>A fresh (non-shared) glow material, for things that animate their own color.</summary>
        public static Material GlowInstance(Color c)
        {
            if (glowShader == null) glowShader = Shader.Find("Sprites/Default");
            var m = glowShader != null ? new Material(glowShader) : new Material(LitBase);
            m.color = c;
            return m;
        }

        public static Material LitInstance(Color c)
        {
            var m = new Material(LitBase);
            m.color = c;
            return m;
        }

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material m, bool collider = true, string name = "Box")
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = false, string name = null)
        {
            var g = GameObject.CreatePrimitive(t);
            if (name != null) g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        /// <summary>Invisible trigger volume.</summary>
        public static GameObject Trigger(Transform parent, Vector3 pos, Vector3 size, string name = "Trigger")
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            var bc = g.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = true;
            return g;
        }

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { font = null; }
                if (font == null)
                {
                    try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { font = null; }
                }
                return font;
            }
        }

        public static TextMesh Label3D(Transform parent, Vector3 pos, string text, Color c, float size = 0.12f)
        {
            var g = new GameObject("Label");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            var tm = g.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = c;
            var f = Font;
            if (f != null)
            {
                tm.font = f;
                g.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            }
            return tm;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        public static void ClearCache() { cache.Clear(); }
    }
}
