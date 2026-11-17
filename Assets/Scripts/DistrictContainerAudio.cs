using System;
using UnityEngine;
namespace Askich {
 // Authored procedural foley, not field recordings. Each audible rummage is a game noise event.
 public sealed class DistrictContainerAudio:MonoBehaviour {
  AudioSource source;readonly AudioClip[,] beats=new AudioClip[3,3];AudioClip latch;
  bool searching,automatic;int material,variation;float clock;
  public bool Searching=>searching;
  public int PulseCount {get;private set;}
  public string LastClip=>source&&source.clip?source.clip.name:"";
  public bool Audible=>source&&source.isPlaying;
  void Awake(){
   source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.minDistance=1;source.maxDistance=16;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;
   for(int m=0;m<3;m++)for(int v=0;v<3;v++)beats[m,v]=Make(m,v);
   latch=Make(1,3);
  }
  static AudioClip Make(int material,int variant){
   const int rate=22050;float duration=variant==3?.16f:material==2?.62f:material==1?.48f:.4f;
   var data=new float[(int)(rate*duration)];var random=new System.Random(117+material*41+variant*13);float smooth=0;
   for(int i=0;i<data.Length;i++){
    float t=(float)i/rate;float raw=(float)random.NextDouble()*2-1;smooth=Mathf.Lerp(smooth,raw,material==0?.12f:.25f);
    float fade=Mathf.Min(1,t/.012f)*Mathf.Min(1,(duration-t)/.06f);
    float drag=Mathf.Sin(Mathf.PI*t/duration);float impact=Mathf.Exp(-t*32)+.55f*Mathf.Exp(-Mathf.Abs(t-.23f)*65);
    float ring=material==2?(Mathf.Sin(t*(710+variant*41)*6.283f)*.11f+Mathf.Sin(t*1133*6.283f)*.055f)*impact:
     material==1?Mathf.Sin(t*(115+variant*17)*6.283f)*.16f*impact:0;
    data[i]=Mathf.Clamp((smooth*(material==0?.35f:.42f)*drag*(.6f+.4f*Mathf.Sin(t*34))+ring)*fade,-.8f,.8f);
   }
   string name=variant==3?"Drawer latch":new[]{"Cloth rustle","Wood scrape","Metal rattle"}[material]+" "+variant;
   var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
  }
  public void StartStorage(Vector3 position,float noise){
   source.Stop();source.transform.position=position;source.loop=false;searching=true;automatic=false;clock=0;variation=0;PulseCount=0;
   material=noise>=9?2:noise<=3?0:1;source.volume=material==2?.7f:material==1?.42f:.24f;
  }
  public void Pulse(){if(!searching)return;source.pitch=1+(variation%3-1)*.025f;source.clip=beats[material,variation++%3];source.Play();PulseCount++;}
  public void SetSearching(bool active){if(!active&&searching){searching=false;automatic=false;source.Stop();}}
  // The garden cache uses its existing timing; household storage drives Pulse explicitly.
  public void Play(Vector3 position,float noise,bool complete){
   if(!complete){StartStorage(position,noise);automatic=true;Pulse();return;}
   searching=automatic=false;source.Stop();source.transform.position=position;source.loop=false;source.clip=latch;source.pitch=1;source.volume=.2f;source.Play();
  }
  void Update(){if(!automatic||!searching||AudioListener.pause)return;clock+=Time.deltaTime;if(clock>=.75f){clock%=.75f;Pulse();}}
  void OnDestroy(){foreach(var clip in beats)if(clip)Destroy(clip);if(latch)Destroy(latch);}
 }
}
