using UnityEngine;

namespace Askich
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SynthAudio : MonoBehaviour
    {
        AudioSource source, ambience;
        AudioClip shot, hit, dash, upgrade, lose, block, step;
        public bool Muted { get; private set; }
        void Awake()
        {
            source = GetComponent<AudioSource>(); source.playOnAwake = false; source.volume = 0.3f;
            shot = Percussion("Weapon sweep", .18f, .75f, 0);
            hit = Percussion("Wood and mail impact", .17f, .32f, 120);
            block = Percussion("Shield rim", .28f, .15f, 730);
            step = Percussion("Earth footstep", .08f, .16f, 65);
            dash = Tone("Dash", 110, 55, 0.16f, 1.6f);
            upgrade = Tone("Upgrade", 330, 440, 0.32f, 0.05f);
            lose = Tone("Shutdown", 260, 35, 0.6f, 0.1f);
            ambience=gameObject.AddComponent<AudioSource>();ambience.playOnAwake=false;ambience.loop=true;
            const int rate=22050,length=rate*8;var wind=new float[length];var random=new System.Random(73);float filtered=0;
            for(int i=0;i<length;i++){filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.018f);float t=(float)i/length;wind[i]=filtered*.5f*Mathf.Sin(Mathf.PI*t)*Mathf.Sin(Mathf.PI*t);}
            ambience.clip=AudioClip.Create("Wind in the trees",length,1,rate,false);ambience.clip.SetData(wind,0);ambience.Play();
        }
        static AudioClip Percussion(string name,float seconds,float noise,float metal)
        {
            const int rate=22050;var samples=new float[(int)(seconds*rate)];var random=new System.Random(name.Length*13);float filtered=0;
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate,n=(float)random.NextDouble()*2-1;filtered=Mathf.Lerp(filtered,n,.22f);float ring=metal==0?0:(Mathf.Sin(t*metal*6.283f)+Mathf.Sin(t*metal*9.91f)*.35f)*.2f;samples[i]=Mathf.Clamp((filtered*noise+ring)*Mathf.Exp(-t*24)*Mathf.Min(1,t*500),-1,1);}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        public void SetVolumes(float effects,float wind){source.volume=Mathf.Clamp01(effects)*.65f;if(ambience!=null)ambience.volume=Mathf.Clamp01(wind);}
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
        public void Toggle() { Muted = !Muted; source.mute = Muted;if(ambience!=null)ambience.mute=Muted; }
        public void Play(string effect)
        {
            AudioClip clip = effect == "shot" ? shot : effect == "hit" ? hit : effect=="block"?block:effect=="step"?step:effect == "dash" ? dash : effect == "lose" ? lose : upgrade;
            source.pitch=effect=="upgrade"||effect=="lose"?1:Random.Range(.94f,1.06f);
            source.PlayOneShot(clip, effect == "step" ? .4f : 1f);
        }
    }
}
