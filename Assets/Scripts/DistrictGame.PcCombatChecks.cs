#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator PreviewPcEncounter(){
   testSave=Path.Combine(Application.temporaryCachePath,"pc-encounter-"+Guid.NewGuid()+".json");Application.runInBackground=true;playing=true;paused=true;
   hero.p=new Vector2(12,-24);weapon=1;bandages=2;sterileBandages=1;food=1;inventory.TryAdd(DistrictItemKind.Water,3);zoom=8;
   Say("Проверочная сцена: топор и припасы выданы для осмотра боя. Пробел — толчок.");yield return null;
  }
  IEnumerator CheckPcCombat(){
   testSave=Path.Combine(Application.temporaryCachePath,"pc-combat-"+Guid.NewGuid()+".json");
   string folder=Environment.GetEnvironmentVariable("BEREZKI_PC_COMBAT_CHECK");Directory.CreateDirectory(folder);
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;playing=true;paused=false;
   var results=new List<string>();
   void Require(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"pc-combat-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   var guard=infected.Find(z=>z.id==0);var heavy=infected.Find(z=>z.id%3==1);var third=infected.Find(z=>z.id!=guard.id&&z.id!=heavy.id&&!IsCrawler(z));
   void Reset(){
    strikeSequence++;ClearPcCombat();foreach(var z in infected){z.hp=0;z.striking=false;z.recovery=z.stagger=0;z.root.gameObject.SetActive(false);}
    hero.p=new Vector2(12,-20);heroVelocity=Vector2.zero;attack=heroStagger=damageDelay=0;health=stamina=100;bleeding=false;infection=drunkSeconds=0;suppliesOpen=false;paused=false;playing=true;dead=false;weapon=1;face=Vector2.up;zoom=7;
    noticeTime=0;guard.root.gameObject.SetActive(true);guard.hp=guard.maxHp;guard.p=hero.p+Vector2.up*1.15f;guard.rest=guard.stagger=100;guard.visual.Revive();guard.root.position=V(guard.p);hero.visual.Revive();
   }
   Reset();yield return new WaitForSeconds(.5f);Vector2 before=guard.p;float hp=guard.hp;
   Require(TryShove(Vector2.up),"shove starts with enough stamina");
   yield return new WaitForSeconds(.12f);Require(guard.p==before&&guard.hp==hp,"shove windup cannot push early");
   yield return new WaitForSeconds(.19f);
   Require(Vector2.Distance(before,guard.p)>.85f&&guard.hp==hp,"shove creates space without free damage");
   Require(stamina>=75&&stamina<=76&&guard.stagger>0,"shove spends 24 stamina and interrupts infected");
   Require(!TryShove(Vector2.up),"shove recovery rejects repeated presses");
   yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"pc-shove.png"),image.EncodeToPNG());Destroy(image);
   Reset();stamina=23;Require(!TryShove(Vector2.up)&&stamina==23,"exhaustion refuses shove without spending stamina");
   Reset();guard.hp=0;heavy.hp=heavy.maxHp;heavy.p=hero.p+Vector2.up*1.15f;before=heavy.p;ResolveStrike(Vector2.up,ShoveStrike());
   Require(Vector2.Distance(before,heavy.p)>.48f&&Vector2.Distance(before,heavy.p)<.60f&&heavy.hp==heavy.maxHp,"heavy infected resists shove displacement without damage reduction changes");
   Reset();heavy.hp=heavy.maxHp;third.hp=third.maxHp;guard.p=hero.p+new Vector2(-.35f,1);heavy.p=hero.p+new Vector2(0,1.2f);third.p=hero.p+new Vector2(.3f,1.35f);before=third.p;
   Require(ResolveStrike(Vector2.up,ShoveStrike())==2&&third.p==before,"shove cannot clear an entire group");
   Reset();var home=homes[6];home.open=false;SnapDoor(home);hero.p=home.p+Vector2.down*4.6f;guard.p=home.p+Vector2.down*3.5f;before=guard.p;
   Require(ResolveStrike(Vector2.up,ShoveStrike())==0&&guard.p==before,"closed clinic door blocks shove");
   Reset();var moved=MoveAmongActors(hero,Vector2.up*3,false);
   Require(moved.y<=guard.p.y-BodySpacing+.01f,"survivor cannot run through a living infected");
   heavy.hp=heavy.maxHp;heavy.p=new Vector2(12,-18);guard.p=new Vector2(11,-18);hero.p=new Vector2(12,-24);moved=MoveAmongActors(guard,Vector2.right*1.1f,true);
   Require(Vector2.Distance(moved,heavy.p)>=BodySpacing-.001f&&(moved-guard.p).magnitude>.5f,"pursuers step around each other without stacking");
   Reset();guard.p=hero.p+Vector2.right*.1f;moved=MoveAmongActors(hero,Vector2.left*.5f,false);
   Require(Vector2.Distance(moved,guard.p)>.59f,"pre-existing overlapping save positions can separate");
   guard.hp=0;moved=MoveAmongActors(hero,Vector2.up*3,false);Require(moved.y>hero.p.y+2.9f,"corpses do not create invisible body barriers");
   Reset();attack=.4f;Require(!RequestPcStrike(Vector2.up,false)&&queuedPcAction==0,"early presses do not queue an automatic combo");
   attack=.1f;Require(RequestPcStrike(Vector2.up,false)&&queuedPcAction==1,"late press enters short recovery buffer");
   attack=0;TickPcCombat(.25f);Require(attack>.8f&&queuedPcAction==0,"buffered attack executes once even across a long frame");
   Reset();attack=.1f;RequestPcStrike(Vector2.up,false);paused=true;yield return null;yield return null;
   Require(queuedPcAction==0,"pause clears buffered action");
   Reset();attack=.1f;RequestPcStrike(Vector2.up,false);suppliesOpen=true;yield return null;yield return null;
   Require(queuedPcAction==0,"inventory clears buffered action");
   Reset();Save();attack=.1f;RequestPcStrike(Vector2.up,false);Require(Load()&&queuedPcAction==0,"loading clears buffered action without a save-format change");
   Reset();attack=.1f;RequestPcStrike(Vector2.up,false);guard.stagger=0;guard.p=hero.p+Vector2.up;guard.recovery=0;BeginEnemyAttack(guard);TickEnemyAttack(guard,EnemyImpact+.01f);
   Require(queuedPcAction==0&&heroStagger>0&&health==74,"real enemy impact interrupts buffer and retains 26 damage");
   Reset();var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;yield return null;
   int sequence=strikeSequence;Require(attack>0&&activeStrike.shove&&stamina<80,"Space input starts live shove through Update");
   yield return new WaitForSeconds(.7f);Require(strikeSequence==sequence&&attack<=0,"holding Space does not repeat shove");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   Reset();var mice=new List<Mouse>();foreach(var device in InputSystem.devices)if(device is Mouse native&&native.enabled){mice.Add(native);InputSystem.DisableDevice(native);}
   var mouse=InputSystem.AddDevice<Mouse>();
   void MouseAt(Vector3 world,bool right,bool left=false){var pos=(Vector2)cam.WorldToScreenPoint(world);var state=new MouseState{position=pos};if(right)state=state.WithButton(MouseButton.Right);if(left)state=state.WithButton(MouseButton.Left);mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,state);}
   yield return new WaitForSeconds(.6f);MouseAt(V(guard.p,.85f),true);yield return new WaitForSeconds(.3f);
   Require(pcAiming&&pcAimCue&&pcAimCue.enabled&&Vector3.Dot(hero.root.forward,V(face))>.95f,"right mouse aims hero and displays weapon reach");
   yield return new WaitForEndOfFrame();image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"pc-aim.png"),image.EncodeToPNG());Destroy(image);
   before=hero.p;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S,Key.LeftShift));yield return new WaitForSeconds(.3f);
   Require(pcAiming&&Vector2.Distance(before,hero.p)>.2f&&heroVelocity.magnitude<2&&hero.visual.Backpedaling,"aimed backstep reverses walking animation and suppresses sprint");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.15f);guard.p=hero.p+face*1.2f;guard.hp=guard.maxHp;guard.stagger=100;guard.root.position=V(guard.p);
   MouseAt(V(guard.p,.85f),true,true);yield return new WaitForSeconds(.1f);MouseAt(V(guard.p,.85f),true);yield return new WaitForSeconds(.3f);
   Require(guard.hp==guard.maxHp-45,"mouse click hits aimed infected at normal axe damage");
   yield return new WaitForSeconds(.6f);
   Reset();guard.rest=guard.stagger=0;MouseAt(V(guard.p,.85f),false);yield return null;yield return null;
   Require(guard.striking&&health==100,"live clinic guard commits to attack before contact");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return new WaitForSeconds(.33f);
   Require(health==100&&guard.hp==guard.maxHp&&!guard.striking&&guard.stagger>0,"Space interrupts live AI windup without inflicting free damage");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.28f);
   before=hero.p;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.LeftShift));yield return new WaitForSeconds(1.05f);
   Require(health==100&&Vector2.Distance(hero.p,before)>4&&Vector2.Distance(hero.p,guard.p)>4,"live shove and sprint escape creates distance without wounds");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());guard.stagger=100;yield return null;
   var ui=ScreenLayout.Offset+new Vector2(200,135)*ScreenLayout.Scale;ui.y=Screen.height-ui.y;mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=ui}.WithButton(MouseButton.Left));yield return null;yield return null;
   Require(attack<=0&&queuedPcAction==0&&!pcAiming,"HUD click cannot attack or leave aiming active");
   Require(!PointerOnHud(new Vector2(900,400)),"former mobile buttons no longer block PC world clicks");
   InputSystem.RemoveDevice(mouse);foreach(var native in mice)InputSystem.EnableDevice(native);InputSystem.RemoveDevice(keyboard);
   Application.Quit();
  }
 }
}
#endif
