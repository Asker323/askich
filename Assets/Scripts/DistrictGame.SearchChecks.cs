#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckSearchBehavior(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_SEARCH_CHECK");Directory.CreateDirectory(folder);
   testSave=Path.Combine(Application.temporaryCachePath,"search-"+Guid.NewGuid()+".json");Application.runInBackground=true;playing=true;paused=true;
   foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}
   var results=new List<string>();void Check(bool ok,string text){results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines(Path.Combine(folder,"search-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(text);}}
   var guard=infected[0];
   void Reset(Vector2 position){
    guard.hp=guard.maxHp;guard.root.gameObject.SetActive(true);guard.visual.Revive();guard.p=position;guard.root.position=V(position);guard.root.rotation=Quaternion.identity;
    guard.striking=false;guard.stagger=guard.recovery=guard.wind=guard.unreachable=0;guard.rest=guard.callCooldown=100;guard.barricadeTarget=guard.barricadeRoute=null;
    guard.searchOrigin=position;StartSearching(guard);hero.p=new Vector2(24,-24);hero.root.position=V(hero.p);health=stamina=100;bleeding=false;infection=0;heroVelocity=Vector2.zero;heroStagger=attack=damageDelay=0;dead=false;
   }
   Reset(new Vector2(-24,-24));Vector2 origin=guard.p;Quaternion facing=guard.root.rotation;
   TickInfected(.2f);Check(guard.p==origin&&Quaternion.Angle(facing,guard.root.rotation)<.1f,"search begins with a listening pause instead of spinning");
   guard.searchPause=0;Check(NextSearchPoint(guard),"search selects a reachable nearby point");
   Vector2 goal=guard.goal;Check(Vector2.Distance(goal,origin)<=3.1f&&guard.path.Count<=14,"search route remains local to the remembered position");
   hero.p=new Vector2(35,-25);ObserveEnemy(guard,.05f);Check(guard.goal==goal&&guard.searchOrigin==origin,"unseen survivor movement cannot redirect search");
   float travelled=0;bool clear=true;for(int i=0;i<50;i++){Vector2 before=guard.p;TickInfected(.05f);travelled+=Vector2.Distance(before,guard.p);clear&=!Blocked(guard.p)&&Vector2.Distance(guard.p,origin)<4.2f;}
   Check(travelled>1&&clear,"infected walks between search points without leaving remembered area or crossing solids");
   Vector2 noise=guard.p+Vector2.left*3;HearNoise(noise,12);Check(guard.awareness==Awareness.Investigate&&guard.goal==noise&&guard.searchOrigin==noise&&guard.path.Count==0,"new sound replaces old search route with the actual sound location");
   guard.p=new Vector2(-24,-24);guard.root.position=V(guard.p);guard.root.rotation=Quaternion.identity;hero.p=guard.p+Vector2.up*2;ObserveEnemy(guard,0);
   Check(guard.awareness==Awareness.Chase&&guard.goal==hero.p,"visible survivor immediately resumes pursuit");HearNoise(guard.p+Vector2.left*3,12);Check(guard.awareness==Awareness.Chase&&guard.goal==hero.p,"visible survivor remains higher priority than a distraction");
   Reset(new Vector2(-24,-24));guard.searchPause=0;NextSearchPoint(guard);guard.striking=true;guard.wind=.4f;guard.strikeDirection=Vector2.down;guard.root.rotation=Quaternion.LookRotation(V(Vector2.down));origin=guard.p;facing=guard.root.rotation;
   TickInfected(.1f);Check(guard.p==origin&&Quaternion.Angle(facing,guard.root.rotation)<.1f,"search cannot move or rotate an infected during a committed attack");
   Reset(new Vector2(-24,-24));var ring=new List<Barrier>();
   foreach(int side in new[]{-1,1}){ring.Add(new Barrier{rect=new Rect(guard.p+new Vector2(side*.8f-.1f,-1),new Vector2(.2f,2))});ring.Add(new Barrier{rect=new Rect(guard.p+new Vector2(-1,side*.8f-.1f),new Vector2(2,.2f))});}barriers.AddRange(ring);
   Check(!NextSearchPoint(guard)&&guard.path.Count==0,"enclosed infected rejects inaccessible search points with a bounded path budget");
   origin=guard.p;for(int i=0;i<100;i++)TickInfected(.05f);Check(guard.p==origin,"failed search cannot escape through a solid enclosure");ObserveEnemy(guard,2);Check(guard.awareness==Awareness.Roam,"unsuccessful search still expires instead of tracking forever");foreach(var b in ring)barriers.Remove(b);
   var home=homes[5];home.open=false;SnapDoor(home);Reset(home.p+Vector2.down*6);hero.p=home.p+Vector2.down*5;ObserveEnemy(guard,0);Vector2 last=guard.goal;hero.p=home.p+Vector2.down*2;ObserveEnemy(guard,.05f);
   Check(guard.awareness==Awareness.Investigate&&guard.goal==last,"closed doorway breaks pursuit and preserves last seen position");
   guard.p=last;guard.root.position=V(last);ObserveEnemy(guard,.05f);Check(guard.awareness==Awareness.Search,"arrival at lost trail enters local search");
   hero.p=ResolveSolidPosition(home.p+new Vector2(.2f,-1.4f));hero.root.position=V(hero.p);zoom=8;progression.Data.minute=990;TickWorldLife(0);noticeTime=0;
   paused=false;yield return new WaitForSeconds(.2f);yield return CaptureGameCamera(Path.Combine(folder,"search-start.png"),8);
   origin=guard.p;travelled=0;float elapsed=0;clear=true;
   while(elapsed<3.2f){Vector2 before=guard.p;yield return null;elapsed+=FrameDelta;travelled+=Vector2.Distance(guard.p,before);clear&=!Blocked(guard.p)&&guard.awareness!=Awareness.Chase;}
   Check(travelled>.8f&&clear&&health==100,"live Player search moves outside a closed house without seeing or damaging the hidden survivor");
   yield return CaptureGameCamera(Path.Combine(folder,"search-after.png"),8);paused=true;
   Save();Check(Load()&&guard.awareness==Awareness.Roam&&guard.searchClock==0&&guard.searchPause==0&&guard.searchStep==0,"save load preserves world and clears transient search state");
   Check(FindPath(new Vector2(54,0),new Vector2(68,0)).Count>0,"normal navigation retains its full budget across the district seam");
   Application.Quit();
  }
 }
}
#endif
