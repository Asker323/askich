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
  IEnumerator CheckExploration(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_EXPLORATION_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"zonbee-exploration-"+Guid.NewGuid()+".json");Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;playing=true;paused=true;
   foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=100;bleeding=false;infection=0;heroVelocity=Vector2.zero;heroStagger=attack=0;inventory.Load(new DistrictInventoryData());progression.Data.minute=1020;TickWorldLife(0);explorationFlags=0;RefreshExploration(true);
   var results=new List<string>();void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"exploration-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   void Place(Vector2 p){hero.p=p;hero.root.position=V(p);heroVelocity=Vector2.zero;}
   Place(gardenGate+Vector2.down*1.3f);Check(!Blocked(hero.p)&&Blocked(gardenGate),"garden approach is clear and closed gate physically blocks passage");
      Check(FindPath(new Vector2(-36,-26),hero.p).Count>0,"garden can be reached from the public street");
   Use();Check((explorationFlags&GardenOpen)!=0&&gardenBarrier.active,"gate starts opening while its leaf still blocks");paused=false;yield return new WaitForSeconds(.25f);paused=true;Check(gardenAngle>10&&gardenAngle<70&&gardenBarrier.active,"gate passes through intermediate solid angles");float angle=gardenAngle;yield return new WaitForSeconds(.2f);Check(Mathf.Abs(gardenAngle-angle)<.01f,"pause freezes moving garden gate");paused=false;yield return new WaitForSeconds(.6f);paused=true;
   Check(!Blocked(gardenGate)&&gardenAngle>=99,"open gate creates a traversable entrance");
   Check(FindPath(gardenGate+Vector2.down*1.3f,gardenCache+Vector2.down*1.25f).Count>0,"path through the garden reaches equipment cache");
   Place(gardenGate);Use();Check((explorationFlags&GardenOpen)!=0,"occupied gate refuses to close on survivor");
   explorationFlags&=~GardenOpen;Save();Check((ReadSave(SavePath).exploration&GardenOpen)!=0,"snapshot keeps an occupied gate open for safe loading");explorationFlags|=GardenOpen;
   Place(gardenCache+Vector2.down*1.25f);Use();Check(cacheSearching&&(explorationFlags&GardenTaken)==0,"cache starts a timed search without granting materials");paused=false;yield return new WaitForSeconds(.7f);paused=true;Check(cacheSearching&&cacheSearch>.4f&&cacheLid.localEulerAngles.x>300,"cache animates its lid and rummages before loot");
   float seconds=cacheSearch;yield return new WaitForSeconds(.2f);Check(cacheSearch==seconds,"pause freezes cache search");Use();Check(!cacheSearching&&(explorationFlags&GardenTaken)==0&&inventory.Count(DistrictItemKind.Wood)==0,"E cancels without granting or consuming the cache");
   Use();heroVelocity=Vector2.right;TickExploration(.1f);heroVelocity=Vector2.zero;Check(!cacheSearching,"movement interrupts equipment search");
   Use();health-=1;TickExploration(.1f);Check(!cacheSearching,"injury interrupts equipment search");health=100;
   var full=new DistrictInventoryData();for(int i=0;i<6;i++)full.slots[i]=new DistrictItem(DistrictItemKind.Stone,10);inventory.Load(full);Use();TickExploration(3.1f);Check((explorationFlags&GardenTaken)==0&&inventory.Count(DistrictItemKind.Wood)==0&&inventory.Count(DistrictItemKind.Stone)==60,"full inventory leaves all materials in the world atomically");
   inventory.Load(new DistrictInventoryData());Use();paused=false;yield return new WaitForSeconds(3.25f);paused=true;Check((explorationFlags&GardenTaken)!=0&&inventory.Count(DistrictItemKind.Wood)==2&&inventory.Count(DistrictItemKind.Wire)==1&&inventory.Count(DistrictItemKind.Cloth)==2,"complete search transfers the advertised crafting materials");Use();Check(inventory.Count(DistrictItemKind.Wood)==2,"empty cache cannot duplicate materials");
   Save();explorationFlags=0;RefreshExploration(true);Check(Load()&&(explorationFlags&GardenTaken)!=0&&(explorationFlags&GardenOpen)!=0&&!gardenBarrier.active,"cache and gate restore from ordinary save");paused=true;
   Place(new Vector2(-45,-14.5f));noticeTime=0;hero.visual.ArtSample("Idle",0);yield return CaptureGameCamera(Path.Combine(folder,"exploration-garden.png"),7.3f);
   Place(alarmSwitch+Vector2.down*1.1f);Check(!Blocked(hero.p)&&!Blocked(alarmSwitch),"alarm switch and sound target are on navigable ground");var witness=infected[0];witness.hp=witness.maxHp;witness.p=alarmSwitch+new Vector2(-3,8);witness.awareness=Awareness.Roam;witness.root.rotation=Quaternion.identity;Use();Check(alarmRemaining>17&&carAlarm.isPlaying&&(explorationFlags&AlarmSpent)!=0,"car alarm starts spatial audio and spends its only battery charge");Check(witness.awareness==Awareness.Investigate&&Vector2.Distance(witness.goal,alarmSwitch)<.01f&&Vector2.Distance(witness.goal,hero.p)>.5f,"infected investigate sound location rather than follow hidden survivor");Check(FindPath(witness.p,witness.goal).Count>0,"infected have an actual path to alarm source");witness.hp=0;
   seconds=alarmRemaining;yield return new WaitForSeconds(.2f);Check(alarmRemaining==seconds,"pause freezes alarm duration");
   paused=false;yield return new WaitForSeconds(.2f);paused=true;Check(alarmRemaining<seconds&&alarmLamps.All(r=>r!=null),"alarm and warning lamps advance in live simulation");yield return CaptureGameCamera(Path.Combine(folder,"exploration-alarm.png"),5.6f);
   Save();Check(Load()&&alarmRemaining==0&&!carAlarm.isPlaying&&(explorationFlags&AlarmSpent)!=0,"loading preserves empty battery without replaying alarm");paused=true;Use();Check(alarmRemaining==0,"spent car battery prevents endless free distractions");
   Place(busNotice+Vector2.down*1.1f);paused=false;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());paused=true;Check(notice.StartsWith("ЗАПИСКА НА ОСТАНОВКЕ"),"real E key reads the evacuation notice");Check((explorationFlags&StopSeen)!=0,"nearby exploration reveals the bus stop marker");Check(!busCanopy.activeSelf&&!busRear.activeSelf,"nearby shelter cuts away roof and rear panel to keep the survivor visible");Check(!Blocked(hero.p)&&Blocked(new Vector2(-7,-18.2f)),"bus shelter has reachable frontage and a solid back wall");noticeTime=0;yield return CaptureGameCamera(Path.Combine(folder,"exploration-stop.png"),5.8f);
   Save();string snapshot=File.ReadAllText(SavePath);var old=Regex.Replace(snapshot,"\"exploration\":\\d+,","");Check(old!=snapshot,"legacy fixture omits newly added optional state");File.WriteAllText(SavePath,old);Check(Load()&&explorationFlags==0&&inventory.Count(DistrictItemKind.Wood)==2,"old v9 saves load with intact inventory and fresh exploration state");paused=true;
   File.WriteAllText(SavePath+".bak","broken");File.WriteAllText(SavePath,snapshot.Replace("\"exploration\":"+JsonUtility.FromJson<SaveData>(snapshot).exploration,"\"exploration\":64"));int previous=explorationFlags;Check(!Load()&&explorationFlags==previous,"unknown exploration bits are rejected before mutation");File.WriteAllText(SavePath,snapshot);Check(Load(),"valid exploration snapshot loads after rejection");
   InputSystem.RemoveDevice(keyboard);Application.Quit();
  }
 }
}
#endif
