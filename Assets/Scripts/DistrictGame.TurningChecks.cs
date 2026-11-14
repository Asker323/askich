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
  IEnumerator CheckTurning(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_TURN_CHECK");Directory.CreateDirectory(folder);
   testSave=Path.Combine(Application.temporaryCachePath,"zonbee-turn-"+Guid.NewGuid()+".json");playing=true;paused=false;Application.runInBackground=true;
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);
   var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
   foreach(var enemy in infected){enemy.hp=0;enemy.root.gameObject.SetActive(false);}
   hero.p=new Vector2(0,-24);hero.root.position=V(hero.p);hero.root.rotation=Quaternion.identity;heroVelocity=Vector2.zero;inventory.Load(new DistrictInventoryData());UpdateClothing();hero.visual.Equip(0);noticeTime=0;
   var checks=new List<string>();
   void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines(Path.Combine(folder,"turn-checks.txt"),checks);if(!ok){Application.Quit(1);throw new Exception(label);}}
   void Aim(float yaw){var direction=Quaternion.Euler(0,yaw,0)*Vector3.forward;var position=(Vector2)cam.WorldToScreenPoint(V(hero.p,.85f)+direction*3);InputSystem.QueueStateEvent(mouse,new MouseState{position=position}.WithButton(MouseButton.Right));}
   yield return new WaitForSeconds(.3f);
   var body=hero.root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Body");var mesh=body.sharedMesh;
   foreach(string side in new[]{"L","R"}){
    int index=Enumerable.Range(0,mesh.blendShapeCount).First(i=>mesh.GetBlendShapeName(i).EndsWith("Grip"+side));
    var middle=new Vector3[mesh.vertexCount];var full=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(index,0,middle,null,null);mesh.GetBlendShapeFrameVertices(index,mesh.GetBlendShapeFrameCount(index)-1,full,null,null);
    float curve=0;for(int i=0;i<middle.Length;i++)curve=Mathf.Max(curve,body.transform.TransformVector(middle[i]-.38f*full[i]).magnitude);
    Check(mesh.GetBlendShapeFrameCount(index)==3&&curve>.002f,"hand "+side+" has a curved relaxed pose rather than a linear blend to the weapon fist, deviation="+curve.ToString("F4")+" m");
   }
   var feet=new[]{hero.visual.ArtBone("Foot.L"),hero.visual.ArtBone("Foot.R")};var origin=hero.p;
   string film=Path.Combine(folder,"frames");Directory.CreateDirectory(film);var times=new List<string>();int frame=0;
   foreach(float yaw in new[]{130f,-70f}){
    bool lifted=false,supported=false,shoulders=false;float supportDrift=0,minY=100;int lastLeg=-1;var lastFeet=new[]{feet[0].position,feet[1].position};float end=Time.time+1.8f;
    while(Time.time<end){
     Aim(yaw);yield return null;yield return new WaitForEndOfFrame();
     int leg=hero.visual.ArtPivotLeg;
     for(int i=0;i<2;i++){minY=Mathf.Min(minY,feet[i].position.y);lifted|=feet[i].position.y>.13f;}
     if(leg>=0&&leg==lastLeg){supported=true;supportDrift=Mathf.Max(supportDrift,Vector3.Distance(lastFeet[1-leg],feet[1-leg].position));}
     shoulders|=Mathf.Abs(hero.visual.ArtTurnLead)>2;lastLeg=leg;lastFeet[0]=feet[0].position;lastFeet[1]=feet[1].position;
     yield return CaptureGameCamera(Path.Combine(film,"turn-"+(frame++).ToString("D3")+".png"),1.6f);times.Add(Time.realtimeSinceStartup.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));
    }
    Check(pcAiming&&Vector2.Distance(origin,hero.p)<.001f,"mouse-driven pivot changes facing without moving the collision body: "+yaw);
    Check(Quaternion.Angle(hero.root.rotation,Quaternion.Euler(0,yaw,0))<3,"pivot reaches requested facing: "+yaw);
    Check(lifted&&supported&&supportDrift<.018f,"pivot lifts one foot while supporting foot stays planted: "+yaw+", drift="+supportDrift.ToString("F4"));
    Check(minY>.03f,"pivot feet stay above the road: "+yaw);
    Check(shoulders,"shoulders lead the turn before the feet settle: "+yaw);
   }
   File.WriteAllLines(Path.Combine(folder,"frame-times.txt"),times);
   Aim(30);yield return new WaitForSeconds(.12f);paused=true;yield return null;yield return new WaitForEndOfFrame();var frozen=feet[0].position;var frozenRotation=hero.root.rotation;yield return new WaitForSeconds(.2f);Check(Vector3.Distance(frozen,feet[0].position)<.001f&&Quaternion.Angle(frozenRotation,hero.root.rotation)<.001f,"pause freezes both turning body and foot placement");paused=false;
   InputSystem.QueueStateEvent(mouse,new MouseState{position=Vector2.one*10});InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSeconds(.5f);
   Check(hero.visual.State=="Walk"&&!hero.visual.ArtPivotPlanted&&Vector2.Distance(origin,hero.p)>.35f,"keyboard movement releases stationary foot anchors");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));yield return new WaitForSeconds(.4f);Check(hero.visual.State=="Run","sprint transition still responds to keyboard input");
   float phase=Mathf.Repeat(hero.visual.ArtPhase("Run"),1);hero.visual.Tick(3.4f,.016f,0,true);Check(hero.visual.State=="Run"&&Mathf.Abs(Mathf.Repeat(hero.visual.ArtPhase("Run"),1)-phase)<.002f,"small speed fluctuations do not restart running animation");hero.visual.Tick(2.9f,.016f,0,true);Check(hero.visual.State=="Walk","slowing below the hysteresis band returns to walking");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.4f);Check(hero.visual.State=="Idle","releasing movement blends back to idle");
   hero.visual.Equip(1);Aim(120);yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();Check(Vector3.Distance(hero.visual.ArtBone("Axe").position,hero.visual.Hand.position)<.12f,"turning shoulders keeps equipped axe in the palm");
   hero.visual.Attack(1,.7f);yield return null;yield return new WaitForEndOfFrame();Check(!hero.visual.ArtPivotPlanted&&hero.visual.State=="Slash","attack cancels foot placement without overriding the combat clip");
   hero.visual.Equip(0);InputSystem.QueueStateEvent(mouse,new MouseState());yield return new WaitForSeconds(.8f);Save();Check(Load(),"existing save loads after new presentation state");
   InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);Application.Quit();
  }
 }
}
#endif
