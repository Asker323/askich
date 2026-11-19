#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckStorageTransitions(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_STORAGE_TRANSITION_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"transition-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;playing=true;paused=false;foreach(var d in InputSystem.devices.ToArray())if(d is Keyboard||d is Mouse)InputSystem.DisableDevice(d);
   var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=stamina=100;attack=heroStagger=0;heroVelocity=Vector2.zero;bleeding=false;infection=0;weapon=0;
   var checks=new List<string>();void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines(Path.Combine(folder,"storage-transition-checks.txt"),checks);if(!ok){Application.Quit(1);throw new Exception(label);}}
   var h=homes[5];h.open=true;SnapDoor(h);
   for(int slot=1;slot<=2;slot++){
    CancelSearch();h.taken[slot]=false;RefreshContainers(h);hero.p=StorageStand(h,slot);heroVelocity=Vector2.zero;yield return new WaitForSeconds(.3f);hero.root.rotation=Quaternion.Euler(0,180,0);current=h;
    ApproachStorage(h,slot);yield return null;
    Check(searchSlot<0&&!containerAudio.Searching,"facing away does not start rummage or its sound: "+slot);
    float deadline=Time.time+2;bool smooth=true;float turned=0;var previous=hero.root.rotation;
    while(approachHome!=null&&Time.time<deadline){yield return null;float angle=Quaternion.Angle(previous,hero.root.rotation);smooth&=angle<=215*Time.deltaTime+1;turned+=angle;previous=hero.root.rotation;}
    Check(smooth&&turned>150&&searchSlot==slot,"180 degree turn advances gradually before search: "+slot);
    Check(Vector3.Angle(hero.root.forward,Vector3.forward)<4,"search starts facing the container: "+slot);
    yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();Check(hero.visual.ArtContactWeight>.95f,"contact reaches the working pose: "+slot);
    if(slot==2)yield return CaptureGameCamera(Path.Combine(folder,"before-release.png"),3);
    CancelSearch();yield return null;yield return new WaitForEndOfFrame();
    Check(hero.visual.ArtContactWeight>0&&hero.visual.ArtContactWeight<1&&!containerAudio.Searching,"cancel stops sound immediately while posture starts releasing: "+slot);
    float previousWeight=hero.visual.ArtContactWeight;bool decreasing=true;deadline=Time.time+.4f;
    while(Time.time<deadline){yield return null;yield return new WaitForEndOfFrame();float weight=hero.visual.ArtContactWeight;decreasing&=weight<=previousWeight+.001f;previousWeight=weight;}
    Check(decreasing&&hero.visual.ArtContactWeight==0&&hero.visual.State=="Idle","release completes in the normal idle animation: "+slot);
    if(slot==2)yield return CaptureGameCamera(Path.Combine(folder,"after-release.png"),3);
   }
   hero.p=StorageStand(h,2);h.taken[2]=false;BeginSearch(h,2);yield return new WaitForSeconds(.25f);var start=hero.p;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));yield return new WaitForSeconds(.35f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   Check(Vector2.Distance(start,hero.p)>.25f&&searchSlot<0&&hero.visual.ArtContactWeight==0,"keyboard movement interrupts search without waiting for the visual release");
   hero.p=StorageStand(h,2);heroVelocity=Vector2.zero;h.taken[2]=false;BeginSearch(h,2);yield return new WaitForSeconds(.25f);TryAttack(Vector2.down);yield return null;yield return new WaitForEndOfFrame();
   Check(attack>0&&hero.visual.State=="Punch"&&hero.visual.ArtContactWeight==0,"attack takes priority over the previous contact pose");
   yield return new WaitForSeconds(1);hero.p=StorageStand(h,2);h.taken[2]=false;BeginSearch(h,2);yield return new WaitForSeconds(SearchDuration(h,2)+.35f);
   Check(h.taken[2]&&hero.visual.ArtContactWeight==0&&hero.visual.State=="Idle","completed search also returns to idle without a retained crouch");
   Save();Check(Load(),"save remains loadable after turn and release transitions");
   h.taken[2]=false;BeginSearch(h,2);yield return new WaitForSeconds(.25f);Save();Check(Load()&&hero.visual.ArtContactWeight==0,"loading clears an unfinished visual release instead of carrying its pose into another state");InputSystem.RemoveDevice(keyboard);Application.Quit();
  }
 }
}
#endif
