using UnityEngine;

namespace Askich
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SynthAudio : MonoBehaviour
    {
        AudioSource source;
        AudioClip shot, hit, dash, upgrade, lose;
        public bool Muted { get; private set; }
        void Awake()
        {
            source = GetComponent<AudioSource>(); source.playOnAwake = false; source.volume = 0.3f;
            shot = Tone("Pulse", 640, 140, 0.075f, 0.2f);
            hit = Tone("Impact", 180, 45, 0.12f, 0.35f);
            dash = Tone("Dash", 160, 750, 0.18f, 0.14f);
            upgrade = Tone("Upgrade", 440, 880, 0.32f, 0.1f);
            lose = Tone("Shutdown", 260, 35, 0.6f, 0.1f);
        }
        static AudioClip Tone(string name, float start, float end, float seconds, float noise)
        {
            const int rate = 22050; int length = (int)(rate * seconds); float phase = 0;
            var samples = new float[length]; var random = new System.Random(name.Length * 7);
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length; phase += Mathf.Lerp(start, end, t) * Mathf.PI * 2 / rate;
                samples[i] = (Mathf.Sin(phase) + ((float)random.NextDouble() * 2 - 1) * noise) * Mathf.Sin(Mathf.Min(t * 25, 1) * Mathf.PI / 2) * (1 - t) * 0.65f;
            }
            var clip = AudioClip.Create(name, length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        public void Toggle() { Muted = !Muted; source.mute = Muted; }
        public void Play(string effect)
        {
            AudioClip clip = effect == "shot" ? shot : effect == "hit" ? hit : effect == "dash" ? dash : effect == "lose" ? lose : upgrade;
            source.PlayOneShot(clip, effect == "shot" ? 0.6f : 1f);
        }
    }
}
