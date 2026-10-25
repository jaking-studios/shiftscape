using System.Collections.Generic;
using UnityEngine;

namespace Shiftscape
{
    public enum Sfx { Jump, DoubleJump, Land, Shard, Dash, Flip, Hurt, Rewind, Click, Success, Fail, Checkpoint, Bounce, Unlock, Wrong, Crumble, Pad0, Pad1, Pad2, Pad3 }

    /// <summary>Every sound in the game is synthesised at startup, so there are no audio assets to manage.</summary>
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio I { get; private set; }
        const int SR = 22050;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        AudioSource[] voices;
        int nextVoice;
        AudioSource music;
        int musicRoot = -1;

        void Awake()
        {
            I = this;
            voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            Build();
        }

        void Update()
        {
            var d = SaveSystem.Data;
            if (d == null) return;
            music.volume = d.musicVolume * d.volume * 0.35f;
        }

        public static void Play(Sfx s, float vol = 1f, float pitch = 1f)
        {
            if (I == null || !I.clips.TryGetValue(s, out var c)) return;
            var v = I.voices[I.nextVoice];
            I.nextVoice = (I.nextVoice + 1) % I.voices.Length;
            v.pitch = pitch;
            v.PlayOneShot(c, vol * (SaveSystem.Data != null ? SaveSystem.Data.volume : 1f));
        }

        public static void PlayMusic(int root)
        {
            if (I == null) return;
            if (I.musicRoot == root && I.music.isPlaying) return;
            I.musicRoot = root;
            I.music.clip = I.MakeMusic(root);
            I.music.Play();
        }

        public static void StopMusic() { if (I != null) I.music.Stop(); }

        static float Midi(float n) => 440f * Mathf.Pow(2f, (n - 69f) / 12f);

        delegate float Gen(float t, float len);

