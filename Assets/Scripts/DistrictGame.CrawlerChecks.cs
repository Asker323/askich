#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckCrawler(Action<bool,string> check,string folder){
   foreach(var enemy in infected)enemy.hp=0;
   var crawler=infected.Find(z=>z.id==8);
   Vector2 yard=new Vector2(-16,-24);
   Vector3 forward=cam.transform.right;
   Vector2 direction=new Vector2(forward.x,forward.z);
   var keys=InputSystem.AddDevice<Keyboard>();

   void SetEncounter(){
    hero.p=yard;heroVelocity=Vector2.zero;heroStagger=attack=damageDelay=0;
    health=stamina=100;bleeding=false;infection=drunkSeconds=0;weapon=1;zoom=7;
    crawler.hp=crawler.maxHp;crawler.striking=false;crawler.stagger=crawler.recovery=0;
    crawler.rest=100;crawler.p=yard-direction*1.79f;
    crawler.root.position=V(crawler.p);crawler.root.rotation=Quaternion.LookRotation(V(direction));
    crawler.visual.Revive();
   }
   SetEncounter();
   crawler.stagger=100;
   yield return new WaitForSeconds(.75f);
   crawler.stagger=crawler.rest=0;
   yield return null;
   check(crawler.striking&&crawler.visual.State=="CrawlerLunge","AI selects dedicated crawler lunge clip");
   Vector2 start=crawler.p;
   yield return new WaitForSeconds(.35f);
   check(crawler.p==start&&health==100,"crawler braces without movement or early damage");
   float beforePause=crawler.wind;paused=true;yield return new WaitForSeconds(.16f);
   check(beforePause==crawler.wind&&crawler.p==start,"pause freezes crawler windup");paused=false;
   yield return CaptureGameCamera(Path.Combine(folder,"crawler-brace.png"));
   float braceHand=Vector3.Dot(crawler.visual.Hand.position-crawler.root.position,forward);
   Debug.Log($"Crawler brace: local hand {crawler.root.InverseTransformPoint(crawler.visual.Hand.position)}, yaw {crawler.root.eulerAngles}, heading {crawler.strikeDirection}");
   check(braceHand>-.05f&&braceHand<.70f&&crawler.visual.Hand.position.y<.18f,"planted hand stays low and in front after pause");
   float timeout=Time.time+2;
   while(crawler.striking&&Time.time<timeout)yield return null;
   check(health<=80&&health>79&&bleeding&&infection>=12,"crawler contact inflicts one 20 HP wound");
   check(Vector2.Distance(start,crawler.p)>.7f&&crawler.recovery>.8f,"lunge advances the actor and exposes a recovery window");
   yield return CaptureGameCamera(Path.Combine(folder,"crawler-contact.png"));
   float contactHand=Vector3.Dot(crawler.visual.Hand.position-crawler.root.position,forward);
   Debug.Log($"Crawler hand reach from actor root: brace {braceHand:F3}, contact {contactHand:F3}");
   check(Mathf.Abs(contactHand-Vector2.Distance(crawler.p,hero.p))<.16f,"hand reaches the target at the damage frame");
   check(contactHand>braceHand+.12f,"baked attack reaches forward with the hand at contact");

   SetEncounter();BeginEnemyAttack(crawler);var fixedDirection=crawler.strikeDirection;
   yield return HoldDiagnosticKey(keys,Key.W,.43f);
   timeout=Time.time+2;
   while(crawler.striking&&Time.time<timeout)yield return null;
   check(health==100&&!bleeding&&crawler.strikeDirection==fixedDirection,"keyboard sidestep escapes the locked crawler lunge");
   check(crawler.recovery>.7f&&TryAttack((crawler.p-hero.p).normalized),"axe counter starts during missed lunge recovery");
   yield return new WaitForSeconds(.4f);
   check(crawler.hp==crawler.maxHp-45&&health==100,"axe counter connects before crawler can retaliate");
   yield return CaptureGameCamera(Path.Combine(folder,"crawler-counter.png"));

   // Two independent fingers go through the live Enhanced Touch path.
   SetEncounter();BeginEnemyAttack(crawler);
   var screen=InputSystem.AddDevice<Touchscreen>();TouchControls.ForceEnabled=true;
   void Finger(int id,Vector2 ui,UnityEngine.InputSystem.TouchPhase phase){
    Vector2 pos=ScreenLayout.Offset+ui*ScreenLayout.Scale;pos.y=Screen.height-pos.y;
    InputSystem.QueueStateEvent(screen,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=id,phase=phase,position=pos,pressure=phase==UnityEngine.InputSystem.TouchPhase.Ended?0:1});
   }
   var stick=new Vector2(115,470);var hit=DistrictTouchControls.Hit.center;
   Finger(1,stick,UnityEngine.InputSystem.TouchPhase.Began);
   yield return new WaitForSeconds(.39f);
   stick=new Vector2(115,520);Finger(1,stick,UnityEngine.InputSystem.TouchPhase.Moved);
   timeout=Time.time+2;while(crawler.striking&&Time.time<timeout)yield return null;
   check(health==100&&Vector2.Distance(yard,hero.p)>.7f,"first touch steers clear of crawler reach");
   check(heroVelocity.magnitude>.05f&&heroVelocity.magnitude<.65f,"small stick offset permits a controlled slow sidestep");
   Finger(2,hit,UnityEngine.InputSystem.TouchPhase.Began);yield return null;
   check(touch.move.sqrMagnitude>0&&touch.hit&&attack>0,"second finger starts counter while movement stick remains held");
   Finger(1,stick,UnityEngine.InputSystem.TouchPhase.Ended);Finger(2,hit,UnityEngine.InputSystem.TouchPhase.Ended);
   yield return new WaitForSeconds(.4f);
   check(crawler.hp==crawler.maxHp-45,"touch counter targets reachable crawler after lateral dodge");
   TouchControls.ForceEnabled=false;InputSystem.RemoveDevice(screen);

   var home=homes[5];home.open=false;SnapDoor(home);
   var doorway=home.p+Vector2.down*4;
   crawler.p=doorway+Vector2.down*.9f;hero.p=doorway+Vector2.up*.75f;
   crawler.recovery=crawler.stagger=0;damageDelay=0;BeginEnemyAttack(crawler);
   face=Vector2.left;check(TouchStrikeDirection()==face,"touch aim ignores enemy behind shut door");
   for(int i=0;i<17;i++)TickEnemyAttack(crawler,.05f);
   check(crawler.p.y<doorway.y&&health==100,"closed door blocks crawler travel and prevents contact");
   InputSystem.RemoveDevice(keys);
  }
 }
}
#endif
