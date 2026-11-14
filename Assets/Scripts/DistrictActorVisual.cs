using System;
using System.Collections.Generic;
using UnityEngine;

namespace Askich {
 // Presentation only: movement, damage and hearing remain owned by DistrictGame.
 public sealed partial class DistrictActorVisual : MonoBehaviour {
  static AudioClip[] voices;
  readonly Dictionary<string,string> clips=new Dictionary<string,string>();
  Animation animationPlayer;
  AnimationClip crawlerLunge;
  readonly AnimationClip[] provisions=new AnimationClip[3];int provisionMode;float provisionTime;
  AnimationClip windowClimb;float windowClimbPhase=-1;
  float lungeTime;
  AudioSource vocal;
  AudioLowPassFilter occlusion;
  string state;
  float actionRemaining,nextVoice;
  bool zombie,dead,crawler,searching;
  Transform head;
  GameObject nativeAxe,nativeSpear;
  SkinnedMeshRenderer handSkin;int leftGrip=-1,rightGrip=-1;
  int visualVariant;float meshHeight=1,meshBase;
  float hurtFlash;bool paused,tintApplied;Renderer[] bodyRenderers;MaterialPropertyBlock tint;
  int clothingBits=-1;
  public bool UsesAdultModel {get;private set;}
  public int EquippedWeapon {get;private set;}
  public bool ReverseWalk {get;set;}
  public bool Backpedaling=>state=="Walk"&&animationPlayer[clips[state]].speed<0;
  public bool NativeEquipmentReady=>nativeAxe&&nativeSpear;
  Vector3 headScale;
  public Transform Hand {get;private set;}
  public bool Ready=>animationPlayer&&clips.ContainsKey("Idle")&&clips.ContainsKey("Walk")&&clips.ContainsKey("Death");
  public string State=>state;
  public float HeadScaleRatio=>head?head.localScale.x/headScale.x:1;
  public int VoiceCount=>voices==null?0:voices.Length;

