using System.Collections;
using System.IO;
using UnityEngine;

namespace Askich {
 public sealed partial class DistrictGame {
  static readonly Rect RadioCaptionArea=new Rect(250,305,550,106);
  readonly string[] radioLines={
   "Если Анна ещё в медпункте — скажите ей: мы ушли. Повторяю, ушли.",
   "Нина, не жди у дома. На голос идут заражённые. Закрой дверь.",
   "Северо-восточная окраина. Водонапорная башня. Возьмите еду и бинты. Мы ещё здесь."
  };
  AudioClip[] radioMessages;
  int radioPart=-1;
  float radioGap,porchScrape;
  bool radioSpeaking;
  void PrepareRadio(){
   radioMessages=new AudioClip[3];
   for(int i=0;i<radioMessages.Length;i++)radioMessages[i]=Resources.Load<AudioClip>("Survival/Radio/message"+i);
   voice.loop=false;
  }
  void ToggleRadio(){
   radioOn=!radioOn;voice.Stop();radioSpeaking=false;radioPart=-1;radioGap=.8f;radioPulse=0;
   if(radioOn){staticSound.Play();Alert(radioPos,25);Say("Приёмник ловит голоса. Можно дослушать или выключить — действие / E.");}
   else{staticSound.Stop();Say("Приёмник выключен. Голоса стихли.");}
  }
  void TickRadio(float dt){
   if(!radioOn)return;
   bool clear=Sight(radioPos,hero.p);
   voice.volume=Mathf.Lerp(voice.volume,clear?.85f:.38f,dt*4);
   voice.GetComponent<AudioLowPassFilter>().cutoffFrequency=clear?3400:1100;
   radioPulse-=dt;
   if(radioPulse<=0){radioPulse=3;Alert(radioPos,25);}
   if(radioSpeaking){
    if(!voice.isPlaying){
     radioSpeaking=false;
     if(radioPart==2){if(!heard){heard=true;Save();}radioPart=-1;radioGap=4;}
     else radioGap=1.1f;
    }
   }else{
    radioGap-=dt;
    if(radioGap<=0){
     radioPart++;voice.clip=radioMessages[radioPart];radioSpeaking=true;
     if(voice.clip)voice.Play();
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
   ?radioSpeaking?"РАДИО · "+radioLines[radioPart]:"РАДИО · треск, голоса теряются в помехах…":null;
  void DrawRadioCaption(){
   if(RadioCaption==null||suppliesOpen)return;
   Panel(RadioCaptionArea);
   GUI.Label(new Rect(265,316,520,86),RadioCaption,small);
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  IEnumerator CheckRadio(System.Action<bool,string> check,string folder){
   foreach(var z in infected)z.hp=0;
   health=100;bleeding=false;infection=0;hero.p=radioPos;heroVelocity=Vector2.zero;heard=false;
   radioOn=false;voice.Stop();staticSound.Stop();ToggleRadio();
   check(!heard&&radioMessages.Length==3&&System.Array.TrueForAll(radioMessages,c=>c),"three radio messages load without granting the destination early");
   yield return new WaitForSeconds(1.2f);
   check(radioSpeaking&&radioPart==0&&RadioCaption.Contains("Анна"),"first spoken fragment has matching Anna subtitle");
   check(PointerOnHud(RadioCaptionArea.center),"radio subtitles exclude clicks from world attacks");
   int part=radioPart;float cursor=voice.time;paused=true;
   yield return new WaitForSeconds(.3f);
   check(radioPart==part&&Mathf.Abs(voice.time-cursor)<.12f,"pause holds the radio scene");paused=false;
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
