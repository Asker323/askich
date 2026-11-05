using System;
using System.IO;
using UnityEngine;
namespace Askich {
 public enum GroundSound {Grass,Asphalt,Wood,Tile}
 public sealed class DistrictFoley:MonoBehaviour {
  readonly AudioClip[,] steps=new AudioClip[4,3];
  AudioSource feet,door,task,breathing;
  AudioClip creak,cloth,craft,drink,breath,gasp,finish;
  int variation;float breathTimer;bool taskWanted;
  public GroundSound LastSurface {get;private set;}
  public int StepClipCount=>steps.Length;
  public bool TaskActive=>taskWanted;
  public void Initialize(Transform hero){
   transform.SetParent(hero,false);feet=Source("Footsteps",true);door=Source("Door sound",true);door.transform.SetParent(null,true);task=Source("Hands",false);breathing=Source("Breathing",false);
   for(int s=0;s<4;s++)for(int n=0;n<3;n++)steps[s,n]=MakeStep((GroundSound)s,n);
   creak=Make("Hinge and door latch",.75f,(t,n)=>n*.15f+(Mathf.Sin(t*620+t*t*900)+Mathf.Sin(t*301))*.1f);
   cloth=Make("Bandage cloth",.85f,(t,n)=>n*.36f*(.3f+.7f*Mathf.Pow(Mathf.Sin(t*15),2)));
   craft=Make("Wrap and bottle",.85f,(t,n)=>n*.28f*(.3f+.7f*Mathf.Pow(Mathf.Sin(t*11),2))+Mathf.Sin(t*2400)*.04f*Mathf.Exp(-t*18));
   drink=Make("Bottle gulp",.58f,(t,n)=>(Mathf.Sin(t*820+2*Mathf.Sin(t*55))*.13f+n*.22f)*Mathf.Exp(-Mathf.Abs(t-.2f)*13));
   breath=Make("Tired breath",1.25f,(t,n)=>n*.24f*(.6f+.4f*Mathf.Sin(t*7)));
   gasp=Make("Wounded breath",.6f,(t,n)=>n*.4f*Mathf.Exp(-t*3));
   finish=Make("Wrap secured",.22f,(t,n)=>n*.35f*Mathf.Exp(-t*10));
  }
  AudioSource Source(string name,bool spatial){var obj=new GameObject(name);obj.transform.SetParent(transform,false);var source=obj.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=spatial?1:0;source.minDistance=1;source.maxDistance=12;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;return source;}
  static AudioClip Make(string name,float duration,Func<float,float,float> sample){
   const int rate=22050;float[] data=new float[(int)(duration*rate)];int seed=17;foreach(char letter in name)seed=unchecked(seed*31+letter);var random=new System.Random(seed);float filtered=0;
   for(int i=0;i<data.Length;i++){float t=(float)i/rate;filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.24f);float fade=Mathf.Min(1,t/.012f)*Mathf.Min(1,(duration-t)/.03f);data[i]=Mathf.Clamp(sample(t,filtered)*fade,-.8f,.8f);}
   var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
  }
  static AudioClip MakeStep(GroundSound surface,int variant){
   float pitch=1+variant*.07f;return Make(surface+" step "+variant,.28f,(t,n)=>{
    float body=Mathf.Sin(t*(surface==GroundSound.Wood?530:surface==GroundSound.Tile?1400:360)*pitch)*Mathf.Exp(-t*(surface==GroundSound.Wood?16:35));
    float crunch=n*Mathf.Exp(-t*(surface==GroundSound.Grass?13:25));
    return surface==GroundSound.Grass?crunch*.8f:surface==GroundSound.Asphalt?crunch*.5f+body*.1f:surface==GroundSound.Wood?crunch*.25f+body*.22f:crunch*.22f+body*.16f;
   });
  }
  public void Step(GroundSound surface,bool running,bool quiet){LastSurface=surface;feet.pitch=UnityEngine.Random.Range(.96f,1.04f);feet.PlayOneShot(steps[(int)surface,variation++%3],quiet?.2f:running?.65f:.45f);}
  public void Door(Vector3 position,bool opening){door.transform.position=position;door.pitch=opening?1:.83f;door.PlayOneShot(creak,.55f);}
  public void StartTask(bool crafting,bool drinking=false){task.clip=drinking?drink:crafting?craft:cloth;task.loop=true;task.volume=0;taskWanted=true;task.Play();}
  public void StopTask(){taskWanted=false;}
  public void FinishTask(bool drinking){StopTask();if(!drinking)breathing.PlayOneShot(finish,.45f);}
  public void Wound(){breathing.PlayOneShot(gasp,.55f);breathTimer=2;}
  public void TickBody(float stamina,float health,float dt){
   breathTimer-=dt;if((stamina<30||health<35)&&breathTimer<=0){breathTimer=stamina<15?2:3;breathing.PlayOneShot(breath,.55f);}
  }
  void Update(){if(!task)return;task.volume=Mathf.MoveTowards(task.volume,taskWanted?.38f:0,Time.deltaTime*2);if(!taskWanted&&task.volume<=0&&task.isPlaying)task.Stop();}
  public void ExportDemo(string path){
   const int rate=22050;float[] data=new float[rate*9];
   void Put(AudioClip clip,float time,float gain){var samples=new float[clip.samples];clip.GetData(samples,0);int offset=(int)(time*rate);for(int i=0;i<samples.Length&&offset+i<data.Length;i++)data[offset+i]+=samples[i]*gain;}
   for(int s=0;s<4;s++)for(int n=0;n<3;n++)Put(steps[s,n],s*1.5f+n*.42f,.6f);
   Put(creak,6,.55f);Put(cloth,6.9f,.4f);Put(breath,7.7f,.5f);
   using(var stream=new BinaryWriter(File.Create(path))){stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));stream.Write(36+data.Length*2);stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));stream.Write(16);stream.Write((short)1);stream.Write((short)1);stream.Write(rate);stream.Write(rate*2);stream.Write((short)2);stream.Write((short)16);stream.Write(System.Text.Encoding.ASCII.GetBytes("data"));stream.Write(data.Length*2);foreach(float v in data)stream.Write((short)(Mathf.Clamp(v,-1,1)*32767));}
  }
  void OnDestroy(){if(door)Destroy(door.gameObject);foreach(var clip in steps)if(clip)Destroy(clip);foreach(var clip in new[]{creak,cloth,craft,drink,breath,gasp,finish})if(clip)Destroy(clip);}
 }
}
