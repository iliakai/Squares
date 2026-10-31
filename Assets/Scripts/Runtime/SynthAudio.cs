using Squares.Simulation;
using UnityEngine;

namespace Squares
{
    public sealed class SynthAudio
    {
        private readonly AudioSource source;
        private readonly AudioClip shot, hit, pickup, hurt, level, dash, overload;
        private float lastShot, lastPickup, lastHit;
        public SynthAudio(GameObject parent)
        {
            source = parent.AddComponent<AudioSource>(); source.spatialBlend = 0; source.volume = .27f;
            shot = Tone("Shot", 240, 65, .065f);
            hit = Tone("Hit", 115, 45, .05f);
            pickup = Tone("Data", 800, 1300, .055f);
            hurt = Tone("Hull", 120, 35, .19f);
            level = Tone("Level", 430, 980, .3f);
            dash = Tone("Dash", 150, 600, .11f);
            overload = Tone("Overload", 90, 430, .5f);
        }
        private AudioClip Tone(string name, float from, float to, float duration)
        {
            const int rate = 22050;
            float[] data = new float[(int)(rate * duration)]; float phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length;
                phase += Mathf.Lerp(from, to, t) * 2 * Mathf.PI / rate;
                data[i] = (Mathf.Sin(phase) + Mathf.Sin(phase * 2) * .18f) * .35f *
                    Mathf.Min(1, t * 30) * (1 - t) * (1 - t);
            }
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }
        public void Event(WorldEvent e, bool enabled)
        {
            if (!enabled) return;
            AudioClip clip = null; float volume = 1;
            switch (e.Kind)
            {
                case EffectKind.Shot:
                    if (Time.unscaledTime - lastShot < .08f) return;
                    lastShot = Time.unscaledTime; clip = shot; volume = .3f; break;
                case EffectKind.Kill:
                    if (Time.unscaledTime - lastHit < .07f) return;
                    lastHit = Time.unscaledTime; clip = hit; volume = .35f; break;
                case EffectKind.Pickup:
                    if (Time.unscaledTime - lastPickup < .11f) return;
                    lastPickup = Time.unscaledTime; clip = pickup; volume = .3f; break;
                case EffectKind.Hurt: clip = hurt; break;
                case EffectKind.Level: clip = level; break;
                case EffectKind.Dash: clip = dash; volume = .7f; break;
                case EffectKind.Overload: clip = overload; break;
                case EffectKind.Boss: clip = hurt; break;
            }
            if (clip != null) source.PlayOneShot(clip, volume);
        }
    }
}
