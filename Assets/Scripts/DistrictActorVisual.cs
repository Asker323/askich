using System;
using System.Collections.Generic;
using UnityEngine;

namespace Askich {
 // Presentation only: movement, damage and hearing remain owned by DistrictGame.
 public sealed class DistrictActorVisual : MonoBehaviour {
  static AudioClip[] voices;
  readonly Dictionary<string,string> clips=new Dictionary<string,string>();
  Animation animationPlayer;
  AnimationClip crawlerLunge;
  float lungeTime;
  AudioSource vocal;
  AudioLowPassFilter occlusion;
  string state;
  float actionRemaining,nextVoice;
  bool zombie,dead,crawler;
  Transform head;
  GameObject nativeAxe,nativeSpear;
  float hurtFlash;bool paused;Renderer[] bodyRenderers;MaterialPropertyBlock tint;
  public int EquippedWeapon {get;private set;}
  public bool NativeEquipmentReady=>nativeAxe&&nativeSpear;
  Vector3 headScale;
  public Transform Hand {get;private set;}
  public bool Ready=>animationPlayer&&clips.ContainsKey("Idle")&&clips.ContainsKey("Walk")&&clips.ContainsKey("Death");
  public string State=>state;
  public int VoiceCount=>voices==null?0:voices.Length;

