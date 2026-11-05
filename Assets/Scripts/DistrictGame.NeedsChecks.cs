#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckNeeds(Action<bool,string> check,string folder){
   foreach(var enemy in infected)enemy.hp=0;
   inventory.Load(new DistrictInventoryData());inventory.TryAdd(DistrictItemKind.Food,3);inventory.TryAdd(DistrictItemKind.Water,3);
   health=100;attack=heroStagger=0;bleeding=false;infection=drunkSeconds=0;CancelMedical();
   nourishment=70;hydration=60;tiredness=15;
   for(int i=0;i<900;i++)TickNeeds(1,false);
   File.WriteAllText(Path.Combine(folder,"needs-15-minute-model.txt"),$"900 simulated seconds walking: satiety {nourishment:F1}, water {hydration:F1}, fatigue {tiredness:F1}. This is a model check, not a manual 15-minute session.");
   check(nourishment>49&&nourishment<51&&hydration>28&&hydration<29&&tiredness>30&&tiredness<31,"15 minute walking model creates thirst while keeping food and fatigue manageable");
   int water=inventory.Count(DistrictItemKind.Water);inventorySelection=Array.FindIndex(inventory.Data.slots,i=>i.kind==9);
   check(BeginMedical(MedicalAction.Water),"selected drinking water starts timed action");TickMedical(1);CancelMedical();
   check(inventory.Count(DistrictItemKind.Water)==water,"interrupted drinking preserves water");
   check(BeginMedical(MedicalAction.Water),"water action can restart");yield return new WaitForSeconds(1.8f);
   check(inventory.Count(DistrictItemKind.Water)==2&&hydration>55,"drinking spends one sip and replenishes water through Update");
   int meals=food;float before=nourishment;inventorySelection=Array.FindIndex(inventory.Data.slots,i=>i.kind==3);
   check(BeginMedical(MedicalAction.Eat),"selected food starts eating");yield return new WaitForSeconds(2);
   check(food==meals-1&&nourishment>before+34&&health==100,"food spends one portion and restores satiety");
   tiredness=80;hydration=10;nourishment=10;
   check(StaminaLimit==65&&StaminaRecovery<.5f,"severe needs limit endurance and recovery");
   hero.p=refuge;campEstablished=true;bleeding=true;
   check(!BeginMedical(MedicalAction.Rest),"bleeding prevents safe rest");bleeding=false;infection=0;
   check(BeginMedical(MedicalAction.Rest),"established safe shelter permits rest");
   var threat=infected[0];threat.hp=threat.maxHp;threat.p=hero.p+Vector2.right*3;TickMedical(.1f);
   check(medicalAction==MedicalAction.None&&tiredness>=80,"approaching infected interrupts rest without recovery");threat.hp=0;
   hydration=60;nourishment=70;
   check(BeginMedical(MedicalAction.Rest),"rest restarts after danger clears");yield return new WaitForSeconds(6.3f);
   check(tiredness<36&&StaminaRecovery>.7f,"completed rest reduces fatigue and restores recovery");
   Save();float saved=hydration;nourishment=0;hydration=0;tiredness=100;
   check(Load()&&Mathf.Abs(hydration-saved)<.01f&&inventory.Count(DistrictItemKind.Water)==2,"needs and partial water bottle persist together");
   string snapshot=File.ReadAllText(SavePath);var old=JsonUtility.FromJson<SaveData>(snapshot);old.version=5;File.WriteAllText(SavePath,JsonUtility.ToJson(old));
   check(Load()&&nourishment==70&&hydration==60&&tiredness==15,"v5 saves migrate with healthy starting needs");
   var bad=JsonUtility.FromJson<SaveData>(snapshot);bad.hydration=float.NaN;File.WriteAllText(SavePath,JsonUtility.ToJson(bad));File.WriteAllText(SavePath+".bak","broken");saved=hydration;
   check(!Load()&&hydration==saved,"invalid needs are rejected before world mutation");
   File.WriteAllText(SavePath,snapshot);check(Load(),"valid v6 save loads after rejected payload");
  }
 }
}
#endif
