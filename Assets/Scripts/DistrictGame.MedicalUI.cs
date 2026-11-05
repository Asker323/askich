using UnityEngine;

namespace Askich {
 public sealed partial class DistrictGame {
  static readonly Rect SuppliesArea=new Rect(160,130,960,494);
  static readonly Rect SuppliesClose=new Rect(950,151,140,50);
  static readonly Rect PlainAction=new Rect(850,252,240,64);
  static readonly Rect SterileAction=new Rect(850,354,240,64);
  static readonly Rect CraftAction=new Rect(850,456,240,56);
  static readonly Rect DrinkAction=new Rect(850,526,240,54);
  static readonly Rect MedicalProgressArea=new Rect(330,551,450,67);
  static readonly Rect CancelTreatment=new Rect(648,562,114,45);
  static readonly Color MedicalAccent=new Color(.63f,.76f,.65f);
  static readonly Color MedicalWarning=new Color(.94f,.53f,.34f);

  MedicalAction QuickDressing()=>sterileBandages>0&&(infection>0||bandages==0)?MedicalAction.Sterile:MedicalAction.Bandage;
  string QuickDressingLabel=>QuickDressing()==MedicalAction.Sterile?"СТЕРИЛЬНЫЙ · "+sterileBandages:"БИНТ · "+bandages;
  string MedicalUnavailable(MedicalAction action){
   if(medicalAction!=MedicalAction.None)return "Действие выполняется";
   if(attack>0||heroStagger>0)return "После удара";
   if(action==MedicalAction.Eat&&food==0)return "Нужна еда";
   if(action==MedicalAction.Water&&inventory.Count(DistrictItemKind.Water)==0)return "Нужна питьевая вода";
   if(action==MedicalAction.Refill&&!NearCistern)return "Подойди к резервуару";
   if(action==MedicalAction.Refill&&inventory.WaterSlot(true)<0)return "Нужна неполная бутылка";
   if(action==MedicalAction.Rest&&!SafeToRest())return bleeding?"Сначала останови кровь":"Отдых доступен в безопасном убежище";
   if((action==MedicalAction.Bandage||action==MedicalAction.Craft)&&bandages==0)return "Нужен обычный бинт";
   if(action==MedicalAction.Sterile&&sterileBandages==0)return "Нет стерильных бинтов";
   if((action==MedicalAction.Craft||action==MedicalAction.Drink)&&alcoholDoses==0)return "Нужен спирт";
   if(action==MedicalAction.Craft&&!inventory.CanCraft())return "Освободи ячейку";
   return CanTreat(action)?null:"Сейчас не требуется";
  }
  string TreatmentAdvice(){
   if(bleeding){
    if(sterileBandages>0)return "Используй стерильный бинт. Он остановит кровь и обработает рану.";
    if(bandages>0&&alcoholDoses>0)return "Стерилизуй бинт, затем примени его. Для крафта уйдёт одна доза спирта.";
    if(bandages>0)return "Обычный бинт остановит кровь. Для обработки раны ещё понадобится спирт.";
    return "Найди перевязочные материалы в доме или медпункте. Кровотечение отнимает здоровье.";
   }
   if(infection>0)return sterileBandages>0?"Кровь остановлена. Теперь обработай рану стерильным бинтом.":"Для обработки раны нужен стерильный бинт. Рецепт — обычный бинт и одна доза спирта.";
   if(drunkSeconds>0)return "Координация и восстановление сил снижены. Дай опьянению пройти.";
   return health<100?"Можно восстановить часть здоровья перевязкой или едой в обустроенном убежище.":"Перед выходом приготовь стерильный бинт. Для убежища оставь еду и обычный бинт.";
  }
  string AlcoholSummary(){
   if(alcoholDoses==0)return "Запас закончился";
   if(alcoholDoses<=3)return "Осталось "+alcoholDoses+" из 3 доз";
   return "Полных бутылок "+(alcoholDoses/3)+" · ещё доз "+(alcoholDoses%3);
  }
  void FillRect(Rect area,Color color){GUI.color=color;GUI.DrawTexture(area,Texture2D.whiteTexture);GUI.color=Color.white;}
  void MedicalButton(Rect area,string label,MedicalAction action){
   string reason=MedicalUnavailable(action);bool enabled=reason==null;
   FillRect(area,enabled?(action==MedicalAction.Drink?new Color(.36f,.25f,.19f):new Color(.22f,.35f,.30f)):new Color(.13f,.18f,.18f));
   GUI.color=enabled?Color.white:new Color(.58f,.63f,.59f);
   GUI.Label(new Rect(area.x+13,area.y+11,area.width-26,area.height-16),enabled?label:reason,small);
   GUI.color=Color.white;
   if(Event.current.type==EventType.Repaint&&pointer.TakeClick(area)&&enabled)BeginMedical(action);
  }
  void MedicalCard(Rect area,string title,string detail){
   FillRect(area,new Color(.095f,.14f,.14f));
   GUI.Label(new Rect(area.x+15,area.y+9,285,34),title,text);
   GUI.color=new Color(.66f,.71f,.65f);
   GUI.Label(new Rect(area.x+15,area.y+44,285,45),detail,small);
   GUI.color=Color.white;
  }
  void DrawMedicalProgress(){
   if(medicalAction==MedicalAction.None)return;
   float progress=Mathf.Clamp01(medicalTime/MedicalDuration(medicalAction));
   Panel(MedicalProgressArea);
   string label=medicalAction==MedicalAction.Craft?"Стерилизация":medicalAction==MedicalAction.Drink||medicalAction==MedicalAction.Water?"Питьё":medicalAction==MedicalAction.Eat?"Еда":medicalAction==MedicalAction.Rest?"Отдых":medicalAction==MedicalAction.Refill?"Набираю воду":"Перевязка";
   GUI.Label(new Rect(344,558,290,30),label+" · "+Mathf.RoundToInt(progress*100)+"%",small);
   FillRect(new Rect(344,596,280,7),new Color(.17f,.22f,.20f));
   FillRect(new Rect(344,596,280*progress,7),MedicalAccent);
   if(Button(CancelTreatment,"Отмена")){CancelMedical();Say("Действие прервано. Материалы сохранены.");}
  }
 }
}
