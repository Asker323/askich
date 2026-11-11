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
  IEnumerator CheckWindowClimb(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_CLIMB_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"zonbee-climb-"+Guid.NewGuid()+".json");Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;playing=true;paused=true;
   foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);var keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=stamina=100;bleeding=false;infection=0;heroStagger=attack=0;RestoreFortifications(0,0);progression.Data.minute=900;TickWorldLife(0);
   var results=new List<string>();void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"climb-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   void Place(Vector2 p){hero.p=p;hero.root.position=V(p);heroVelocity=Vector2.zero;current=RoomAt(p);}
   bool Begin(){paused=false;bool started=StartWindowClimb();paused=true;return started;}
   var w=houseWindows[0];var center=w.p+Vector2.up*.55f;var outside=center+Vector2.right*ClimbOffset;var inside=center-Vector2.right*ClimbOffset;Place(outside);
   Check(!Begin()&&!Climbing,"closed window cannot be traversed");w.state=1;w.angle=30;RefreshWindow(w);Check(!Begin(),"moving sash must fully clear before traversal");w.angle=95;RefreshWindow(w);stamina=11;Check(!Begin()&&stamina==11,"exhausted survivor cannot begin");stamina=100;
   var blocker=new Barrier{rect=new Rect(inside-Vector2.one*.1f,Vector2.one*.2f)};barriers.Add(blocker);Check(!Begin()&&stamina==100,"blocked landing rejects traversal without spending stamina");barriers.Remove(blocker);
   var guard=infected[0];guard.hp=guard.maxHp;guard.p=inside;Check(!Begin(),"living infected occupies landing");guard.hp=0;
   Place(w.p+Vector2.left*.72f);Check(!Begin(),"family cot prevents starting through furniture");Place(outside);
   Check(Begin()&&stamina==88,"clear family window starts and charges exactly twelve stamina");Check(w.sill.active&&Blocked(center),"starting traversal does not remove global sill collision");
   float charged=stamina;Check(!Begin()&&stamina==charged,"repeated climb command cannot charge stamina twice");
   Check(!TryAttack(Vector2.up)&&!TryShove(Vector2.up)&&!ThrowStone()&&!BeginMedical(MedicalAction.Rest),"traversal owns hands and excludes conflicting actions");hero.root.rotation=Quaternion.identity;climbFacing=hero.root.rotation;TickWindowClimb(climbApproach*.5f);AnimateWindowClimb();Check(Quaternion.Angle(hero.root.rotation,climbFacing)>10&&Quaternion.Angle(hero.root.rotation,Quaternion.LookRotation(V(climbTo-climbFrom)))>10,"preparation turns smoothly instead of snapping to the window");ClearWindowClimb(false);
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.C));paused=false;yield return null;yield return null;InputSystem.QueueStateEvent(keys,new KeyboardState());yield return new WaitForSeconds(.6f);paused=true;
   Check(Climbing&&hero.visual.State=="WindowClimb","real C input enters dedicated animation through Update");float time=climbTime;var position=hero.p;yield return new WaitForSeconds(.2f);Check(climbTime==time&&hero.p==position,"pause freezes root and animation time");
   Check(hero.visual.EquippedWeapon==0,"both hands are available during crossing");
   Save();var snapshot=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));Check(!Blocked(new Vector2(snapshot.x,snapshot.z))&&Mathf.Abs(snapshot.x-outside.x)<.01f,"early mid-action save stores safe departure rather than sill");
   paused=false;yield return new WaitForSeconds(3.1f);paused=true;Check(!Climbing&&Vector2.Distance(hero.p,inside)<.02f&&!Blocked(hero.p),"live movement lands inside family home without overlapping furniture");Check(w.sill.active&&Blocked(center),"sill still blocks ordinary movement after landing");
   Check(Begin(),"window traversal works from inside toward outside");paused=false;yield return new WaitForSeconds(3.3f);paused=true;Check(Vector2.Distance(hero.p,outside)<.02f&&!Climbing,"reverse crossing lands on the original outside");
   Place(outside);stamina=100;Begin();TickWindowClimb(climbApproach+ClimbDuration*.6f);AnimateWindowClimb();Save();snapshot=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));Check(!Blocked(new Vector2(snapshot.x,snapshot.z))&&Mathf.Abs(snapshot.x-inside.x)<.01f,"late mid-action save stores safe destination");Check(Load()&&!Climbing&&!Blocked(hero.p)&&Vector2.Distance(hero.p,inside)<.02f,"load discards transient climb and restores safe destination");paused=true;
   Place(outside);Begin();health-=10;heroStagger=.2f;TickWindowClimb(.01f);Check(!Climbing&&hero.p==outside,"hit during approach cancels before committing to sill");heroStagger=0;health=100;
   Begin();TickWindowClimb(climbApproach+ClimbDuration*.45f);guard.hp=guard.maxHp;guard.p=hero.p+Vector2.right;damageDelay=0;BeginEnemyAttack(guard);TickEnemyAttack(guard,EnemyImpact+.01f);guard.hp=0;TickWindowClimb(.1f);Check(Climbing&&health<100&&bleeding,"committed crossing remains vulnerable to wounds");TickWindowClimb(3);Check(!Climbing&&!Blocked(hero.p)&&heroStagger>=.35f,"wounded survivor completes crossing and staggers on clear ground");heroStagger=0;bleeding=false;infection=0;health=100;
   Place(outside);Begin();guard.hp=guard.maxHp;guard.p=inside+Vector2.left;Check(!ActorSpaceFree(guard,guard.p,inside),"landing reservation prevents a pursuer occupying the endpoint mid-climb");guard.hp=0;ClearWindowClimb(true);
   w=houseWindows[1];w.state=2;RefreshWindow(w);center=w.p+Vector2.up*.55f;outside=center+Vector2.right*ClimbOffset;inside=center-Vector2.right*ClimbOffset;Place(outside);stamina=100;Check(Begin(),"broken workshop window can be climbed");paused=false;yield return new WaitForSeconds(3.3f);paused=true;Check(!Climbing&&Vector2.Distance(hero.p,inside)<.02f,"workshop crossing lands safely");Check(w.leaf.gameObject.activeSelf&&!w.glass.activeSelf&&w.sill.active,"traversal preserves broken glass and sill state");
   // Inspect actual skeletal contacts, with the same phase/root mapping used in gameplay.
   Place(outside);stamina=100;Begin();var samples=new List<string>();
   bool feetClear=true;for(int frame=0;frame<=112;frame++){float phase=frame/112f;hero.p=Vector2.Lerp(climbFrom,climbTo,WindowClimbMotion.Travel(phase));hero.root.position=V(hero.p);hero.root.rotation=Quaternion.LookRotation(V(climbTo-climbFrom));hero.visual.ArtSample("WindowClimb",phase);foreach(string bone in new[]{"Foot.L","Foot.R","Toes.L","Toes.R"}){var p=hero.visual.ArtBone(bone).position;if(Mathf.Abs(p.x-w.p.x)<.20f&&p.y<.82f)feetClear=false;}}Check(feetClear,"both feet and toes clear sill throughout all 113 authored frames");
   foreach(float phase in new[]{.30f,.42f,.53f,.66f,.77f}){
    climbTime=climbApproach+ClimbDuration*phase;hero.p=Vector2.Lerp(climbFrom,climbTo,WindowClimbMotion.Travel(phase));AnimateWindowClimb();hero.visual.ArtSample("WindowClimb",phase);yield return new WaitForEndOfFrame();
    foreach(string bone in new[]{"Hand.L","Hand.R","Foot.L","Foot.R"}){var p=hero.visual.ArtBone(bone).position;samples.Add(phase+" "+bone+" "+p.ToString("F3"));if(bone.StartsWith("Foot")&&Mathf.Abs(p.x-w.p.x)<.27f)Check(p.y>.82f,"foot clears sill at phase "+phase+" "+bone);}
    File.WriteAllLines(Path.Combine(folder,"climb-bones.txt"),samples);
    if(phase<=.42f){var hand=hero.visual.ArtBone("Hand.L").position;Check(Mathf.Abs(hand.x-w.p.x)<.12f&&Mathf.Abs(hand.y-.87f)<.12f,"supporting palm stays on sill at phase "+phase);}
    if(phase==.42f||phase==.66f)yield return CaptureGameCamera(Path.Combine(folder,phase==.42f?"climb-support.png":"climb-crossing.png"),3.1f);
   }
   File.WriteAllLines(Path.Combine(folder,"climb-bones.txt"),samples);ClearWindowClimb(true);Check(!Blocked(hero.p),"interrupted diagnostic pose settles to safe side");
   InputSystem.RemoveDevice(keys);Application.Quit();
  }
 }
}
#endif
