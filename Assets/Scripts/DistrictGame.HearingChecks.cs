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
  IEnumerator CheckHearing(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_HEARING_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"hearing-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;playing=true;paused=true;foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}var guard=infected[0];guard.hp=guard.maxHp;guard.root.gameObject.SetActive(true);guard.visual.Revive();health=stamina=100;bleeding=false;infection=0;heroVelocity=Vector2.zero;heroStagger=attack=damageDelay=0;
   var results=new List<string>();void Check(bool ok,string text){results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines(Path.Combine(folder,"hearing-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(text);}}
   void Listen(Vector2 position,Vector2 source,float radius){guard.p=position;guard.root.position=V(position);guard.awareness=Awareness.Roam;guard.path.Clear();guard.rest=guard.stagger=100;guard.recovery=0;guard.striking=false;HearNoise(source,radius);}
   Vector2 a=new Vector2(-24,-24),b=a+Vector2.up*4;hero.p=new Vector2(24,-24);hero.root.position=V(hero.p);
   Check(Mathf.Abs(NoiseDistance(a,b)-4)<.01f,"open ground preserves the normal sound range");
   var wall=new Barrier{rect=new Rect(a+new Vector2(-1,1.9f),new Vector2(2,.2f)),soundLoss=6};barriers.Add(wall);Check(Mathf.Abs(NoiseDistance(a,b)-10)<.01f,"solid wall attenuates a crossing sound ray");
   var overlap=new Barrier{rect=wall.rect,soundLoss=3};barriers.Add(overlap);Check(Mathf.Abs(NoiseDistance(a,b)-10)<.01f,"overlapping wall pieces count as one obstacle");
   var second=new Barrier{rect=new Rect(a+new Vector2(-1,2.9f),new Vector2(2,.2f)),soundLoss=6};barriers.Add(second);Check(Mathf.Abs(NoiseDistance(a,b)-16)<.01f,"separate walls add attenuation independently");
   wall.active=overlap.active=second.active=false;Check(Mathf.Abs(NoiseDistance(a,b)-4)<.01f,"inactive obstacles do not muffle a sound");barriers.Remove(wall);barriers.Remove(overlap);barriers.Remove(second);
   Listen(a+Vector2.up*3,a,FootstepNoise(false,true));Check(guard.awareness==Awareness.Roam,"quiet steps do not alert a listener three metres away");
   Listen(a+Vector2.up*3,a,FootstepNoise(false,false));Check(guard.awareness==Awareness.Investigate,"ordinary footsteps alert a nearby listener");
   Listen(a+Vector2.up*8,a,FootstepNoise(false,false));Check(guard.awareness==Awareness.Roam,"ordinary footsteps do not reach a distant listener");
   Listen(a+Vector2.up*8,a,FootstepNoise(true,false));Check(guard.awareness==Awareness.Investigate,"running reaches farther than walking");
   var keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();
   for(int mode=0;mode<2;mode++){
    hero.p=a;hero.root.position=V(a);heroVelocity=Vector2.zero;stepTime=0;Listen(a+Vector2.right*3.1f,a,0);guard.root.rotation=Quaternion.LookRotation(V(Vector2.right));guard.callCooldown=100;
    paused=false;InputSystem.QueueStateEvent(keys,mode==0?new KeyboardState(Key.W,Key.LeftCtrl):new KeyboardState(Key.W));
    yield return new WaitForSeconds(.82f);paused=true;InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;
    Check(Vector2.Distance(hero.p,a)>.7f&&guard.awareness==(mode==0?Awareness.Roam:Awareness.Investigate),mode==0?"real crouched keyboard movement stays unheard behind the listener":"real walking footsteps alert a nearby listener who cannot see the survivor");
   }
   InputSystem.RemoveDevice(keys);
   var home=homes[5];Vector2 source=home.p+Vector2.down*2,listenerPosition=home.p+Vector2.down*6;home.open=false;SnapDoor(home);
   Listen(listenerPosition,source,6);Check(guard.awareness==Awareness.Roam,"closed front door muffles a moderate indoor noise");
   home.open=true;SnapDoor(home);Listen(listenerPosition,source,6);Check(guard.awareness==Awareness.Investigate&&guard.goal==source,"open doorway passes the same noise and keeps its source location");
   home.open=false;SnapDoor(home);Listen(listenerPosition,source,14);Check(guard.awareness==Awareness.Investigate,"loud noise remains audible through a closed door");
   var exit=backExits[0];exit.open=false;exit.angle=0;exit.gate.active=true;float closed=NoiseDistance(exit.p+Vector2.down*1.5f,exit.p+Vector2.up*1.5f);exit.open=true;exit.angle=100;exit.gate.active=false;
   Check(NoiseDistance(exit.p+Vector2.down*1.5f,exit.p+Vector2.up*1.5f)<closed-2.9f,"rear doorway uses its actual blocking state");
   var window=houseWindows[0];source=window.p+Vector2.left*1.5f;listenerPosition=window.p+Vector2.right*1.5f;window.state=0;window.angle=0;RefreshWindow(window);Listen(listenerPosition,source,4);
   Check(guard.awareness==Awareness.Roam,"closed glass muffles a quiet sound through the window");window.state=1;window.angle=95;RefreshWindow(window);Listen(listenerPosition,source,4);
   Check(guard.awareness==Awareness.Investigate,"open window passes sound without the low sill acting as a full wall");window.state=2;window.angle=0;RefreshWindow(window);Listen(listenerPosition,source,4);
   Check(guard.awareness==Awareness.Investigate,"broken glass no longer muffles sound");
   guard.path.Add(guard.p+Vector2.up);guard.repath=.5f;HearNoise(source,4);Check(guard.path.Count==1&&guard.repath==.5f,"repeated stationary noise refreshes interest without rebuilding the same path");
   guard.p=a;guard.root.rotation=Quaternion.identity;hero.p=a+Vector2.up;ObserveEnemy(guard,0);HearNoise(a+Vector2.left*3,14);Check(guard.awareness==Awareness.Chase&&guard.goal==hero.p,"visible survivor keeps priority over acoustic distractions");
   home.open=false;SnapDoor(home);source=home.p+home.containers[1];listenerPosition=source+Vector2.up*4;hero.p=ResolveSolidPosition(source+Vector2.left);hero.root.position=V(hero.p);current=home;
   Listen(listenerPosition,source,0);guard.callCooldown=100;BeginSearch(home,1);Check(searchSlot==1&&guard.awareness==Awareness.Roam,"actual kitchen search is muffled by the intact exterior wall");
   zoom=8;progression.Data.minute=990;TickWorldLife(0);noticeTime=0;paused=false;yield return new WaitForSeconds(1.1f);Check(searchSlot==1&&guard.awareness==Awareness.Roam,"repeated search noise stays muffled in the running Player");
   yield return CaptureGameCamera(Path.Combine(folder,"quiet-search.png"),8);paused=true;CancelSearch();HearNoise(source,26);Check(guard.awareness==Awareness.Investigate&&guard.goal==source,"loud sound crosses the same wall and attracts the listener");
   Save();home.open=true;SnapDoor(home);window.state=0;RefreshWindow(window);Check(Load()&&!home.open&&window.state==2,"save load restores acoustic door and window states");
   Check(NoiseDistance(home.p+Vector2.down*2,home.p+Vector2.down*6)>6&&NoiseDistance(window.p+Vector2.left*1.5f,window.p+Vector2.right*1.5f)<4,"restored barriers immediately affect hearing correctly");
   Application.Quit();
  }
 }
}
#endif
