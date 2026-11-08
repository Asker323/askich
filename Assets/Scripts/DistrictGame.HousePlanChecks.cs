#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckHousePlans(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_HOUSE_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"zonbee-houses-"+Guid.NewGuid()+".json");Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;playing=true;paused=true;
   foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);var keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=100;bleeding=false;infection=0;heroVelocity=Vector2.zero;heroStagger=attack=0;inventory.Load(new DistrictInventoryData());RestoreBackExits(0);progression.Data.minute=900;TickWorldLife(0);
   var results=new List<string>();void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"house-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   void Place(Vector2 p){hero.p=p;hero.root.position=V(p);heroVelocity=Vector2.zero;current=RoomAt(p);}
   Check(backExits.Count==2&&homes.Count==22,"two new exits preserve all existing home identities");
   foreach(var e in backExits){
    var h=e.home;string tag=homes.IndexOf(h).ToString();h.open=false;SnapDoor(h);Place(e.p+Vector2.up*1.2f);
    Check(!Blocked(hero.p)&&Blocked(e.p)&&e.gate.active,"clear rear approach and solid closed door: "+tag);
    InputSystem.QueueStateEvent(keys,new KeyboardState(Key.E));paused=false;yield return null;yield return null;InputSystem.QueueStateEvent(keys,new KeyboardState());paused=true;Check(e.open&&e.angle<70&&e.gate.active,"real E begins physical door swing: "+tag);
    float angle=e.angle;yield return new WaitForSeconds(.2f);Check(e.angle==angle,"pause freezes rear door: "+tag);paused=false;yield return new WaitForSeconds(.6f);paused=true;
    Check(!Blocked(e.p)&&e.angle>=99,"open rear doorway is traversable: "+tag);
    Check(FindPath(e.p+Vector2.up*1.2f,h.p+Vector2.down*2).Count>0,"rear entrance reaches living space with front door closed: "+tag);
    for(int slot=0;slot<3;slot++){var approach=h.p+h.containers[slot]+Vector2.down*1.1f;Check(!Blocked(approach)&&FindPath(e.p+Vector2.up*1.2f,approach).Count>0,"rear entrance reaches storage "+tag+" / "+slot);}
    Place(e.p+Vector2.down*.9f);var moved=Move(hero.p,Vector2.up*2);Check(moved.y>e.p.y+.8f,"body can move through the open exit: "+tag);
    Place(e.p);Use();Check(e.open,"occupied rear door cannot close on survivor: "+tag);e.open=false;Save();Check((ReadSave(SavePath).backDoors&e.bit)!=0,"occupied rear exit saves safely open: "+tag);e.open=true;
    Place(e.p+Vector2.up*1.2f);Use();paused=false;yield return new WaitForSeconds(.6f);paused=true;Check(!e.open&&Blocked(e.p),"closing rear door restores collision: "+tag);
    Vector2 inside=HasHousePlan(homes.IndexOf(h))&&homes.IndexOf(h)==4?h.p+new Vector2(.8f,2):h.p+new Vector2(-2,-.9f);var fixedPoint=ResolveSolidPosition(inside);Check(Blocked(inside)&&!Blocked(fixedPoint)&&RoomAt(fixedPoint)==h,"old position in a new partition is repaired within its own home: "+tag);
    e.open=true;e.angle=100;e.gate.active=false;e.leaf.rotation=Quaternion.Euler(0,100,0);
   }
   var exit=backExits[0];var witness=infected[0];witness.hp=witness.maxHp;witness.root.gameObject.SetActive(true);witness.visual.Revive();witness.p=exit.p+Vector2.up*2;witness.root.position=V(witness.p);witness.root.rotation=Quaternion.identity;witness.awareness=Awareness.Investigate;witness.goal=exit.home.p;witness.searchOrigin=witness.goal;witness.alert=14;witness.repath=0;witness.rest=0;witness.path.Clear();Place(exit.home.p+Vector2.down*2);paused=false;yield return new WaitForSeconds(3.1f);paused=true;Check(witness.p.y<exit.p.y-.4f,"living infected navigates through rear doorway while front is closed");witness.hp=0;witness.root.gameObject.SetActive(false);health=100;bleeding=false;infection=0;heroStagger=attack=0;
   var family=homes[4];var shop=homes[8];Check(!Sight(family.p+new Vector2(.1f,2),family.p+new Vector2(1.5f,2))&&Sight(family.p+new Vector2(.1f,0),family.p+new Vector2(1.5f,0)),"family bedroom wall blocks sight while its doorway remains open");
   Check(!Sight(shop.p+new Vector2(-2,-1.7f),shop.p+new Vector2(-2,-.1f))&&Sight(shop.p+new Vector2(0,-1.7f),shop.p),"carpenter partition has a distinct central passage");
   Place(family.p+family.containers[1]+Vector2.down*.8f);Check(NearbyContainer(family)==1&&NearbyBackExit()==null,"nearer kitchen storage retains E priority over rear exit");
   Place(family.p+new Vector2(-.5f,-1.5f));hero.visual.ArtSample("Idle",0);paused=false;yield return null;paused=true;noticeTime=0;yield return CaptureGameCamera(Path.Combine(folder,"houses-family.png"),5.6f);
   Place(shop.p+new Vector2(0,-2));paused=false;yield return null;paused=true;noticeTime=0;yield return CaptureGameCamera(Path.Combine(folder,"houses-workshop.png"),5.6f);
   Place(backExits[0].p+Vector2.up*1.2f);Save();string snapshot=File.ReadAllText(SavePath);RestoreBackExits(0);Check(Load()&&backExits.All(e=>e.open&&!e.gate.active),"both rear exits restore their open state from save");paused=true;
   var old=Regex.Replace(snapshot,"\"backDoors\":\\d+,","");File.WriteAllText(SavePath,old);Check(old!=snapshot&&Load()&&backExits.All(e=>!e.open&&e.gate.active),"old v9 saves without rear-door state load with closed new exits");paused=true;
   File.WriteAllText(SavePath+".bak","broken");File.WriteAllText(SavePath,snapshot.Replace("\"backDoors\":3","\"backDoors\":4"));Check(!Load()&&backExits.All(e=>!e.open),"unknown rear-door state rejected before changing the world");File.WriteAllText(SavePath,snapshot);Check(Load(),"valid household snapshot reloads after rejection");
   InputSystem.RemoveDevice(keys);Application.Quit();
  }
 }
}
#endif