  static string ProvisionKey(int mode)=>mode==1?"DrinkProvision":mode==2?"EatProvision":"RestProvision";
  public bool Initialize(bool infected,int variant){
   visualVariant=variant;zombie=infected;crawler=infected&&variant%3==2;
   string name=!infected?"Hero":variant%3==1?"Heavy":variant%3==2?"Crawler":"Lean";
   var prefab=Resources.Load<GameObject>("Survival/Human/"+name);
   if(!prefab)throw new InvalidOperationException("Missing adult character: "+name);UsesAdultModel=true;
   var model=Instantiate(prefab,transform,false);
   animationPlayer=model.GetComponentInChildren<Animation>();
   if(!animationPlayer){Destroy(model);return false;}
   // The survivor is the camera subject; its weapon hand must also animate on cutaway frames.
   animationPlayer.cullingType=infected?AnimationCullingType.BasedOnRenderers:AnimationCullingType.AlwaysAnimate;
   foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=!infected;
   foreach(string key in new[]{"Idle","Walk","Run","Punch","Slash","Stab","HitReact","Death","Duck","Crawl"}){
    var clip=Resources.Load<AnimationClip>("Survival/Human/"+name+"_"+key);if(!clip)continue;animationPlayer.AddClip(clip,key);clips[key]=key;
   }
   if(crawler){
    crawlerLunge=Resources.Load<AnimationClip>("Survival/Human/Crawler_CrawlerLunge");
    if(!crawlerLunge)throw new InvalidOperationException("Missing crawler lunge clip");
    animationPlayer.AddClip(crawlerLunge,"CrawlerLunge");
    clips["CrawlerLunge"]="CrawlerLunge";
   }
   if(!zombie){
    windowClimb=Resources.Load<AnimationClip>("Survival/Human/Hero_WindowClimb");if(!windowClimb)throw new InvalidOperationException("Missing window climb animation");animationPlayer.AddClip(windowClimb,"WindowClimb");clips["WindowClimb"]="WindowClimb";
    for(int i=0;i<3;i++){provisions[i]=Resources.Load<AnimationClip>("Survival/Human/Hero_"+new[]{"DrinkProvision","EatProvision","RestProvision"}[i]);if(!provisions[i])throw new InvalidOperationException("Missing provision animation");animationPlayer.AddClip(provisions[i],ProvisionKey(i+1));clips[ProvisionKey(i+1)]=ProvisionKey(i+1);}
    var search=Resources.Load<AnimationClip>("Survival/Human/Hero_Search");
    if(!search)throw new InvalidOperationException("Missing survivor search clip");
    animationPlayer.AddClip(search,"Search");clips["Search"]="Search";
   }
   foreach(var bone in model.GetComponentsInChildren<Transform>(true)){
    string n=bone.name.ToLowerInvariant();
    if(infected&&n=="tongue"&&bone.GetComponent<SkinnedMeshRenderer>())bone.gameObject.SetActive(false);
    if(n=="head"){head=bone;headScale=bone.localScale;}
    if(n==(infected?"hand.r":"hand.l"))Hand=bone;
    if(!infected&&n=="axe")nativeAxe=bone.gameObject;if(!infected&&n=="spear")nativeSpear=bone.gameObject;
    if(!infected&&Array.IndexOf(new[]{"knife","woodenbat_barbed","woodenbat_saw","pistol","shotgun","guitar","smg","spear","rifle","axe"},n)>=0)bone.gameObject.SetActive(false);
   }
   bodyRenderers=model.GetComponentsInChildren<Renderer>(true);tint=new MaterialPropertyBlock();
   if(!infected){foreach(var renderer in bodyRenderers)if(renderer is SkinnedMeshRenderer skin&&skin.name=="Body"){handSkin=skin;for(int i=0;i<skin.sharedMesh.blendShapeCount;i++){string shape=skin.sharedMesh.GetBlendShapeName(i);if(shape.EndsWith("GripL"))leftGrip=i;if(shape.EndsWith("GripR"))rightGrip=i;}}}
   var firstSkin=model.GetComponentInChildren<SkinnedMeshRenderer>();if(firstSkin){meshHeight=firstSkin.sharedMesh.bounds.size.y;meshBase=firstSkin.sharedMesh.bounds.min.y;}
   if(infected){
    foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
     for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)skin.SetBlendShapeWeight(i,skin.sharedMesh.GetBlendShapeName(i).Contains(variant%3==1?"Heavy":"Gaunt")?80:0);
     if(skin.name=="Shirt")skin.gameObject.SetActive(variant%3!=1);if(skin.name=="Trousers")skin.gameObject.SetActive(variant%3!=2);if(skin.name=="Shorts")skin.gameObject.SetActive(variant%3==2);if(skin.name=="Shoes")skin.gameObject.SetActive(variant%3==0);if(skin.name=="Hair")skin.gameObject.SetActive(variant%3==0);
    }
   }
   PrepareTurning(model);
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
   if(!zombie&&provisionMode>0&&provisionMode<=3&&state==ProvisionKey(provisionMode))provisions[provisionMode-1].SampleAnimation(animationPlayer.gameObject,provisionTime);
   if(windowClimbPhase>=0)windowClimb.SampleAnimation(animationPlayer.gameObject,windowClimbPhase*windowClimb.length);
   if(head)head.localScale=headScale;if(paused)return;ApplyTurning(Time.deltaTime);UpdateHands(Time.deltaTime);hurtFlash=Mathf.Max(0,hurtFlash-Time.deltaTime);if(hurtFlash<=0&&tintApplied)return;tintApplied=hurtFlash<=0;if(bodyRenderers!=null)foreach(var r in bodyRenderers){tint.Clear();tint.SetFloat("_Decay",zombie?.85f:0);tint.SetFloat("_Variant",visualVariant);tint.SetFloat("_PatternHeight",meshHeight);tint.SetFloat("_PatternBase",meshBase);if(hurtFlash>0)tint.SetColor("_Color",new Color(1.35f,.65f,.5f));r.SetPropertyBlock(tint);}}
  public void ApplyClothing(DistrictInventoryData data){
   if(zombie||bodyRenderers==null)return;int bits=(data.shirt?1:0)|(data.trousers?2:0)|(data.shoes?4:0);if(bits==clothingBits)return;clothingBits=bits;
   foreach(var renderer in bodyRenderers){if(renderer.name=="Shirt")renderer.gameObject.SetActive(data.shirt);if(renderer.name=="Trousers")renderer.gameObject.SetActive(data.trousers);if(renderer.name=="Shorts")renderer.gameObject.SetActive(!data.trousers);if(renderer.name=="Shoes")renderer.gameObject.SetActive(data.shoes);}
  }
  static float LocomotionGrip(string key,float phase)=>key=="Run"?60+2*Mathf.Sin(phase*Mathf.PI*2-1):key=="Walk"?46+3*Mathf.Sin(phase*Mathf.PI*2-1):38;
  float LooseGrip=>windowClimbPhase>=0?0:provisionMode>0||searching?0:state=="Punch"?85:LocomotionGrip(state,state!=null&&clips.ContainsKey(state)?animationPlayer[clips[state]].normalizedTime:0);
  void UpdateHands(float dt){if(!handSkin)return;float target=LooseGrip;void Set(int index,float value){if(index>=0)handSkin.SetBlendShapeWeight(index,Mathf.Lerp(handSkin.GetBlendShapeWeight(index),value,1-Mathf.Exp(-18*dt)));}Set(leftGrip,EquippedWeapon>0?100:target);Set(rightGrip,target);}
  public bool Equip(int weapon){bool changed=EquippedWeapon!=weapon;EquippedWeapon=weapon;if(nativeAxe)nativeAxe.SetActive(weapon==1);if(nativeSpear)nativeSpear.SetActive(weapon==2);if(handSkin&&leftGrip>=0&&(changed||weapon>0))handSkin.SetBlendShapeWeight(leftGrip,weapon>0?100:LooseGrip);return weapon==1&&nativeAxe||weapon==2&&nativeSpear;}

  void Play(string key,bool loop,float speed){
   if(!clips.TryGetValue(key,out string name))return;
   var s=animationPlayer[name];s.wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever;s.speed=speed;
   if(state==key)return;
   float phase=state!=null&&(state=="Walk"||state=="Run")&&(key=="Walk"||key=="Run")?Mathf.Repeat(animationPlayer[clips[state]].normalizedTime,1):0;
   s.normalizedTime=phase;animationPlayer.CrossFade(name,loop?(key=="Idle"?.20f:.16f):.055f);state=key;
  }
  public void SetPaused(bool pause){paused=pause;if(animationPlayer)animationPlayer.enabled=!pause;}
  public void SetSurvivalPose(int mode,float progress){
   if(mode==0){if(provisionMode>0&&state!=null&&(state.EndsWith("Provision")||state=="Search")){state=null;actionRemaining=0;}provisionMode=0;return;}
   if(provisionMode!=mode){state=null;Play(mode==4?"Search":ProvisionKey(mode),false,0);}
   provisionMode=mode;provisionTime=Mathf.Clamp01(progress);actionRemaining=.2f;
  }
  public void SetWindowClimb(float phase){windowClimbPhase=phase;if(phase<0){if(state=="WindowClimb"){state=null;actionRemaining=0;}return;}Play("WindowClimb",false,0);actionRemaining=.2f;}
  public void SetSearching(bool active){searching=active;}
  public void Tick(float speed,float dt,float distance,bool clear){
   if(dead)return;
   actionRemaining-=dt;
   if(!zombie&&searching&&actionRemaining<=0){Play("Search",true,1);return;}
   if(actionRemaining<=0&&crawler&&speed<=.1f)Play("Crawl",true,0);
   else if(actionRemaining<=0){bool running=!zombie&&!ReverseWalk&&speed>(state=="Run"?3.0f:3.5f);Play(speed>.1f?(crawler?"Crawl":running?"Run":"Walk"):"Idle",true,speed>.1f?Mathf.Clamp(speed/(crawler?1.1f:zombie?1.45f:running?4.6f:1.8f),.35f,1.9f)*(!zombie&&ReverseWalk?-1:1):1);}
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
  public void AlertCall(){if(zombie&&vocal&&vocal.isActiveAndEnabled&&voices!=null&&voices.Length>0){vocal.PlayOneShot(voices[visualVariant%voices.Length],.8f);nextVoice=4;}}
  public void Hurt(){if(dead)return;hurtFlash=.14f;if(windowClimbPhase>=0)return;if(clips.ContainsKey("HitReact")){state=null;Play("HitReact",false,1.4f);actionRemaining=.26f;}}
  public void Revive(){dead=false;hurtFlash=0;tintApplied=false;actionRemaining=0;state=null;Play(crawler?"Crawl":"Idle",true,crawler?0:1);}
  public void Restore(bool corpse){Revive();if(!corpse)return;Die();var clip=animationPlayer[clips["Death"]];animationPlayer.Play(clips["Death"]);clip.time=clip.length;clip.speed=0;animationPlayer.Sample();}
  public void Die(){if(dead)return;dead=true;state=null;Play("Death",false,1);if(vocal)vocal.Stop();}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public float ArtPhase(string key)=>animationPlayer[clips[key]].normalizedTime;
  public AnimationClip ArtClip(string key)=>animationPlayer[clips[key]].clip;
  public Transform ArtBone(string key){foreach(var bone in animationPlayer.GetComponentsInChildren<Transform>(true))if(bone.name==key)return bone;return null;}
  public void ArtSample(string key,float phase){ArtClip(key).SampleAnimation(animationPlayer.gameObject,ArtClip(key).length*phase);if(handSkin){float weight=LocomotionGrip(key,phase);if(leftGrip>=0)handSkin.SetBlendShapeWeight(leftGrip,EquippedWeapon>0?100:weight);if(rightGrip>=0)handSkin.SetBlendShapeWeight(rightGrip,weight);}}
#endif
 }
}
