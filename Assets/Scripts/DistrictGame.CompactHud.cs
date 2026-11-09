using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  bool showMap,showHelp;
  Home hudRoom;float roomTitleUntil;
  static readonly Rect HudInventory=new Rect(1042,22,44,40),HudJournal=new Rect(1094,22,44,40),HudMap=new Rect(1146,22,44,40),HudPause=new Rect(1200,22,48,40);
  static readonly Rect HudClock=new Rect(904,20,125,46),HudCaption=new Rect(320,625,640,65);
  Rect HudVitals=>new Rect(20,16,(5+(bleeding?1:0)+(infection>0?1:0)+(drunkSeconds>0?1:0))*43+20,60);
  bool CompactHudHit(Vector2 p)=>HudVitals.Contains(p)||HudClock.Contains(p)||HudInventory.Contains(p)||HudJournal.Contains(p)||HudMap.Contains(p)||HudPause.Contains(p)||TouchControls.Enabled&&new Rect(1080,338,170,45).Contains(p)||showMap&&new Rect(1080,120,170,170).Contains(p)||showHelp&&new Rect(25,160,450,180).Contains(p);
  void DrawCompactChrome(){
   Panel(HudVitals);DrawCompactVitals();
   if(playing&&!paused&&!dead&&!TouchControls.Enabled){
   if(!Climbing&&Button(HudInventory,"I")){suppliesOpen=!suppliesOpen;journalOpen=false;}
   if(!Climbing&&Button(HudJournal,"J")){journalOpen=true;suppliesOpen=false;}
   if(Button(HudMap,"M"))showMap=!showMap;
   }
   if(playing&&!TouchControls.Enabled&&Button(HudPause,"Ⅱ")){paused=!paused;CancelMedical();CancelSearch();Save();}
   GUI.Label(new Rect(913,26,115,30),ClockText,text);
   if(HudClock.Contains(pointer.Position))GUI.Label(new Rect(905,69,260,27),"День "+progression.Data.day,small);
   if(TouchControls.Enabled&&playing&&!paused&&!dead&&Button(new Rect(1080,338,170,45),"Персонаж")){journalOpen=true;suppliesOpen=false;}
   if(showMap)DrawMap();
   if(current!=hudRoom){hudRoom=current;roomTitleUntil=Time.unscaledTime+2.5f;}
   if(current!=null&&Time.unscaledTime<roomTitleUntil&&!(vitalTip>=0&&Time.unscaledTime<vitalTipTime)){Panel(new Rect(25,87,390,35));GUI.Label(new Rect(36,93,370,25),current.title,small);}
   if(showHelp){Panel(new Rect(25,160,450,180));GUI.Label(new Rect(38,172,425,160),"WASD — движение · Shift — бег\nCtrl — тихий шаг · F — фонарь\nЛКМ — удар / обыск · ПКМ — прицел\nПробел — толчок · E — действие\nI — вещи · J — навыки · M — карта\nF1 — скрыть помощь",small);}
  }
  void DrawCompactNotice(){
   if(RadioCaption!=null||noticeTime<=0||medicalAction!=MedicalAction.None)return;
   bool longNote=notice.StartsWith("ЗАПИСКА");var rect=longNote?new Rect(310,560,660,125):new Rect(350,647,580,46);Panel(rect);GUI.Label(new Rect(rect.x+12,rect.y+8,rect.width-24,rect.height-12),notice,small);
  }
 }
}
