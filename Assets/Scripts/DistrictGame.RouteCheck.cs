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
  Keyboard routeKeyboard;string routeFolder;float routeStart;readonly List<string> routeLog=new List<string>();
  void RouteNote(string note){routeLog.Add($"{Time.time-routeStart:F1}s {note}; p={hero.p}; hp={health:F1}; weapon={weapon}; food={food}; bandages={bandages}; sterile={sterileBandages}; doses={alcoholDoses}; infection={infection:F1}; bleeding={bleeding}");File.WriteAllLines(Path.Combine(routeFolder,"route-report.txt"),routeLog);}
  void RouteRequire(bool condition,string note){RouteNote((condition?"PASS ":"FAIL ")+note);if(!condition){Application.Quit(1);throw new Exception("Route check: "+note);}}
  void RouteKeys(params Key[] keys){routeKeyboard.MakeCurrent();InputSystem.QueueStateEvent(routeKeyboard,new KeyboardState(keys));}
  void RouteDefend(){if(weapon==0)return;Person near=null;float range=1.75f;foreach(var z in infected)if(z.hp>0&&Vector2.Distance(hero.p,z.p)<range&&Sight(hero.p,z.p)){range=Vector2.Distance(hero.p,z.p);near=z;}if(near!=null)TryAttack((near.p-hero.p).normalized);}
  IEnumerator RouteWait(float seconds){RouteKeys();float start=Time.time;while(Time.time-start<seconds){RouteRequireAlive();yield return null;}}
  void RouteRequireAlive(){if(dead)RouteRequire(false,"hero died during ordinary route commands");}
  IEnumerator RouteWalk(Vector2 goal,bool sprint=false,bool defend=true){
   RouteNote("walk to "+goal);float start=Time.time;var path=FindPath(hero.p,goal);int index=0;
   RouteRequire(path.Count>0||Vector2.Distance(hero.p,goal)<1.5f,"route has navigable path");
   while(Vector2.Distance(hero.p,goal)>.28f){
    RouteRequireAlive();if(Time.time-start>55)RouteRequire(false,"walking timed out");if(defend)RouteDefend();
    while(index<path.Count&&Vector2.Distance(hero.p,path[index])<.3f)index++;
    Vector2 target=index<path.Count?path[index]:goal;Vector3 direction=V((target-hero.p).normalized);float x=Vector3.Dot(direction,cam.transform.right),y=Vector3.Dot(direction,Vector3.ProjectOnPlane(cam.transform.forward,Vector3.up).normalized);
    var keys=new List<Key>();if(x>.3f)keys.Add(Key.D);if(x<-.3f)keys.Add(Key.A);if(y>.3f)keys.Add(Key.W);if(y<-.3f)keys.Add(Key.S);if(sprint&&stamina>15)keys.Add(Key.LeftShift);RouteKeys(keys.ToArray());yield return null;
   }
   yield return RouteWait(.2f);
  }
  IEnumerator SecureRouteDoor(Home home){
   float until=Time.time+12;
   while(!home.gate.active&&Time.time<until){
    RouteRequireAlive();RouteDefend();RouteKeys();yield return null;
   }
   RouteRequire(home.gate.active,"cleared occupied threshold and secured door");
  }
  IEnumerator RouteUse(float wait=.3f){RouteKeys(Key.E);yield return new WaitForSeconds(.12f);yield return RouteWait(wait);}
  IEnumerator RoutePhoto(string name)=>CaptureGameCamera(Path.Combine(routeFolder,name+".png"));
  IEnumerator CaptureGameCamera(string file){
   // Capture the current gameplay camera even when the desktop window is hidden.
   yield return new WaitForEndOfFrame();
   var previous=cam.targetTexture;
   var active=RenderTexture.active;
   var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
   var frame=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
   try {
    cam.targetTexture=target;
    cam.Render();
    RenderTexture.active=target;
    frame.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
    frame.Apply();
    File.WriteAllBytes(file,frame.EncodeToPNG());
   } finally {
    cam.targetTexture=previous;
    RenderTexture.active=active;
    RenderTexture.ReleaseTemporary(target);
    Destroy(frame);
   }
  }
  IEnumerator FaceClinicGuard(){
   var guard=infected.Find(z=>z.id==0);
   RouteRequire(guard!=null&&guard.hp>0,"clinic guard present for exposure check");
   float until=Time.time+12;
   while(!bleeding&&Time.time<until){
    RouteRequireAlive();
    if(Vector2.Distance(hero.p,guard.p)>1.1f){
     yield return RouteWalk(guard.p,true,false);
    }else{
     yield return RouteWait(.1f);
    }
   }
   RouteRequire(bleeding&&infection>0,"actual enemy strike caused bleeding and infection");
   yield return RoutePhoto("route-wound");
  }
  IEnumerator CheckRoute(){
   // No teleportation, inventory grants, stat overrides, frozen AI or accelerated time.
   testSave=Path.Combine(Application.temporaryCachePath,"berezki-route-"+Guid.NewGuid().ToString("N")+".json");routeFolder=Environment.GetEnvironmentVariable("GOIDA_ROUTE_CHECK");Directory.CreateDirectory(routeFolder);Application.runInBackground=true;playing=true;paused=false;routeStart=Time.time;routeKeyboard=InputSystem.AddDevice<Keyboard>();
   RouteNote("fresh ordinary start");yield return RoutePhoto("route-street-start");yield return RouteWalk(new Vector2(-14,-20));yield return RouteUse();RouteRequire(stones==3,"picked up roadside stones");
   yield return RouteWalk(new Vector2(-12,-17));yield return RouteUse();RouteRequire(homes[5].open,"opened radio house");
   yield return RouteWalk(new Vector2(-12,-15));yield return RouteUse();RouteRequire(!homes[5].open,"closed door from inside");yield return SecureRouteDoor(homes[5]);
   yield return RouteWalk(new Vector2(-10,-13));yield return RouteUse(3.7f);RouteRequire(weapon==1,"found axe through timed search");yield return RoutePhoto("route-first-weapon");
   yield return RouteWalk(new Vector2(-10,-10.6f));yield return RouteUse(2.5f);RouteRequire(food>0,"found food");
   yield return RouteWalk(new Vector2(-12,-10.6f));yield return RouteUse(1.6f);RouteRequire(bandages>0,"found first bandage");
   yield return RouteWalk(new Vector2(-15,-14));yield return RouteUse();RouteRequire(alcoholDoses==3,"found alcohol bottle");
   yield return RouteWalk(backpackPosition);yield return RouteUse();
   RouteRequire(inventory.Data.bagFound,"found school backpack in the house");
   int bagSlot=Array.FindIndex(inventory.Data.slots,item=>item.kind==8);
   RouteRequire(inventory.Equip(bagSlot)&&inventory.Capacity==18,"equipped found backpack and expanded capacity");
   Save();yield return RoutePhoto("route-found-backpack");
   yield return RouteWalk(waterPositions[0]);yield return RouteUse();
   RouteRequire(inventory.Count(DistrictItemKind.Water)==3,"found drinking water in kitchen");
   inventorySelection=Array.FindIndex(inventory.Data.slots,item=>item.kind==9);
   RouteRequire(BeginMedical(MedicalAction.Water),"drank found water");yield return RouteWait(1.8f);
   RouteRequire(inventory.Count(DistrictItemKind.Water)==2,"water bottle retains two sips");inventorySelection=-1;
   RouteRequire(BeginMedical(MedicalAction.Craft),"craft command accepted");yield return RouteWait(3.3f);RouteRequire(sterileBandages>0,"crafted from collected materials");
   if(bleeding||infection>0){BeginMedical(MedicalAction.Sterile);yield return RouteWait(2.8f);RouteRequire(!bleeding&&infection==0,"treated actual route wound");}
   yield return RouteWalk(new Vector2(-14,-9));yield return RouteUse();float radioDeadline=Time.time+65;while(!heard&&Time.time<radioDeadline)yield return RouteWait(.1f);RouteRequire(heard,"heard radio objective");yield return RoutePhoto("route-radio");yield return RouteUse();RouteRequire(!radioOn,"switched off radio before leaving");
   yield return RouteWalk(new Vector2(-12,-15));yield return RouteUse();yield return RouteWalk(new Vector2(-12,-17));yield return RouteUse();
   yield return RouteWalk(new Vector2(-12,-24),true);yield return RoutePhoto("route-street-crossing");yield return RouteWalk(new Vector2(4,-24),true);face=Vector2.up;RouteRequire(ThrowStone(),"threw collected stone toward clinic flank");yield return RouteWait(.65f);
   yield return FaceClinicGuard();
   foreach(var coverTurn in new[]{new Vector2(20,-22),new Vector2(20,-17),new Vector2(14,-17)})yield return RouteWalk(coverTurn,true);
   yield return RouteWalk(new Vector2(12,-17),true);yield return RouteUse();RouteRequire(homes[6].open,"entered guarded clinic");
   yield return RouteWalk(new Vector2(12,-15));yield return RouteUse();yield return SecureRouteDoor(homes[6]);yield return RouteWalk(new Vector2(9,-14));yield return RouteUse();RouteRequire(bandages>=2,"picked up medical supplies");
   if(bleeding||infection>0){if(sterileBandages==0){BeginMedical(MedicalAction.Craft);yield return RouteWait(3.3f);}RouteRequire(BeginMedical(MedicalAction.Sterile),"began dressing actual wound");yield return RouteWait(2.8f);RouteRequire(!bleeding&&infection==0,"completed treatment inside clinic");}
   if(sterileBandages==0&&bandages>1&&alcoholDoses>0){RouteRequire(BeginMedical(MedicalAction.Craft),"prepared spare dressing inside clinic");yield return RouteWait(3.3f);}
   yield return RoutePhoto("route-clinic");yield return RouteWalk(new Vector2(12,-15));yield return RouteUse();yield return RouteWalk(new Vector2(12,-17));yield return RouteUse();
   foreach(var waypoint in new[]{new Vector2(12,-24),new Vector2(24,-24),new Vector2(24,0),new Vector2(24,24),new Vector2(48,24),new Vector2(48,45),refuge}){
    yield return RouteWalk(waypoint,true);
    if(bleeding&&sterileBandages>0&&!infected.Exists(z=>z.hp>0&&Vector2.Distance(z.p,hero.p)<6&&Sight(hero.p,z.p))){
     RouteRequire(BeginMedical(MedicalAction.Sterile),"began spare dressing after breaking pursuit");yield return RouteWait(2.8f);
     RouteRequire(!bleeding,"stopped new bleeding on return journey");
    }
   }
   yield return RouteUse();RouteRequire(campEstablished,"established shelter using collected supplies");yield return RoutePhoto("route-shelter");
   if(SafeToRest()&&tiredness>5){float before=tiredness;RouteRequire(BeginMedical(MedicalAction.Rest),"started safe shelter rest");yield return RouteWait(6.3f);RouteRequire(tiredness<before,"shelter rest restores energy");}
   yield return RouteWalk(cistern);yield return RouteUse();yield return RouteWait(3.3f);
   RouteRequire(inventory.Count(DistrictItemKind.Water)>=3,"refilled carried bottle at shelter reserve");
   Save();RouteNote("COMPLETE automated ordinary-rules route; not a manual playthrough");Application.Quit();
  }
 }
}
#endif
