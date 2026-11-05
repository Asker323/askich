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
   if((action==MedicalAction.Bandage||action==MedicalAction.Craft)&&bandages==0)return "Нужен обычный бинт";
   if(action==MedicalAction.Sterile&&sterileBandages==0)return "Нет стерильных бинтов";
   if((action==MedicalAction.Craft||action==MedicalAction.Drink)&&alcoholDoses==0)return "Нужен спирт";
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
  void DrawSuppliesMenu(){
   if(!suppliesOpen)return;
   FillRect(new Rect(0,0,1280,720),new Color(.01f,.02f,.025f,.68f));
   FillRect(SuppliesArea,new Color(.045f,.073f,.075f));
   GUI.Label(new Rect(190,148,600,45),"РЮКЗАК",large);
   if(Button(SuppliesClose,"Закрыть · I"))suppliesOpen=false;
   GUI.color=MedicalWarning;
   GUI.Label(new Rect(190,207,880,30),"Мир продолжает жить · сначала закрой вход",small);
   GUI.color=Color.white;

   GUI.Label(new Rect(190,248,295,35),"Здоровье · "+Mathf.CeilToInt(health)+" / 100",text);
   FillRect(new Rect(190,289,290,9),new Color(.15f,.20f,.19f));
   FillRect(new Rect(190,289,290*Mathf.Clamp01(health/100),9),health<35?MedicalWarning:MedicalAccent);
   GUI.color=bleeding?MedicalWarning:MedicalAccent;
   GUI.Label(new Rect(190,316,300,30),bleeding?"Кровотечение · идёт":"Кровотечение · остановлено",small);
   GUI.color=infection>0?MedicalWarning:MedicalAccent;
   GUI.Label(new Rect(190,352,300,30),"Заражение раны · "+Mathf.CeilToInt(infection)+"%",small);
   GUI.color=drunkSeconds>0?MedicalWarning:new Color(.66f,.71f,.65f);
   GUI.Label(new Rect(190,388,300,30),drunkSeconds>0?"Опьянение · "+Mathf.CeilToInt(drunkSeconds)+" с":"Координация · в порядке",small);
   GUI.color=Color.white;
   GUI.Label(new Rect(190,441,295,125),TreatmentAdvice(),small);
   GUI.color=new Color(.66f,.71f,.65f);
   GUI.Label(new Rect(190,575,300,32),"Еда "+food+" · Камни "+stones,small);
   GUI.color=Color.white;

   MedicalCard(new Rect(530,243,570,95),"Обычные бинты · "+bandages,"Остановить кровь · 2,5 с");
   MedicalButton(PlainAction,"Перевязать · B",MedicalAction.Bandage);
   MedicalCard(new Rect(530,345,570,95),"Стерильные бинты · "+sterileBandages,"Кровь и обработка раны · 2,5 с");
   MedicalButton(SterileAction,"Обработать рану · H",MedicalAction.Sterile);
   MedicalCard(new Rect(530,447,570,158),"Этиловый спирт · "+alcoholDoses+" доз",AlcoholSummary());
   int remaining=alcoholDoses==0?0:(alcoholDoses-1)%3+1;
   for(int i=0;i<3;i++)FillRect(new Rect(545+i*31,522,24,7),i<remaining?MedicalAccent:new Color(.2f,.25f,.23f));
   GUI.Label(new Rect(545,545,285,48),"Рецепт · бинт + 1 доза спирта",small);
   MedicalButton(CraftAction,"Стерилизовать · 3 с",MedicalAction.Craft);
   MedicalButton(DrinkAction,"Выпить · опьянение",MedicalAction.Drink);
  }
  void DrawMedicalProgress(){
   if(medicalAction==MedicalAction.None)return;
   float progress=Mathf.Clamp01(medicalTime/MedicalDuration(medicalAction));
   Panel(MedicalProgressArea);
   string label=medicalAction==MedicalAction.Craft?"Стерилизация":medicalAction==MedicalAction.Drink?"Питьё":"Перевязка";
   GUI.Label(new Rect(344,558,290,30),label+" · "+Mathf.RoundToInt(progress*100)+"%",small);
   FillRect(new Rect(344,596,280,7),new Color(.17f,.22f,.20f));
   FillRect(new Rect(344,596,280*progress,7),MedicalAccent);
   if(Button(CancelTreatment,"Отмена")){CancelMedical();Say("Действие прервано. Материалы сохранены.");}
  }
 }
}
