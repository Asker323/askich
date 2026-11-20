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
  void PopulateLegacyTestEnemies(SaveData data){
   var roster=new List<EnemySave>();
   for(int id=0;id<=108;id++)if(LegacyCuratedEnemyId(id,data.version)){
    var current=Array.Find(data.enemies,e=>e.id==id);var actor=infected.Find(e=>e.id==id);
    roster.Add(new EnemySave{id=id,x=current==null?20:current.x,z=current==null?0:current.z,yaw=current==null?0:current.yaw,hp=current==null?0:current.hp/actor.maxHp*(data.version<=3?LegacyEnemyHealth(id):PreviousEnemyHealth(id))});
   }
   data.enemies=roster.ToArray();
  }
  IEnumerator CheckCuratedDistrict(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_CURATED_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"curated-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;playing=true;paused=true;foreach(var d in InputSystem.devices.ToArray())if(d is Keyboard||d is Mouse)InputSystem.DisableDevice(d);
   var checks=new List<string>();void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines(Path.Combine(folder,"curated-checks.txt"),checks);if(!ok){Application.Quit(1);throw new Exception(label);}}
   yield return null;var buildings=homes.Where(h=>h.built).ToArray();
   Check(buildings.Length==3&&homes.Count==22,"three constructed buildings retain 22 stable legacy storage identities");
   Check(homes.Where(h=>!h.built).All(h=>h.roof==null&&h.door==null&&h.gate==null&&h.lids.All(l=>!l)),"retired buildings create no roofs doors storage meshes or barriers");
   Check(Vector2.Distance(buildings[0].p,buildings[1].p)>50&&Vector2.Distance(buildings[1].p,buildings[2].p)>100,"occupied buildings spread across the continuous map");
   Check(infected.Count==6&&infected.Select(z=>z.id).Distinct().Count()==6,"six persistent infected replace the previous 39");
   Check(infected.All(z=>z.hp==z.maxHp&&z.maxHp>PreviousEnemyHealth(z.id)&&!Blocked(z.spawn)),"all six spawn clear of obstacles with increased health");
   Check(infected.Where(z=>z.id%3==0).All(z=>z.speed==3.35f&&EnemyDamage(z)==34)&&infected.Where(z=>z.id%3==1).All(z=>z.speed==2.55f&&EnemyDamage(z)==44)&&infected.Where(IsCrawler).All(z=>z.speed==2.1f&&EnemyDamage(z)==28),"runner heavy and crawler have distinct increased speed and contact damage");
   File.WriteAllText(Path.Combine(folder,"world-budget.txt"),"Buildings: 22 -> 3\nInfected: 39 -> 6\nRenderers after initial frame: "+FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length+"\nMovement prop records: "+solidProps.Count+"\nWall/door barriers: "+barriers.Count+"\nNo measured FPS claim.\n");
   foreach(var enemy in infected){enemy.hp=0;enemy.root.gameObject.SetActive(false);}
   foreach(var h in buildings){
    h.open=true;SnapDoor(h);
    for(int i=0;i<3;i++){var stand=StorageStand(h,i);Check(!Blocked(stand)&&Sight(stand,h.p+h.containers[i])&&FindPath(h.p+Vector2.down*5,stand).Count>0,"reachable actual search position: "+homes.IndexOf(h)+" / "+i);}
    foreach(var point in new[]{new Vector2(-1.6f,1.55f),new Vector2(-1.65f,-.95f),new Vector2(2.35f,-.3f),new Vector2(1.2f,1.3f)})Check(!Blocked(h.p+point)&&FindPath(h.p+Vector2.down*5,h.p+point).Count>0,"bathroom bedroom living room and kitchen circulation: "+homes.IndexOf(h)+" "+point);
    Check(Blocked(h.p+new Vector2(-3.15f,2.65f))&&Blocked(h.p+new Vector2(-2.55f,-2.65f))&&Blocked(h.p+new Vector2(.9f,-.1f)),"bathtub bed and couch stop movement: "+homes.IndexOf(h));
    Check(!Sight(h.p+new Vector2(-2,0),h.p+new Vector2(-2,1.5f)),"bathroom partition blocks vision: "+homes.IndexOf(h));
   }
   foreach(var p in waterPositions)Check(!Blocked(p),"water pickup remains visible on clear floor "+p);
   foreach(var p in clothingPositions)Check(!Blocked(p),"clothing pickup remains on clear floor "+p);
   Check(backExits.Count==2&&houseWindows.Count==2,"retained homes have working rear exits and glazed climbing windows");
   var tv=televisions[0];hero.p=tv.p+Vector2.left;current=tv.home;hero.root.position=V(hero.p);
   Check(UseTelevision()&&tv.on&&tv.sound.isPlaying&&tv.glow.enabled,"E powers television picture spatial static and local glow");
   var listenerEnemy=infected[0];listenerEnemy.hp=listenerEnemy.maxHp;listenerEnemy.p=tv.home.p+new Vector2(6,-.3f);listenerEnemy.awareness=Awareness.Roam;TickTelevisions(.1f);
   Check(listenerEnemy.awareness==Awareness.Investigate&&Vector2.Distance(listenerEnemy.goal,tv.p)<.01f,"television noise alerts an infected through the nearby exterior wall");
   listenerEnemy.hp=0;Save();ResetTelevisions();Check(Load()&&televisions[0].on,"television switch state survives save and reload");paused=true;
   hero.p=tv.p+Vector2.left;Check(UseTelevision()&&!tv.on&&!tv.sound.isPlaying&&!tv.glow.enabled,"E switches picture noise and glow off together");
   // Load actual old v9 data plus retained belongings, with no writes to the user profile.
   var fixturePath=Environment.GetEnvironmentVariable("ZONBEE_LEGACY_FIXTURE");string original=File.ReadAllText(fixturePath);var legacy=JsonUtility.FromJson<SaveData>(original);
   legacy.inventory.containers.Add(new DistrictContainerItems{index=0,items=new[]{new DistrictItem(DistrictItemKind.Alcohol,2),new DistrictItem(DistrictItemKind.Axe,1),default,default,default,default}});
   legacy.x=12;legacy.z=-14;legacy.enemies.First(e=>e.id==0).hp=0;legacy.enemies.First(e=>e.id==1).hp=80;
   string slots=JsonUtility.ToJson(new SlotSnapshot{items=legacy.inventory.slots});float oldHp=legacy.hp;string legacyJson=JsonUtility.ToJson(legacy);File.WriteAllText(SavePath,legacyJson);Check(Load(),"real v9 snapshot migrates to the curated district");paused=true;
   Check(File.ReadAllText(SavePath+".before-district-v9.json")==legacyJson,"migration archives exact original bytes outside rotating autosave backup");
   Check(File.ReadAllText(fixturePath)==original&&health==oldHp&&JsonUtility.ToJson(new SlotSnapshot{items=inventory.Data.slots})==slots,"migration preserves original file health and all carried items");
   Check(inventory.Data.overflow.Count==2&&inventory.Data.overflow[0].amount==2&&inventory.Data.overflow[1].kind==1&&inventory.Container(0).All(v=>v.Empty),"retired storage contents return once without granting unsearched loot");
   Check(infected.Find(e=>e.id==0).hp==0&&Mathf.Abs(infected.Find(e=>e.id==1).hp-97.5f)<.01f,"migration preserves dead infected and relative damage of surviving infected");
   Check(RoomAt(hero.p)==homes[6]&&!Blocked(hero.p),"old clinic position moves safely with the relocated home");
   Check(Load()&&inventory.Data.overflow.Count==2,"reloading migrated v10 cannot duplicate recovered items");paused=true;
   var snapshot=CaptureSave();snapshot.enemies[1].id=snapshot.enemies[0].id;File.WriteAllText(SavePath,JsonUtility.ToJson(snapshot));File.WriteAllText(SavePath+".bak","invalid");float before=health;
   Check(!Load()&&health==before,"duplicate enemy identity rejects corrupted save before mutating world");
   foreach(var enemy in infected){enemy.hp=0;enemy.root.gameObject.SetActive(false);}health=100;bleeding=false;infection=0;ResetTelevisions();progression.Data.minute=990;TickWorldLife(0);
   foreach(var h in buildings){hero.p=h.p+new Vector2(0,-1);hero.root.position=V(hero.p);current=h;foreach(var other in buildings){other.roof.SetActive(other!=h);foreach(var wall in other.cut)wall.SetActive(other!=h);}cam.transform.position=V(hero.p)+new Vector3(-20,25,-20);noticeTime=0;yield return CaptureGameCamera(Path.Combine(folder,"interior-"+homes.IndexOf(h)+".png"),5.8f);}
   hero.p=televisions[0].p+Vector2.left;current=televisions[0].home;foreach(var h in buildings){h.roof.SetActive(h!=current);foreach(var wall in h.cut)wall.SetActive(h!=current);}progression.Data.minute=1260;TickWorldLife(0);UseTelevision();yield return CaptureGameCamera(Path.Combine(folder,"television-evening.png"),3.8f);
   Check(televisions.Count==3&&interiorBatches.All(g=>g.GetComponentsInChildren<Renderer>().Any(r=>r.isPartOfStaticBatch)),"three furnished interiors use runtime static batching and independent televisions");
   Application.Quit();
  }
  [Serializable] sealed class SlotSnapshot {public DistrictItem[] items;}
 }
}
#endif
