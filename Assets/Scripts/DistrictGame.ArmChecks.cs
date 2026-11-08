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
  IEnumerator CheckRelaxedArms(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_ARM_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"zonbee-arms-"+Guid.NewGuid()+".json");Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;playing=true;paused=true;
   foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);var keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();
   foreach(var enemy in infected){enemy.hp=0;enemy.root.gameObject.SetActive(false);}hero.p=new Vector2(-24,-24);hero.root.position=V(hero.p);hero.root.rotation=Quaternion.identity;heroVelocity=Vector2.zero;inventory.Load(new DistrictInventoryData());UpdateClothing();hero.visual.Equip(0);item.gameObject.SetActive(false);axeHead.gameObject.SetActive(false);spearHead.gameObject.SetActive(false);hero.visual.SetPaused(true);noticeTime=0;
   var results=new List<string>();var measurements=new List<string>();
   void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"arm-checks.txt"),results);}
   Vector3 Point(string bone)=>hero.root.InverseTransformPoint(hero.visual.ArtBone(bone).position);
   foreach(string key in new[]{"Idle","Walk","Run"}){
    float maxSpread=0,minDrop=100,minFlex=180,maxFlex=0,minHandY=100,maxHandY=-100,minHandX=100;float[] minZ={100,100},maxZ={-100,-100};bool outside=true;Quaternion wristStart=Quaternion.identity;float wristTravel=0;
    for(int frame=0;frame<=48;frame++){
     hero.visual.ArtSample(key,frame/48f);
     var wrist=hero.visual.ArtBone("Hand.R").localRotation;if(frame==0)wristStart=wrist;wristTravel=Mathf.Max(wristTravel,Quaternion.Angle(wristStart,wrist));
     for(int side=0;side<2;side++){
      string suffix=side==0?"L":"R";var shoulder=Point("UpperArm."+suffix);var elbow=Point("LowerArm."+suffix);var hand=Point("Hand."+suffix);float sign=Mathf.Sign(shoulder.x);
      maxSpread=Mathf.Max(maxSpread,Mathf.Abs(elbow.x-shoulder.x));minDrop=Mathf.Min(minDrop,shoulder.y-elbow.y);float flex=Vector3.Angle(elbow-shoulder,hand-elbow);minFlex=Mathf.Min(minFlex,flex);maxFlex=Mathf.Max(maxFlex,flex);minZ[side]=Mathf.Min(minZ[side],hand.z);maxZ[side]=Mathf.Max(maxZ[side],hand.z);minHandY=Mathf.Min(minHandY,hand.y);maxHandY=Mathf.Max(maxHandY,hand.y);minHandX=Mathf.Min(minHandX,sign*hand.x);outside&=sign*hand.x>.17f;
     }
    }
    measurements.Add($"{key}: elbow lateral offset {maxSpread:F3} m; minimum shoulder/elbow drop {minDrop:F3} m; elbow flex {minFlex:F1}–{maxFlex:F1} deg; minimum hand distance from body centre {minHandX:F3} m; hand height {minHandY:F3}–{maxHandY:F3} m; hand travel {maxZ[0]-minZ[0]:F3}/{maxZ[1]-minZ[1]:F3} m; wrist travel {wristTravel:F1} deg.");File.WriteAllLines(Path.Combine(folder,"arm-measurements.txt"),measurements);
    Check(maxSpread<.08f,key+": elbows stay close to shoulder line instead of flaring out");Check(minDrop>(key=="Run"?.16f:.22f),key+": elbows remain below shoulders throughout the cycle");Check(outside,key+": hands stay outside the torso");
    Check(key=="Run"?minFlex>55&&maxFlex<100:minFlex>5&&maxFlex<32,key+": elbow bend matches relaxed walk or running carriage");
    Check(wristTravel>(key=="Idle"?2:10),key+": wrist flexes and rotates relative to forearm during the cycle");
    if(key!="Idle")Check(maxZ[0]-minZ[0]>.22f&&maxZ[1]-minZ[1]>.22f,key+": both hands have a forward/back swing");
   }
   hero.visual.ArtSample("Walk",0);float leftBack=Point("Hand.L").z,rightFront=Point("Hand.R").z;Check(Point("Foot.L").z>Point("Foot.R").z&&leftBack<rightFront,"arms counter-swing against the opposite legs");hero.visual.ArtSample("Walk",.5f);Check(Point("Hand.L").z>Point("Hand.R").z,"arm swing reverses in the second half cycle");
   foreach(string key in new[]{"Idle","Walk","Run"}){
    hero.visual.ArtSample(key,0);var first=Point("Hand.L");hero.visual.ArtSample(key,1);Check(Vector3.Distance(first,Point("Hand.L"))<.002f,key+": hand path loops without a seam");
   }
   string film=Path.Combine(folder,"frames");Directory.CreateDirectory(film);float near=cam.nearClipPlane;cam.nearClipPlane=32;var fill=new GameObject("Arm inspection fill",typeof(Light)).GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.color=new Color(.88f,.92f,1);
   foreach(string view in new[]{"front","side"}){
    cam.transform.rotation=Quaternion.Euler(10,view=="front"?180:90,0);hero.root.rotation=Quaternion.identity;fill.transform.rotation=cam.transform.rotation;
    foreach(string key in new[]{"Walk","Run"})for(int frame=0;frame<24;frame++){hero.visual.ArtSample(key,frame/24f);yield return CaptureGameCamera(Path.Combine(film,view+"-"+key.ToLowerInvariant()+"-"+frame.ToString("D3")+".png"),1.25f);}
   }
   hero.visual.ArtSample("Walk",0);cam.transform.rotation=Quaternion.Euler(10,180,0);fill.transform.rotation=cam.transform.rotation;yield return CaptureGameCamera(Path.Combine(folder,"arms-walk-front.png"),1.25f);
   hero.visual.ArtSample("Walk",0);cam.transform.rotation=Quaternion.Euler(10,90,0);fill.transform.rotation=cam.transform.rotation;yield return CaptureGameCamera(Path.Combine(folder,"arms-walk-side.png"),1.25f);cam.nearClipPlane=near;Destroy(fill.gameObject);
   foreach(int tool in new[]{1,2}){hero.visual.Equip(tool);hero.visual.ArtSample("Walk",.25f);var held=hero.visual.ArtBone(tool==1?"Axe":"Spear");Check(Vector3.Distance(held.position,hero.visual.Hand.position)<.12f,"equipped tool remains in palm: "+tool);}hero.visual.Equip(0);
   cam.transform.rotation=Quaternion.Euler(42,45,0);hero.visual.SetPaused(false);paused=false;heroVelocity=Vector2.zero;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W));yield return new WaitForSeconds(.45f);Check(hero.visual.State=="Walk"&&hero.visual.ArtPhase("Walk")>0,"real keyboard movement advances revised walk");InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.LeftShift));yield return new WaitForSeconds(.30f);Check(hero.visual.State=="Run","real keyboard sprint switches to revised run");InputSystem.QueueStateEvent(keys,new KeyboardState());yield return new WaitForSeconds(.3f);Check(hero.visual.State=="Idle","releasing movement returns to relaxed idle");
   var skin=hero.root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Body");int left=Enumerable.Range(0,skin.sharedMesh.blendShapeCount).First(i=>skin.sharedMesh.GetBlendShapeName(i).EndsWith("GripL")),right=Enumerable.Range(0,skin.sharedMesh.blendShapeCount).First(i=>skin.sharedMesh.GetBlendShapeName(i).EndsWith("GripR"));
   Check(skin.GetBlendShapeWeight(left)>32&&skin.GetBlendShapeWeight(left)<45&&skin.GetBlendShapeWeight(right)>32&&skin.GetBlendShapeWeight(right)<45,"both unarmed hands settle into a loose resting curl");
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W,Key.LeftShift));yield return new WaitForSeconds(.5f);Check(skin.GetBlendShapeWeight(left)>50&&skin.GetBlendShapeWeight(right)>50,"live sprint curls both hands more than idle");
   hero.visual.Equip(1);Check(skin.GetBlendShapeWeight(left)>99&&skin.GetBlendShapeWeight(right)<75,"weapon keeps full left grip while offhand stays relaxed");hero.visual.Equip(0);
   paused=true;hero.visual.SetPaused(true);float frozen=skin.GetBlendShapeWeight(right);yield return new WaitForSeconds(.2f);Check(Mathf.Abs(skin.GetBlendShapeWeight(right)-frozen)<.001f,"pause freezes the finger transition");InputSystem.QueueStateEvent(keys,new KeyboardState());paused=false;hero.visual.SetPaused(false);yield return new WaitForSeconds(.5f);
   Save();Check(Load()&&inventory.Data.weapon==0,"ordinary save loads after arm changes");InputSystem.RemoveDevice(keys);Application.Quit(results.Any(r=>r.StartsWith("FAIL "))?1:0);
  }
 }
}
#endif
