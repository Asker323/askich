using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  [Serializable] sealed class EnemySave {public int id;public float x,z,hp,yaw;}
  static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
  static bool ValidPosition(float x,float z)=>Finite(x)&&Finite(z)&&InsideWorld(new Vector2(x,z));
  SaveData CaptureSave(){
   var s=new SaveData{weapon=weapon,food=food,bandages=bandages,x=hero.p.x,z=hero.p.y,hp=health,stamina=stamina,heard=heard,camp=campEstablished,depot=depotLooted,bleeding=bleeding,infection=infection,drunk=drunkSeconds,alcohol=alcoholDoses,sterile=sterileBandages,stones=stones,supplies=new bool[supplies.Count],taken=new bool[homes.Count*3],doors=new bool[homes.Count],enemies=new EnemySave[infected.Count]};
   for(int n=0;n<homes.Count;n++){s.doors[n]=homes[n].open;for(int i=0;i<3;i++)s.taken[n*3+i]=homes[n].taken[i];}
   for(int n=0;n<infected.Count;n++){var e=infected[n];s.enemies[n]=new EnemySave{id=e.id,x=e.p.x,z=e.p.y,hp=Mathf.Max(0,e.hp),yaw=e.root.eulerAngles.y};}
   for(int n=0;n<supplies.Count;n++)s.supplies[n]=supplies[n].taken;
   return s;
  }
  SaveData ReadSave(string path){
   var s=JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
   if(s==null||(s.version<1||s.version>4)||!ValidPosition(s.x,s.z)||!Finite(s.hp)||!Finite(s.stamina)||s.hp<=0||s.hp>100||s.stamina<0||s.stamina>100||s.weapon<0||s.weapon>2||s.food<0||s.bandages<0||s.taken==null||s.taken.Length!=(s.version<3?16:homes.Count)*3||s.doors==null||s.doors.Length!=(s.version<3?16:homes.Count))throw new InvalidDataException("Неверный формат сохранения");
   if(s.version>=4&&(s.alcohol<0||s.alcohol>10000||s.sterile<0||s.sterile>10000||s.stones<0||s.stones>10000||!Finite(s.infection)||s.infection<0||s.infection>100||!Finite(s.drunk)||s.drunk<0||s.drunk>90||s.supplies==null||s.supplies.Length!=supplies.Count))throw new InvalidDataException("Неверное состояние ранений и припасов");
   if(s.version>=2){
    if(s.enemies==null||s.enemies.Length!=(s.version==2?legacyEnemyCount:infected.Count))throw new InvalidDataException("Неполное состояние мира");
    var seen=new HashSet<int>();
    foreach(var e in s.enemies){var actor=e==null?null:infected.Find(z=>z.id==e.id);if(actor==null||(s.version==2&&e.id>=100)||!seen.Add(e.id)||!ValidPosition(e.x,e.z)||!Finite(e.hp)||e.hp<0||e.hp>(s.version<=3?LegacyEnemyHealth(actor.id):actor.maxHp)||!Finite(e.yaw))throw new InvalidDataException("Неверное состояние заражённого");}
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
   SaveData s;bool recovered=false;
   try{s=ReadSave(SavePath);}catch{
    try{s=ReadSave(SavePath+".bak");recovered=true;}catch{Say("Сохранение и резервная копия недоступны. Можно начать новую игру.");return false;}
   }
   // No world state changes until the complete snapshot has passed validation.
   worldGeneration++;CancelMedical();suppliesOpen=false;throwCooldown=0;heroVelocity=Vector2.zero;heroStagger=0;searchSlot=-1;attack=0;damageDelay=0;radioOn=false;voice.Stop();staticSound.Stop();
   for(int n=0;n<homes.Count;n++){var h=homes[n];h.open=n<s.doors.Length&&s.doors[n];h.gate.active=!h.open;h.door.SetActive(!h.open);for(int i=0;i<3;i++)h.taken[i]=n*3+i<s.taken.Length&&s.taken[n*3+i];RefreshContainers(h);}
   foreach(var actor in infected){
    var e=s.version>=2?Array.Find(s.enemies,v=>v.id==actor.id):null;
    actor.p=e==null?actor.spawn:new Vector2(e.x,e.z);actor.hp=e==null?actor.maxHp:s.version<=3?e.hp/LegacyEnemyHealth(actor.id)*actor.maxHp:e.hp;
    actor.awareness=Awareness.Roam;actor.searchClock=actor.unreachable=0;actor.searchOrigin=actor.p;actor.goal=actor.p;actor.alert=actor.wind=actor.rest=actor.repath=actor.stagger=actor.recovery=0;actor.striking=false;actor.path.Clear();actor.root.position=V(actor.p);actor.root.rotation=Quaternion.Euler(0,e==null?0:e.yaw,0);
    if(actor.visual)actor.visual.Restore(actor.hp<=0);
   }
   hero.p=new Vector2(s.x,s.z);if(Blocked(hero.p))hero.p=new Vector2(-12,-20);
   health=s.hp;stamina=s.stamina;weapon=s.weapon;food=s.food;bandages=s.bandages;heard=s.heard;campEstablished=s.version>=2&&s.camp;depotLooted=s.version>=3&&s.depot;
   bleeding=s.version>=4&&s.bleeding;infection=s.version>=4?s.infection:0;drunkSeconds=s.version>=4?s.drunk:0;alcoholDoses=s.version>=4?s.alcohol:0;sterileBandages=s.version>=4?s.sterile:0;stones=s.version>=4?s.stones:0;
   for(int n=0;n<supplies.Count;n++){supplies[n].taken=s.version>=4&&s.supplies[n];supplies[n].model.SetActive(!supplies[n].taken);}
   hero.root.position=V(hero.p);hero.root.rotation=Quaternion.identity;if(hero.visual)hero.visual.Revive();
   dead=won=paused=false;playing=true;autosave=0;
   Say(recovered?"Мир восстановлен из резервной копии":s.version==1?"Старое сохранение загружено. Состояние заражённых начальное.":"Мир восстановлен");return true;
  }
 }
}