        AudioClip Clip(string name, float len, Gen g)
        {
            int n = Mathf.CeilToInt(len * SR);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(g(i / (float)SR, len), -1f, 1f);
            var c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        static float Env(float t, float len, float attack = 0.005f) =>
            Mathf.Clamp01(t / attack) * Mathf.Pow(1f - Mathf.Clamp01(t / len), 2f);

        static float Sq(float ph) => Mathf.Sign(Mathf.Sin(ph)) * 0.6f;

        void Build()
        {
            var rnd = new System.Random(7);
            clips[Sfx.Jump] = Clip("jump", 0.18f, (t, l) => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(300, 700, t / l) * t) * Env(t, l) * 0.5f);
            clips[Sfx.DoubleJump] = Clip("djump", 0.2f, (t, l) => (Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(500, 1100, t / l) * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * 1500 * t)) * Env(t, l) * 0.4f);
            clips[Sfx.Land] = Clip("land", 0.1f, (t, l) => ((float)rnd.NextDouble() * 2 - 1) * Env(t, l) * 0.25f * (1 - t / l));
            clips[Sfx.Shard] = Clip("shard", 0.25f, (t, l) => (Mathf.Sin(2 * Mathf.PI * (t < 0.07f ? 1318 : 1760) * t)) * Env(t, l) * 0.35f);
            clips[Sfx.Dash] = Clip("dash", 0.25f, (t, l) => ((float)rnd.NextDouble() * 2 - 1) * Mathf.Sin(Mathf.PI * t / l) * 0.35f + Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(200, 90, t / l) * t) * Env(t, l) * 0.3f);
            clips[Sfx.Flip] = Clip("flip", 0.45f, (t, l) => Mathf.Sin(2 * Mathf.PI * (400 + 250 * Mathf.Sin(t * 40)) * t) * Env(t, l) * 0.4f);
            clips[Sfx.Hurt] = Clip("hurt", 0.4f, (t, l) => Sq(2 * Mathf.PI * Mathf.Lerp(300, 80, t / l) * t) * Env(t, l) * 0.35f);
            clips[Sfx.Rewind] = Clip("rewind", 0.5f, (t, l) => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(900, 250, t / l) * t * (1 + 0.1f * Mathf.Sin(t * 70))) * Env(t, l, 0.05f) * 0.35f);
            clips[Sfx.Click] = Clip("click", 0.05f, (t, l) => Mathf.Sin(2 * Mathf.PI * 1200 * t) * Env(t, l) * 0.3f);
            clips[Sfx.Checkpoint] = Clip("cp", 0.4f, (t, l) => (Mathf.Sin(2 * Mathf.PI * Midi(t < 0.12f ? 76 : 83) * t)) * Env(t, l) * 0.35f);
            clips[Sfx.Bounce] = Clip("bounce", 0.3f, (t, l) => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(150, 600, Mathf.Sqrt(t / l)) * t) * Env(t, l) * 0.5f);
            clips[Sfx.Unlock] = Clip("unlock", 0.6f, (t, l) => (Mathf.Sin(2 * Mathf.PI * Midi(72) * t) + Mathf.Sin(2 * Mathf.PI * Midi(t > 0.15f ? 79 : 76) * t)) * Env(t, l) * 0.25f);
            clips[Sfx.Wrong] = Clip("wrong", 0.35f, (t, l) => Sq(2 * Mathf.PI * 140 * t) * Env(t, l) * 0.3f);
            clips[Sfx.Crumble] = Clip("crumble", 0.35f, (t, l) => ((float)rnd.NextDouble() * 2 - 1) * Env(t, l) * 0.3f * (0.5f + 0.5f * Mathf.Sin(t * 120)));
            clips[Sfx.Success] = Clip("success", 1.4f, (t, l) =>
            {
                int[] notes = { 72, 76, 79, 84 };
                int idx = Mathf.Min(3, (int)(t / 0.14f));
                float s = Mathf.Sin(2 * Mathf.PI * Midi(notes[idx]) * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * Midi(notes[idx] - 12) * t);
                return s * Env(t, l, 0.01f) * 0.3f;
            });
            clips[Sfx.Fail] = Clip("fail", 1.2f, (t, l) =>
            {
                int[] notes = { 67, 63, 60, 55 };
                int idx = Mathf.Min(3, (int)(t / 0.2f));
                return Sq(2 * Mathf.PI * Midi(notes[idx]) * t) * Env(t, l, 0.01f) * 0.2f;
            });
            int[] pads = { 60, 64, 67, 72 };
            for (int p = 0; p < 4; p++)
            {
                float f = Midi(pads[p]);
                clips[Sfx.Pad0 + p] = Clip("pad" + p, 0.45f, (t, l) => (Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * f * t)) * Env(t, l) * 0.35f);
            }
        }

        AudioClip MakeMusic(int root)
        {
            // 8 bars of a gentle arpeggio + bass, loops seamlessly.
            float bpm = 112f;
            float beat = 60f / bpm;
            int bars = 8;
            float len = bars * 4 * beat;
            int n = Mathf.CeilToInt(len * SR);
            var data = new float[n];
            int[][] chords = { new[] { 0, 4, 7 }, new[] { -3, 0, 4 }, new[] { 5, 9, 12 }, new[] { 7, 11, 14 } };
            float step = beat / 2f;
            int steps = Mathf.FloorToInt(len / step);
            for (int s = 0; s < steps; s++)
            {
                int bar = (int)(s * step / (4 * beat));
                var ch = chords[bar % 4];
                int note = root + 12 + ch[s % 3] + ((s / 3) % 2 == 0 ? 0 : 12);
                float f = Midi(note);
                int start = Mathf.FloorToInt(s * step * SR);
                int dur = Mathf.FloorToInt(step * 1.8f * SR);
                for (int i = 0; i < dur && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    float e = Mathf.Exp(-t * 7f);
                    data[start + i] += (Mathf.Sin(2 * Mathf.PI * f * t) + 0.25f * Sq(2 * Mathf.PI * f * t)) * e * 0.12f;
                }
                if (s % 4 == 0)
                {
                    float fb = Midi(root - 12 + ch[0]);
                    int bd = Mathf.FloorToInt(beat * 2f * SR);
                    for (int i = 0; i < bd && start + i < n; i++)
                    {
                        float t = i / (float)SR;
                        data[start + i] += Mathf.Sin(2 * Mathf.PI * fb * t) * Mathf.Exp(-t * 2.5f) * 0.18f;
                    }
                }
            }
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var c = AudioClip.Create("music" + root, n, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
