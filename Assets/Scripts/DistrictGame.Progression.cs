using System;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  readonly DistrictProgression progression=new DistrictProgression();
  static readonly string[] SkillNames={"Сила","Ловкость","Выносливость","Механика","Ремесло"};
  int readingBook,activeRecipe,selectedFood=3;
  float progressionTaskDuration=360,workNoise;
  bool journalOpen,trainingRadio;
  int journalTab;
  sealed class Recipe {
   public string name,detail;public DistrictItem[] needs;public DistrictItem result;public int level;public float seconds,xp;
   public Recipe(string name,string detail,DistrictItem[] needs,DistrictItem result,int level,float seconds,float xp){this.name=name;this.detail=detail;this.needs=needs;this.result=result;this.level=level;this.seconds=seconds;this.xp=xp;}
  }
  static DistrictItem Ingredient(DistrictItemKind kind,int amount)=>new DistrictItem(kind,amount);
  static readonly Recipe[] Recipes={
   new Recipe("Полоски ткани → бинт","2 лоскута · обычная перевязка",new[]{Ingredient(DistrictItemKind.Cloth,2)},Ingredient(DistrictItemKind.Bandage,1),0,8,8),
   new Recipe("Распороть майку","1 майка из рюкзака · 3 лоскута",new[]{Ingredient(DistrictItemKind.Shirt,1)},Ingredient(DistrictItemKind.Cloth,3),0,10,5),
   new Recipe("Самодельное копьё","2 доски, 2 металла, проволока и лоскут",new[]{Ingredient(DistrictItemKind.Wood,2),Ingredient(DistrictItemKind.Scrap,2),Ingredient(DistrictItemKind.Wire,1),Ingredient(DistrictItemKind.Cloth,1)},Ingredient(DistrictItemKind.Spear,1),1,30,18)
  };
  bool TryPack(DistrictItem[] items,bool commit){
   var candidate=new DistrictInventory();candidate.Load(JsonUtility.FromJson<DistrictInventoryData>(JsonUtility.ToJson(inventory.Data)));
   foreach(var item in items)if(!candidate.TryAdd((DistrictItemKind)item.kind,item.amount))return false;
   if(commit)inventory.Load(candidate.Data);return true;
  }
  bool RecipeFits(int index,bool commit){
   var r=Recipes[index];if(progression.Level(DistrictSkill.Craft)<r.level)return false;
   var candidate=new DistrictInventory();candidate.Load(JsonUtility.FromJson<DistrictInventoryData>(JsonUtility.ToJson(inventory.Data)));
   foreach(var need in r.needs)if(!candidate.Remove((DistrictItemKind)need.kind,need.amount))return false;
   if(!candidate.TryAdd((DistrictItemKind)r.result.kind,r.result.amount))return false;
   if(commit)inventory.Load(candidate.Data);return true;
  }
  string RecipeReason(int index){var r=Recipes[index];if(progression.Level(DistrictSkill.Craft)<r.level)return "Нужно ремесло "+r.level;foreach(var need in r.needs)if(inventory.Count((DistrictItemKind)need.kind)<need.amount)return "Нужно: "+ItemNames[need.kind]+" ×"+need.amount;return RecipeFits(index,false)?null:"Нет места для результата";}
  bool CanReadLight=>atmosphere.Daylight>.23f||atmosphere.FlashlightOn||current!=null&&homes.IndexOf(current)%3==2;
  bool HasBook(int book)=>inventory.Count(book==0?DistrictItemKind.MechanicsBook:DistrictItemKind.CraftBook)>0;
  bool StartReading(int book){
   if(medicalAction!=MedicalAction.None||book<0||book>1)return false;
   readingBook=book;progressionTaskDuration=DistrictProgression.ChapterSeconds-progression.Data.books[book]%DistrictProgression.ChapterSeconds;
   return BeginMedical(MedicalAction.Read);
  }
  bool StartRecipe(int index){if(medicalAction!=MedicalAction.None||index<0||index>=Recipes.Length)return false;activeRecipe=index;return BeginMedical(MedicalAction.Make);}
  bool NearEngine=>Vector2.Distance(hero.p,enginePosition)<1.9f&&Sight(hero.p,enginePosition+(hero.p-enginePosition).normalized*.9f);
  DistrictItem[] EngineYield()=>new[]{Ingredient(DistrictItemKind.Scrap,4+Mathf.Min(2,Mathf.Max(0,progression.Level(DistrictSkill.Mechanics)-1))),Ingredient(DistrictItemKind.Wire,1)};
  bool CanProgressionTask(MedicalAction action){
   if(action==MedicalAction.Read)return HasBook(readingBook)&&progression.Data.books[readingBook]<DistrictProgression.BookSeconds&&CanReadLight&&!bleeding&&infection<50;
   if(action==MedicalAction.Make)return RecipeFits(activeRecipe,false);
   if(action==MedicalAction.Salvage)return NearEngine&&!progression.Data.engineDismantled&&progression.Level(DistrictSkill.Mechanics)>=1&&inventory.Count(DistrictItemKind.Wrench)>0&&TryPack(EngineYield(),false);
   return false;
  }
  string ProgressionReason(MedicalAction action){
   if(action==MedicalAction.Make)return RecipeReason(activeRecipe);
   if(action==MedicalAction.Read)return !HasBook(readingBook)?"Нужна книга в рюкзаке":progression.Data.books[readingBook]>=DistrictProgression.BookSeconds?"Книга изучена":bleeding?"Сначала останови кровь":infection>=50?"Сначала обработай рану":!CanReadLight?"Нужен свет для чтения":null;
   return !NearEngine?"Подойди к двигателю на АЗС":progression.Data.engineDismantled?"Двигатель уже разобран":progression.Level(DistrictSkill.Mechanics)<1?"Нужна механика 1":inventory.Count(DistrictItemKind.Wrench)==0?"Нужен гаечный ключ":!TryPack(EngineYield(),false)?"Освободи место под металл и провод":null;
  }
  bool TickReading(float dt){
   if(medicalAction!=MedicalAction.Read)return false;
   if(!CanProgressionTask(MedicalAction.Read)){CancelMedical();Say("Чтение прервано. Прочитанное сохранено.");return true;}
   workNoise-=dt;if(workNoise<=0){workNoise=25;if(foley)foley.PageTurn();}
   float amount=Mathf.Min(dt,Mathf.Max(0,progressionTaskDuration-medicalTime));int chapters=progression.Read(readingBook,amount);medicalTime+=amount;
   if(chapters>0||medicalTime>=progressionTaskDuration-.001f){CancelMedical();Save();Say("Глава изучена · +100 опыта: "+SkillNames[readingBook==0?3:4]+". Следующую главу можно начать позже.");}
   return true;
  }
  void FinishProgressionTask(MedicalAction action){
   if(action==MedicalAction.Make&&RecipeFits(activeRecipe,true)){progression.Gain(DistrictSkill.Craft,Recipes[activeRecipe].xp);Say("Изготовлено: "+ItemNames[Recipes[activeRecipe].result.kind]);}
   if(action==MedicalAction.Salvage&&CanProgressionTask(action)&&TryPack(EngineYield(),true)){progression.Data.engineDismantled=true;engineModel.SetActive(false);progression.Gain(DistrictSkill.Mechanics,20);Say("Двигатель разобран. Металл и провод убраны в рюкзак.");}
   Save();
  }
  static bool Edible(int kind)=>kind==3||kind>=14&&kind<=16;
  bool HasAnyMeal=>food>0||inventory.Count(DistrictItemKind.Crackers)>0||inventory.Count(DistrictItemKind.Beans)>0||inventory.Count(DistrictItemKind.Chocolate)>0;
  int FirstMealKind(){foreach(int kind in new[]{3,14,15,16})if(inventory.Count((DistrictItemKind)kind)>0)return kind;return 3;}
  bool SpendCampMeal()=>HasAnyMeal&&inventory.Remove((DistrictItemKind)FirstMealKind(),1);
  bool HasMeal=>inventory.Count((DistrictItemKind)selectedFood)>0&&Edible(selectedFood);
  bool EatSelected(int slot){if(slot<0||slot>=inventory.Capacity||!Edible(inventory.Data.slots[slot].kind))return false;selectedFood=inventory.Data.slots[slot].kind;inventorySelection=slot;return BeginMedical(MedicalAction.Eat);}
  float MealNourishment=>selectedFood==14?22:selectedFood==15?32:selectedFood==16?18:35;
  void EatMeal(){
   if(!inventory.Remove((DistrictItemKind)selectedFood,1))return;
   nourishment=Mathf.Min(100,nourishment+MealNourishment);hydration=Mathf.Clamp(hydration+(selectedFood==14?-4:selectedFood==15?6:0),0,100);stamina=Mathf.Min(StaminaLimit,stamina+(selectedFood==16?16:8));
   Say(selectedFood==14?"Сухари утолили голод, но захотелось пить.":selectedFood==15?"Поел фасоли. Немного восстановил запас воды.":selectedFood==16?"Шоколад дал немного сил.":"Поел. Голод отступил.");
  }
  float SearchDuration(Home h,int slot)=>h.seconds[slot]*(1-.04f*progression.Level(DistrictSkill.Agility));
  void PracticeMovement(Vector2 movement,bool running){float distance=movement.magnitude;if(distance<=0)return;progression.Gain(DistrictSkill.Endurance,distance*(running?.03f:.004f));if(crouching)progression.Gain(DistrictSkill.Agility,distance*.04f);}
  Strike SkilledStrike(Strike strike){int level=progression.Level(DistrictSkill.Strength);strike.damage*=1+level*(weapon==0?.06f:.03f);if(strike.shove)strike.push*=1+level*.04f;return strike;}
  void TuneTrainingRadio(){
   if(Vector2.Distance(hero.p,radioPos)>2||!Sight(hero.p,radioPos))return;
   radioOn=trainingRadio=true;voice.Stop();staticSound.Play();radioSpeaking=false;radioPulse=0;journalOpen=false;Alert(radioPos,25);Say("Учебная частота · ремесло. Слушай рядом с приёмником. Звук привлекает заражённых.");
  }
  string TrainingLine=>progression.Data.lessonSeconds<30?"Домашняя мастерская: соединяй сухие детали, проверяй крепление перед работой.":progression.Data.lessonSeconds<60?"Сначала подготовь материал и инструмент. Повторное использование экономит запасы.":"Не спеши: аккуратная работа сохраняет материал. На сегодня урок окончен.";
  void TickTraining(float dt){
   var data=progression.Data;if(data.lessonDay!=data.day){data.lessonDay=data.day;data.lessonSeconds=0;}
   if(data.lessonCompletedDay==data.day||Vector2.Distance(hero.p,radioPos)>6||!Sight(radioPos,hero.p))return;
   data.lessonSeconds=Mathf.Min(90,data.lessonSeconds+dt);
   if(data.lessonSeconds>=90){data.lessonCompletedDay=data.day;progression.Gain(DistrictSkill.Craft,35);Save();Say("Учебная передача усвоена · +35 опыта ремесла. Новая программа завтра.");}
  }
 }
}
