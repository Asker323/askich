using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Askich {
 public sealed partial class DistrictGame {
  void BuildDoor(Home h){
   var point=h.p+Vector2.down*4;
   h.door=new GameObject("Hinged front door");h.door.transform.position=V(point+Vector2.left*.95f);
   Color wood=new Color(.25f,.31f,.30f);
   Cube("Door leaf",new Vector3(.95f,1.25f,0),new Vector3(1.9f,2.5f,.15f),wood,h.door.transform);
   foreach(float side in new[]{-1f,1f}){
    for(int i=0;i<2;i++)Cube("Recessed door panel",new Vector3(.95f,.65f+i*1.05f,side*.084f),new Vector3(1.5f,.75f,.035f),new Color(.19f,.25f,.24f),h.door.transform);
    Cube("Door latch plate",new Vector3(1.63f,1.22f,side*.11f),new Vector3(.13f,.25f,.055f),new Color(.38f,.36f,.27f),h.door.transform);
    Cube("Door handle",new Vector3(1.51f,1.23f,side*.16f),new Vector3(.3f,.055f,.08f),new Color(.49f,.46f,.33f),h.door.transform);
   }
   for(int side=-1;side<=1;side+=2)Cube("Door frame",V(point+Vector2.right*(side*1.04f),1.28f),new Vector3(.14f,2.56f,.24f),new Color(.42f,.43f,.36f));
   h.cut.Add(Cube("Door frame lintel",V(point,2.56f),new Vector3(2.2f,.14f,.24f),new Color(.42f,.43f,.36f)));
   SnapDoor(h);
  }
  void SnapDoor(Home h){
   h.doorHeld=false;h.doorAngle=h.open?100:0;
   h.door.SetActive(true);
   h.door.transform.localRotation=Quaternion.Euler(0,-h.doorAngle,0);
   h.gate.active=!h.open;
  }
  bool DoorwayOccupied(Home h){
   var rect=new Rect(h.gate.rect.x-.28f,h.gate.rect.y-.28f,h.gate.rect.width+.56f,h.gate.rect.height+.56f);
   if(rect.Contains(hero.p))return true;
   return infected.Exists(z=>z.hp>0&&rect.Contains(z.p));
  }
  void TickInteractions(float dt){
   foreach(var h in homes){
    bool occupied=DoorwayOccupied(h);
    bool held=!h.open&&occupied;
    if(held&&!h.doorHeld&&Vector2.Distance(hero.p,h.p+Vector2.down*4)<3)Say("Проём занят. Отойди или оттесни заражённого.");
    h.doorHeld=held;
    float target=h.open?100:occupied?32:0;
    h.doorAngle=Mathf.MoveTowards(h.doorAngle,target,dt*320);
    h.door.transform.localRotation=Quaternion.Euler(0,-h.doorAngle,0);
    h.gate.active=!occupied&&h.doorAngle<(h.open?70:25);
    for(int i=0;i<3;i++){
     bool searching=searchSlot==i&&searchHome==h;
     float angle=h.taken[i]?105:searching?Mathf.Lerp(15,55,Mathf.Clamp01(searchTime/.4f))+Mathf.Sin(searchTime*14)*3:0;
     h.lids[i].localRotation=Quaternion.RotateTowards(h.lids[i].localRotation,Quaternion.Euler(angle,0,0),dt*240);
    }
   }
   containerAudio.SetSearching(searchSlot>=0||cacheSearching);
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  IEnumerator CheckInteractions(Action<bool,string> check,string folder){
   foreach(var enemy in infected)enemy.hp=0;
   health=100;bleeding=false;infection=0;heroVelocity=Vector2.zero;heroStagger=attack=0;
   var home=homes[5];hero.p=home.p+Vector2.down*5.5f;home.open=false;SnapDoor(home);
   yield return new WaitForSeconds(.75f);
   Door(home);
   check(home.gate.active&&home.door.activeSelf,"opening door retains its mesh and blocks until swung clear");
   yield return new WaitForSeconds(.12f);
   check(home.doorAngle>10&&home.doorAngle<80,"front door rotates through intermediate angle");
   paused=true;float angle=home.doorAngle;yield return new WaitForSeconds(.2f);
   check(Mathf.Abs(home.doorAngle-angle)<.01f,"pause freezes door motion");paused=false;
   yield return new WaitForSeconds(.4f);
   check(!home.gate.active&&home.doorAngle>=99,"open doorway becomes traversable after swing");
   yield return CaptureGameCamera(Path.Combine(folder,"interactions-door.png"));
   hero.p=home.p+Vector2.down*4;Door(home);yield return new WaitForSeconds(.4f);
   check(!home.gate.active&&home.doorAngle>=30,"occupied doorway holds closing leaf and avoids trapping survivor");
   Save();Vector2 atThreshold=hero.p;
   check(Load()&&home.open&&!home.gate.active&&Vector2.Distance(hero.p,atThreshold)<.01f,"saving occupied doorway reloads safe open state without moving survivor");
   home.open=false;
   hero.p=home.p+Vector2.down*2;heroVelocity=Vector2.zero;yield return new WaitForSeconds(.4f);
   check(home.gate.active&&home.doorAngle<1,"door finishes closing when threshold clears");
   home.open=true;SnapDoor(home);Save();home.open=false;SnapDoor(home);
   check(Load()&&home.open&&!home.gate.active&&home.doorAngle==100,"loading snaps saved open door without replaying motion");
   hero.p=home.p+new Vector2(1.2f,2.4f);
   check(NearbyContainer(home)==1,"hint and action choose nearest container in overlapping reach");
   hero.p=home.p+new Vector2(2,4.6f);
   check(NearbyContainer(home)==-1,"closed wall prevents searching container from outside");
   home.taken[0]=false;RefreshContainers(home);hero.p=home.p+home.containers[0]+Vector2.down;
   BeginSearch(home,0);yield return new WaitForSeconds(.65f);
   check(!home.taken[0]&&Quaternion.Angle(home.lids[0].localRotation,Quaternion.identity)>25&&containerAudio.Searching,"search opens lid while rummage loop plays before loot");
   Vector3 hand=hero.root.InverseTransformPoint(hero.visual.Hand.position);
   check(hero.visual.State=="Search"&&hand.y<1.45f&&hand.z>.3f,"dedicated search animation holds hands in front below shoulder level");
   yield return CaptureGameCamera(Path.Combine(folder,"interactions-search.png"));
   searchSlot=-1;yield return new WaitForSeconds(.4f);
   check(!home.taken[0]&&Quaternion.Angle(home.lids[0].localRotation,Quaternion.identity)<1&&!containerAudio.Searching,"interrupted search closes lid and stops rummage without granting loot");
   BeginSearch(home,0);yield return new WaitForSeconds(3.7f);
   check(home.taken[0]&&weapon==1&&!containerAudio.Searching,"completed search grants axe once and ends rummage loop");
   Save();check(Load()&&home.taken[0]&&Quaternion.Angle(home.lids[0].localRotation,Quaternion.Euler(105,0,0))<1,"loading restores searched container at final open pose");
  }
#endif
 }
}
