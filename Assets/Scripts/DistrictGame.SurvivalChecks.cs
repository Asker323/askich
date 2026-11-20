#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckSurvival(Action<bool,string> check,string folder){
   foreach(var z in infected){z.hp=0;z.striking=false;if(z.visual)z.visual.Die();}
   attack=heroStagger=0;heroVelocity=Vector2.zero;paused=dead=suppliesOpen=false;playing=true;health=100;bleeding=false;infection=drunkSeconds=0;CancelMedical();
   hero.p=supplies[0].p;alcoholDoses=0;supplies[0].taken=false;supplies[0].model.SetActive(true);
   check(UseSupply()&&alcoholDoses==3&&supplies[0].taken,"alcohol bottle grants exactly three doses");
   check(!UseSupply()&&alcoholDoses==3,"bottle cannot be picked up twice");
   WoundHero();float wounded=health;TickAfflictions(10);check(bleeding&&health<wounded&&infection>12,"untreated bleeding and infection continue over time");
   hero.p=refuge;campEstablished=true;food=1;Use();check(bleeding&&infection>0,"shelter and food do not cure wounds");
   bandages=3;check(BeginMedical(MedicalAction.Bandage),"bandage can treat bleeding at full health");TickMedical(1);CancelMedical();check(bandages==3&&bleeding,"interrupted treatment consumes no bandage");
   BeginMedical(MedicalAction.Bandage);TickMedical(2.5f);check(!bleeding&&infection>0&&bandages==2,"ordinary bandage stops blood but leaves infection");
   bandages=3;sterileBandages=0;
   for(int i=0;i<3;i++){check(BeginMedical(MedicalAction.Craft),"craft dose "+(i+1));TickMedical(3);}
   check(alcoholDoses==0&&bandages==0&&sterileBandages==3&&!BeginMedical(MedicalAction.Craft),"three dose bottle supports three crafts without negative inventory");
   alcoholDoses=1;WoundHero();float priorInfection=infection;BeginMedical(MedicalAction.Drink);TickMedical(1.2f);
   check(alcoholDoses==0&&drunkSeconds==45&&bleeding&&infection==priorInfection,"drinking spends dose and intoxicates without curing wound");
   BeginMedical(MedicalAction.Sterile);TickMedical(2.5f);check(!bleeding&&infection==0&&sterileBandages==2&&drunkSeconds>0,"sterile bandage cures wound and bleeding but not intoxication");
   drunkSeconds=0;bandages=1;alcoholDoses=1;int crafted=sterileBandages;BeginMedical(MedicalAction.Craft);yield return new WaitForSeconds(3.25f);
   check(sterileBandages==crafted+1&&alcoholDoses==0,"craft completes through real player Update");
   bleeding=true;infection=37;drunkSeconds=28;alcoholDoses=2;stones=3;Save();string snapshot=File.ReadAllText(SavePath);
   bleeding=false;infection=drunkSeconds=0;alcoholDoses=stones=0;supplies[0].taken=false;
   check(Load()&&bleeding&&infection==37&&drunkSeconds==28&&alcoholDoses==2&&stones==3&&supplies[0].taken,"wounds intoxication doses and pickup state survive save load");
   var malformed=JsonUtility.FromJson<SaveData>(snapshot);malformed.alcohol=-1;File.WriteAllText(SavePath,JsonUtility.ToJson(malformed));File.WriteAllText(SavePath+".bak","broken");
   check(!Load()&&alcoholDoses==2&&bleeding,"invalid medical inventory rejected without mutation");
   var legacy=JsonUtility.FromJson<SaveData>(snapshot);legacy.version=3;PopulateLegacyTestEnemies(legacy);foreach(var e in legacy.enemies)e.hp=e.id%3==0?LegacyEnemyHealth(e.id)/2:0;File.WriteAllText(SavePath,JsonUtility.ToJson(legacy));
   check(Load()&&!bleeding&&infection==0&&alcoholDoses==0&&supplies.TrueForAll(s=>!s.taken),"v3 saves migrate without invented wounds and with new pickups");
   var foe=infected.Find(z=>z.id%3==0);check(Mathf.Abs(foe.hp-foe.maxHp/2)<.01f,"legacy enemy health fraction migrates to hardcore balance");
   foreach(var z in infected){z.hp=0;z.striking=false;z.stagger=z.recovery=0;}
   foe.hp=foe.maxHp;foe.visual.Revive();var house=homes[5];house.open=false;SnapDoor(house);
   foe.p=house.p+Vector2.down*6;foe.root.rotation=Quaternion.identity;hero.p=house.p+Vector2.down*5;ObserveEnemy(foe,0);Vector2 last=foe.goal;
   check(foe.awareness==Awareness.Chase,"visible hero starts pursuit");hero.p=house.p+Vector2.down*2;ObserveEnemy(foe,.1f);
   check(foe.awareness==Awareness.Investigate&&foe.goal==last&&foe.goal!=hero.p,"closed door breaks sight and keeps last seen position");
   hero.p+=Vector2.right;ObserveEnemy(foe,.1f);check(foe.goal==last,"hidden movement does not update enemy goal");foe.p=last;ObserveEnemy(foe,.1f);check(foe.awareness==Awareness.Search,"enemy searches after reaching lost trail");ObserveEnemy(foe,7);check(foe.awareness==Awareness.Roam,"unsuccessful search eventually calms enemy");
   HearNoise(foe.p+Vector2.left*4,14);check(foe.awareness==Awareness.Investigate&&foe.goal==foe.p+Vector2.left*4,"noise directs investigation to sound location");
   foe.p=new Vector2(-24,-27);hero.p=new Vector2(-24,-24);foe.root.rotation=Quaternion.identity;ObserveEnemy(foe,0);HearNoise(foe.p+Vector2.left*5,14);check(foe.awareness==Awareness.Chase&&foe.goal==hero.p,"visible hero is not abandoned for distraction");
   foe.p=new Vector2(-16,-19);foe.awareness=Awareness.Roam;foe.alert=0;foe.stagger=100;foe.rest=100;foe.root.rotation=Quaternion.identity;face=Vector2.right;stones=1;throwCooldown=0;Vector2 landing=StoneTarget(face);
   check(ThrowStone()&&stones==0,"throw consumes one stone");yield return new WaitForSeconds(.7f);
   check(foe.awareness==Awareness.Investigate&&Vector2.Distance(foe.goal,landing)<.1f,"landed stone attracts nearby infected");
   bool wasTouch=TouchControls.ForceEnabled;TouchControls.ForceEnabled=true;
   check(PointerOnHud(InventoryButton.center)&&!InventoryButton.Overlaps(DistrictTouchControls.Hit),"inventory touch target is isolated from world attack");TouchControls.ForceEnabled=wasTouch;
   suppliesOpen=true;float energy=stamina;check(!TryAttack(Vector2.up)&&stamina==energy,"inventory clicks cannot trigger melee attacks");suppliesOpen=false;
   foe.hp=0;foe.visual.Die();hero.p=homes[6].p;bleeding=true;infection=38;drunkSeconds=25;bandages=2;alcoholDoses=2;sterileBandages=1;stones=2;health=80;zoom=8;yield return new WaitForSeconds(1);suppliesOpen=true;
   yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"survival-inventory.png"),frame.EncodeToPNG());Destroy(frame);suppliesOpen=false;
   foreach(var z in infected)z.hp=0;foe.hp=foe.maxHp;foe.visual.Revive();foe.p=hero.p+Vector2.down*6;foe.root.position=V(foe.p);foe.stagger=0;foe.rest=100;foe.awareness=Awareness.Search;foe.searchClock=6;foe.alert=6;
   yield return new WaitForEndOfFrame();frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"survival-wounds.png"),frame.EncodeToPNG());Destroy(frame);
   TouchControls.ForceEnabled=true;yield return null;yield return new WaitForEndOfFrame();frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"survival-touch.png"),frame.EncodeToPNG());Destroy(frame);TouchControls.ForceEnabled=false;
  }
 }
}
#endif
