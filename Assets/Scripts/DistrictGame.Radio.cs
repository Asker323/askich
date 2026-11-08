using System.Collections;
using System.IO;
using UnityEngine;

namespace Askich {
 public sealed partial class DistrictGame {
  static Rect RadioCaptionArea=>HudCaption;
  readonly string[] radioLines={
   "Если Анна ещё в медпункте — скажите ей: мы ушли. Повторяю, ушли.",
   "Нина, не жди у дома. На голос идут заражённые. Закрой дверь.",
   "Северо-восточная окраина. Водонапорная башня. Возьмите еду и бинты. Мы ещё здесь."
  };
  readonly float[] radioDurations={6,6,8};
  float radioRemaining;
  int radioPart=-1;
  float radioGap,porchScrape;
  bool radioSpeaking;
  void PrepareRadio(){
   // No synthetic speech is loaded. The damaged receiver carries timed text and static.
   voice.clip=null;voice.loop=false;
  }
  void ToggleRadio(){
   trainingRadio=false;radioOn=!radioOn;voice.Stop();radioSpeaking=false;radioPart=-1;radioRemaining=0;radioGap=.8f;radioPulse=0;
   if(radioOn){staticSound.Play();Alert(radioPos,25);Say("Слабый сигнал. E — выключить.");}
   else{staticSound.Stop();Say("Приёмник выключен.");}
  }
  void TickRadio(float dt){
   if(!radioOn)return;
   bool clear=Sight(radioPos,hero.p);
   staticSound.volume=Mathf.Lerp(staticSound.volume,clear?.075f:.035f,dt*4);
   voice.GetComponent<AudioLowPassFilter>().cutoffFrequency=clear?3400:1100;
   radioPulse-=dt;
   if(radioPulse<=0){radioPulse=3;Alert(radioPos,25);}
   if(trainingRadio){TickTraining(dt);return;}
   if(radioSpeaking){
    radioRemaining-=dt;
    if(radioRemaining<=0){
     radioSpeaking=false;
     if(radioPart==2){if(!heard){heard=true;Save();}radioPart=-1;radioGap=4;}
     else radioGap=1.1f;
    }
   }else{
    radioGap-=dt;
    if(radioGap<=0){
     radioPart++;radioRemaining=radioDurations[radioPart];radioSpeaking=true;
    }
   }
   porchScrape-=dt;
   var h=homes[5];var threshold=h.p+Vector2.down*4;
   if(!h.open&&porchScrape<=0&&infected.Exists(z=>z.hp>0&&Vector2.Distance(z.p,threshold)<2.7f)){
    foley.Scrape(V(threshold,.4f));porchScrape=8;
    if(current==h)Say("Снаружи скребут по крыльцу. Дверь пока держится.");
   }
  }
  string RadioCaption=>radioOn&&Vector2.Distance(hero.p,radioPos)<12
   ?trainingRadio?"УЧЕБНАЯ ЧАСТОТА · "+(progression.Data.lessonCompletedDay==progression.Data.day?"Сегодняшняя передача усвоена. Новая программа завтра.":TrainingLine+" · "+Mathf.FloorToInt(progression.Data.lessonSeconds)+" / 90 с"):radioSpeaking?"РАДИО · "+radioLines[radioPart]:null:null;
  void DrawRadioCaption(){
   if(RadioCaption==null||suppliesOpen)return;
   Panel(HudCaption);
   GUI.Label(new Rect(333,635,614,50),RadioCaption,small);
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  IEnumerator CheckRadio(System.Action<bool,string> check,string folder){
   foreach(var z in infected)z.hp=0;
   health=100;bleeding=false;infection=0;hero.p=radioPos;heroVelocity=Vector2.zero;heard=false;
   radioOn=false;voice.Stop();staticSound.Stop();ToggleRadio();
   check(!heard&&radioDurations.Length==3&&voice.clip==null&&staticSound.isPlaying,"text radio starts with static and no synthetic voice or early destination");
   yield return new WaitForSeconds(1.2f);
   check(radioSpeaking&&radioPart==0&&RadioCaption.Contains("Анна"),"first timed fragment shows Anna subtitle without speech");
   check(PointerOnHud(RadioCaptionArea.center),"radio subtitles exclude clicks from world attacks");
   int part=radioPart;float cursor=radioRemaining;paused=true;
   yield return new WaitForSeconds(.3f);
   check(radioPart==part&&Mathf.Abs(radioRemaining-cursor)<.12f,"pause holds the radio scene");paused=false;
   yield return CaptureGameCamera(Path.Combine(folder,"radio-story-house.png"));
   ToggleRadio();yield return new WaitForSeconds(.3f);
   check(!voice.isPlaying&&!staticSound.isPlaying&&!heard,"early switch-off silences both sources and keeps clue unknown");
   ToggleRadio();float timeout=Time.time+65;
   while(!heard&&Time.time<timeout)yield return null;
   check(heard&&!radioSpeaking,"finishing final fragment reveals shelter");
   Save();ToggleRadio();heard=false;
   check(Load()&&heard&&!radioOn,"completed clue survives save and load with radio switched off");
  }
#endif
 }
}