  public bool Initialize(bool infected,int variant){
   zombie=infected;crawler=infected&&variant%3==2;
   string name=!infected?"Characters_Matt":variant%3==1?"Zombie_Chubby":"Zombie_Basic";
   var prefab=Resources.Load<GameObject>("Survival/Art/"+name);
   if(!prefab)return false;
   var model=Instantiate(prefab,transform,false);
   animationPlayer=model.GetComponentInChildren<Animation>();
   if(!animationPlayer){Destroy(model);return false;}
   // The survivor is the camera subject; its weapon hand must also animate on cutaway frames.
   if(!infected)animationPlayer.cullingType=AnimationCullingType.AlwaysAnimate;
   foreach(AnimationState s in animationPlayer){string key=s.name.Substring(s.name.LastIndexOf('|')+1);if(!clips.ContainsKey(key))clips[key]=s.name;}
   if(crawler){
    crawlerLunge=Resources.Load<AnimationClip>("Survival/CrawlerLunge");
    if(!crawlerLunge)throw new InvalidOperationException("Missing crawler lunge clip");
    animationPlayer.AddClip(crawlerLunge,"CrawlerLunge");
    clips["CrawlerLunge"]="CrawlerLunge";
   }
   foreach(var bone in model.GetComponentsInChildren<Transform>(true)){
    string n=bone.name.ToLowerInvariant();
    if(n=="head"){head=bone;headScale=bone.localScale;}
    if(n==(infected?"index1.r":"index1.l"))Hand=bone;
    if(!infected&&n=="axe")nativeAxe=bone.gameObject;if(!infected&&n=="spear")nativeSpear=bone.gameObject;
    if(!infected&&Array.IndexOf(new[]{"knife","woodenbat_barbed","woodenbat_saw","pistol","shotgun","guitar","smg","spear","rifle","axe"},n)>=0)bone.gameObject.SetActive(false);
   }
   bodyRenderers=model.GetComponentsInChildren<Renderer>();tint=new MaterialPropertyBlock();
   if(!infected&&bodyRenderers.Length>0)foreach(var tool in new[]{nativeAxe,nativeSpear})if(tool)foreach(var r in tool.GetComponentsInChildren<Renderer>(true)){var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=bodyRenderers[0].sharedMaterial;r.sharedMaterials=materials;}
   Play(crawler?"Crawl":"Idle",true,crawler?0:1);
   animationPlayer[clips[state]].normalizedTime=(variant*.173f)%1;
   if(zombie){
    if(voices==null)voices=Resources.LoadAll<AudioClip>("Survival/ZombieAudio");
    var emitter=new GameObject("Infected voice");emitter.transform.SetParent(transform,false);emitter.transform.localPosition=Vector3.up*1.5f;
    vocal=emitter.AddComponent<AudioSource>();vocal.playOnAwake=false;vocal.spatialBlend=1;vocal.dopplerLevel=0;vocal.rolloffMode=AudioRolloffMode.Linear;vocal.minDistance=1.5f;vocal.maxDistance=23;vocal.volume=.45f;if(voices.Length>0)vocal.clip=voices[variant%voices.Length];vocal.pitch=.88f+(variant%5)*.055f;
    occlusion=emitter.AddComponent<AudioLowPassFilter>();occlusion.cutoffFrequency=12000;
    nextVoice=2+(variant*.83f)%8;
   }
   return Ready;
  }
  void LateUpdate(){
   // Legacy Animation can restore its bind pose when disabled on pause. Keep the committed pose
   // at the same simulation time, including the frame before resuming gameplay.
   if(state=="CrawlerLunge"&&lungeTime>=.12f) crawlerLunge.SampleAnimation(animationPlayer.gameObject,lungeTime);
   if(head)head.localScale=headScale*.84f;if(paused)return;hurtFlash=Mathf.Max(0,hurtFlash-Time.deltaTime);if(bodyRenderers!=null)foreach(var r in bodyRenderers){tint.Clear();if(hurtFlash>0)tint.SetColor("_Color",new Color(1.35f,.65f,.5f));r.SetPropertyBlock(tint);}}
  public bool Equip(int weapon){EquippedWeapon=weapon;if(nativeAxe)nativeAxe.SetActive(weapon==1);if(nativeSpear)nativeSpear.SetActive(weapon==2);return weapon==1&&nativeAxe||weapon==2&&nativeSpear;}
  void Play(string key,bool loop,float speed){
   if(!clips.TryGetValue(key,out string name))return;
   var s=animationPlayer[name];s.wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever;s.speed=speed;
   if(state==key)return;
   s.time=0;animationPlayer.CrossFade(name,.12f);state=key;
  }
  public void SetPaused(bool pause){paused=pause;if(animationPlayer)animationPlayer.enabled=!pause;}
  public void Tick(float speed,float dt,float distance,bool clear){
   if(dead)return;
   actionRemaining-=dt;
   if(actionRemaining<=0&&crawler&&speed<=.1f)Play("Crawl",true,0);
   else if(actionRemaining<=0)Play(speed>.1f?(crawler?"Crawl":speed>3.5f&&!zombie?"Run":"Walk"):"Idle",true,speed>.1f?Mathf.Clamp(speed/(crawler?1.1f:zombie?1.45f:speed>3.5f?5f:2.6f),.35f,1.9f):1);
   if(!zombie)return;
   vocal.volume=Mathf.Lerp(vocal.volume,clear?(distance<6?.55f:.36f):.12f,dt*4);
   occlusion.cutoffFrequency=Mathf.Lerp(occlusion.cutoffFrequency,clear?12000:1000,dt*4);
   nextVoice-=dt;
   if(nextVoice<=0){nextVoice=UnityEngine.Random.Range(6,14);if(distance<22&&!vocal.isPlaying&&voices.Length>0){vocal.clip=voices[UnityEngine.Random.Range(0,voices.Length)];vocal.Play();}}
  }
  public void Attack(int weapon,float duration){
   if(dead)return;
   string key=crawler?"CrawlerLunge":zombie?(clips.ContainsKey("Punch")?"Punch":"Crawl"):weapon==1?"Slash":weapon==2?"Stab":"Punch";
   if(!clips.ContainsKey(key))return;
   if(crawler)lungeTime=0;
   state=null;Play(key,false,animationPlayer[clips[key]].length/duration);actionRemaining=duration;
  }
  public void SampleLunge(float seconds){
   if(state!="CrawlerLunge")return;
   if(!animationPlayer.IsPlaying(clips[state]))animationPlayer.Play(clips[state]);
   var pose=animationPlayer[clips[state]];
   pose.speed=0;
   lungeTime=pose.time=Mathf.Clamp(seconds,0,pose.length);
   actionRemaining=.1f;
  }
  public void Hurt(){if(dead)return;hurtFlash=.14f;if(clips.ContainsKey("HitReact")){state=null;Play("HitReact",false,1.4f);actionRemaining=.26f;}}
  public void Revive(){dead=false;hurtFlash=0;actionRemaining=0;state=null;Play(crawler?"Crawl":"Idle",true,crawler?0:1);}
  public void Restore(bool corpse){Revive();if(!corpse)return;Die();var clip=animationPlayer[clips["Death"]];animationPlayer.Play(clips["Death"]);clip.time=clip.length;clip.speed=0;animationPlayer.Sample();}
  public void Die(){if(dead)return;dead=true;state=null;Play("Death",false,1);if(vocal)vocal.Stop();}
 }
}
