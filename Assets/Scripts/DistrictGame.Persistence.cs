using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  [Serializable] sealed class EnemySave {public int id;public float x,z,hp,yaw;}
  static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
  static bool ValidPosition(float x,float z)=>Finite(x)&&Finite(z)&&InsideWorld(new Vector2(x,z));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  string legacySaveOverride;
#endif
  string LegacySavePath {get{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   if(legacySaveOverride!=null)return legacySaveOverride;
#endif
   return Path.Combine(Directory.GetParent(Application.persistentDataPath).FullName,"Berezki Last Frequency","district-v1.json");}}
  SaveData CaptureSave(){
   var s=new SaveData{televisions=CaptureTelevisions(),windowState=CaptureWindows(),barricades=CaptureBoards(),backDoors=CaptureBackExits(),exploration=GardenDoorOccupied()?explorationFlags|GardenOpen:explorationFlags,progression=progression.Data,nourishment=nourishment,hydration=hydration,tiredness=tiredness,water=(bool[])waterTaken.Clone(),inventory=inventory.Data,weapon=weapon,food=food,bandages=bandages,x=ClimbSavePosition.x,z=ClimbSavePosition.y,hp=health,stamina=stamina,heard=heard,camp=campEstablished,depot=depotLooted,bleeding=bleeding,infection=infection,drunk=drunkSeconds,alcohol=alcoholDoses,sterile=sterileBandages,stones=stones,supplies=new bool[supplies.Count],taken=new bool[homes.Count*3],doors=new bool[homes.Count],enemies=new EnemySave[infected.Count]};
   for(int n=0;n<homes.Count;n++){s.doors[n]=homes[n].open||DoorwayOccupied(homes[n]);for(int i=0;i<3;i++)s.taken[n*3+i]=homes[n].taken[i];}
   for(int n=0;n<infected.Count;n++){var e=infected[n];s.enemies[n]=new EnemySave{id=e.id,x=e.p.x,z=e.p.y,hp=Mathf.Max(0,e.hp),yaw=e.root.eulerAngles.y};}
   for(int n=0;n<supplies.Count;n++)s.supplies[n]=supplies[n].taken;
   return s;
  }
  SaveData ReadSave(string path){
   var s=JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
   if(s==null||s.televisions<0||s.televisions>7||s.barricades<0||s.barricades>15||s.windowState<0||s.windowState>10||(s.windowState&3)>2||((s.windowState>>2)&3)>2||s.backDoors<0||s.backDoors>3||s.exploration<0||s.exploration>63||(s.version<1||s.version>10)||!ValidPosition(s.x,s.z)||!Finite(s.hp)||!Finite(s.stamina)||s.hp<=0||s.hp>100||s.stamina<0||s.stamina>100||s.weapon<0||s.weapon>2||s.food<0||s.bandages<0||s.taken==null||s.taken.Length!=(s.version<3?16:homes.Count)*3||s.doors==null||s.doors.Length!=(s.version<3?16:homes.Count))throw new InvalidDataException("Неверный формат сохранения");
   if(s.version>=4&&(s.alcohol<0||s.alcohol>10000||s.sterile<0||s.sterile>10000||s.stones<0||s.stones>10000||!Finite(s.infection)||s.infection<0||s.infection>100||!Finite(s.drunk)||s.drunk<0||s.drunk>90||s.supplies==null||s.supplies.Length!=supplies.Count))throw new InvalidDataException("Неверное состояние ранений и припасов");
   if(s.version>=6&&(!Finite(s.nourishment)||s.nourishment<0||s.nourishment>100||!Finite(s.hydration)||s.hydration<0||s.hydration>100||!Finite(s.tiredness)||s.tiredness<0||s.tiredness>100||s.water==null||s.water.Length!=3))throw new InvalidDataException("Неверные потребности");
   if(s.version>=9)DistrictProgression.Validate(s.progression);
   if(s.version>=5)DistrictInventory.Validate(s.inventory,homes.Count*3);
   if(s.version>=2){
    if(s.enemies==null||s.enemies.Length!=(s.version==2?30:s.version<10?39:infected.Count))throw new InvalidDataException("Неполное состояние мира");
    var seen=new HashSet<int>();
    foreach(var e in s.enemies){var actor=e==null?null:infected.Find(z=>z.id==e.id);if(e==null||(s.version<10?!LegacyCuratedEnemyId(e.id,s.version):actor==null)||!seen.Add(e.id)||!ValidPosition(e.x,e.z)||!Finite(e.hp)||e.hp<0||e.hp>(s.version<=3?LegacyEnemyHealth(e.id):s.version<10?PreviousEnemyHealth(e.id):actor.maxHp)||!Finite(e.yaw))throw new InvalidDataException("Неверное состояние заражённого");}
   }
   return s;
  }
  void Save(){
   if(!playing||dead)return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-district-check")>=0&&testSave==null)return;
#endif
   try {
    File.WriteAllText(SavePath+".tmp",JsonUtility.ToJson(CaptureSave()));
    // Only a validated previous snapshot may replace the recovery copy.
    bool valid=false;if(File.Exists(SavePath)){try{ReadSave(SavePath);valid=true;}catch{}}
    if(File.Exists(SavePath))File.Replace(SavePath+".tmp",SavePath,valid?SavePath+".bak":null);
    else File.Move(SavePath+".tmp",SavePath);
   }catch(Exception e){Debug.LogWarning("District save: "+e.Message);Say("Не удалось сохранить мир");}
  }
  bool Load(){
   SaveData s;bool recovered=false,imported=false;string loadedFrom=SavePath;
   try{s=ReadSave(SavePath);}catch{
    try{s=ReadSave(SavePath+".bak");loadedFrom=SavePath+".bak";recovered=true;}catch{
     bool legacyAllowed=testSave==null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
     legacyAllowed|=legacySaveOverride!=null;
#endif
     if(!legacyAllowed||File.Exists(SavePath)||File.Exists(SavePath+".bak")){Say("Сохранение и резервная копия недоступны.");return false;}
     string legacy=LegacySavePath;
     try{s=ReadSave(legacy);loadedFrom=legacy;imported=true;}catch{try{s=ReadSave(legacy+".bak");loadedFrom=legacy+".bak";imported=true;}catch{Say("Сохранение недоступно. Можно начать новую игру.");return false;}}
    }
   }
   // No world state changes until the complete snapshot has passed validation.
   if(s.version<10){try{string archive=SavePath+".before-district-v"+s.version+".json";if(!File.Exists(archive))File.Copy(loadedFrom,archive);}catch(Exception e){Debug.LogWarning("District migration backup: "+e.Message);Say("Не удалось сохранить копию старого мира. Загрузка отменена.");return false;}}
   RestoreBackExits(s.backDoors);RestoreFortifications(s.barricades,s.windowState);explorationFlags=s.exploration;RefreshExploration(true);
   worldGeneration++;ClearLootPreview();approachHome=null;approachTurning=false;trainingRadio=false;journalOpen=false;ClearPcCombat();ClearStorageFocus();containerAudio.SetSearching(false);CancelMedical();suppliesOpen=false;throwCooldown=0;heroVelocity=Vector2.zero;heroStagger=0;searchSlot=-1;attack=0;damageDelay=0;radioOn=false;voice.Stop();staticSound.Stop();
   for(int n=0;n<homes.Count;n++){var h=homes[n];h.open=n<s.doors.Length&&s.doors[n];SnapDoor(h);for(int i=0;i<3;i++)h.taken[i]=n*3+i<s.taken.Length&&s.taken[n*3+i];RefreshContainers(h);}
   foreach(var actor in infected){
    var e=s.version>=2?Array.Find(s.enemies,v=>v.id==actor.id):null;
    actor.p=e==null?actor.spawn:e.hp>0?ResolveSolidPosition(new Vector2(e.x,e.z)):new Vector2(e.x,e.z);actor.hp=e==null?actor.maxHp:s.version<10?e.hp/(s.version<=3?LegacyEnemyHealth(actor.id):PreviousEnemyHealth(actor.id))*actor.maxHp:e.hp;
    actor.callCooldown=0;actor.awareness=Awareness.Roam;actor.searchClock=actor.searchPause=actor.unreachable=0;actor.searchStep=0;actor.searchOrigin=actor.p;actor.goal=actor.p;actor.alert=actor.wind=actor.rest=actor.repath=actor.stagger=actor.recovery=0;actor.striking=false;actor.path.Clear();actor.root.position=V(actor.p);actor.root.rotation=Quaternion.Euler(0,e==null?0:e.yaw,0);
    if(actor.visual)actor.visual.Restore(actor.hp<=0);
   }
   Vector2 restoredPosition=new Vector2(s.x,s.z);if(s.version<10&&Mathf.Abs(s.x-12)<4.5f&&Mathf.Abs(s.z+12)<4.5f)restoredPosition+=homes[6].p-new Vector2(12,-12);hero.p=ResolveSolidPosition(restoredPosition);if(Blocked(hero.p))hero.p=new Vector2(-12,-20);
   health=s.hp;stamina=s.stamina;heard=s.heard;campEstablished=s.version>=2&&s.camp;depotLooted=s.version>=3&&s.depot;
   inventory.Load(s.version>=5?s.inventory:new DistrictInventoryData());
   if(s.version<10)RecoverRetiredStorage();
   ResetTelevisions();RestoreTelevisions(s.televisions);
   if(s.version<5){
    weapon=s.weapon;food=s.food;bandages=s.bandages;
    alcoholDoses=s.version>=4?s.alcohol:0;sterileBandages=s.version>=4?s.sterile:0;stones=s.version>=4?s.stones:0;
   }
   if(s.version<8){inventory.Data.shirt=inventory.Data.trousers=inventory.Data.shoes=true;inventory.Data.clothingFound=7;}
   UpdateClothing();progression.Load(s.version>=9?s.progression:new DistrictProgressionData());atmosphere.SetTime(progression.Data.minute/60);RefreshLifeFinds();
   nourishment=s.version>=6?s.nourishment:70;hydration=s.version>=6?s.hydration:60;tiredness=s.version>=6?s.tiredness:15;
   for(int i=0;i<waterTaken.Length;i++){waterTaken[i]=s.version>=6&&s.water[i];waterModels[i].SetActive(!waterTaken[i]);}
   bleeding=s.version>=4&&s.bleeding;infection=s.version>=4?s.infection:0;drunkSeconds=s.version>=4?s.drunk:0;
   inventorySelection=dragSlot=-1;inventoryDropPending=inventoryMouseHeld=false;pointer.Reset();findUntil=0;
   if(backpackFind)backpackFind.SetActive(!inventory.Data.bagFound);
   for(int n=0;n<supplies.Count;n++){supplies[n].taken=s.version>=4&&s.supplies[n];supplies[n].model.SetActive(!supplies[n].taken);}
   hero.root.position=V(hero.p);hero.root.rotation=Quaternion.identity;if(hero.visual)hero.visual.Revive();
   dead=won=paused=false;playing=true;autosave=0;
   if(imported||s.version<10)Save();
   Say(imported?"Сохранение перенесено в Zonbee":recovered?"Мир восстановлен из резервной копии":s.version==1?"Старое сохранение загружено. Состояние заражённых начальное.":"Мир восстановлен");return true;
  }
 }
}
