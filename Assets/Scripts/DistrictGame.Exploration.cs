using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  const int GardenOpen=1,GardenTaken=2,AlarmSpent=4,GardenSeen=8,CarSeen=16,StopSeen=32;
  readonly Vector2 gardenGate=new Vector2(-46,-17),gardenCache=new Vector2(-47,-10),alarmSwitch=new Vector2(24.25f,-9),busNotice=new Vector2(-6,-20.8f);
  int explorationFlags;Barrier gardenBarrier;Transform gardenLeaf,cacheLid;float gardenAngle,cacheSearch,alarmRemaining,alarmPulse;bool cacheSearching;
  AudioSource carAlarm;AudioClip carAlarmClip;Renderer[] alarmLamps;GameObject busCanopy,busRear;
  bool NearExplore(Vector2 p,float reach=1.8f)=>Vector2.Distance(hero.p,p)<reach&&Sight(hero.p,p);
  void BuildExploration(){
   Color timber=new Color(.28f,.25f,.18f),metal=new Color(.25f,.32f,.28f),soil=new Color(.23f,.20f,.14f);
   // The garden occupies a side yard, away from the existing street and home doors.
   Cube("Garden worn earth",new Vector3(-46,.04f,-12),new Vector3(9,.025f,10),soil);
   void FencePart(Vector2 p,Vector2 size){barriers.Add(new Barrier{rect=new Rect(p-size/2,size)});bool across=size.x>size.y;for(int rail=0;rail<2;rail++)Cube("Garden fence rail",V(p,.3f+rail*.5f),new Vector3(across?size.x:.1f,.10f,across?.1f:size.y),timber);int count=Mathf.CeilToInt(Mathf.Max(size.x,size.y)/.45f);for(int n=0;n<=count;n++){Vector2 at=p+new Vector2(size.x>.3f?Mathf.Lerp(-size.x/2,size.x/2,(float)n/count):0,size.y>.3f?Mathf.Lerp(-size.y/2,size.y/2,(float)n/count):0);Cube("Garden fence post",V(at,.65f),new Vector3(across?.22f:.10f,1.3f,across?.10f:.22f),timber*.8f);}}
   FencePart(new Vector2(-50.5f,-12),new Vector2(.13f,10));FencePart(new Vector2(-41.5f,-12),new Vector2(.13f,10));FencePart(new Vector2(-46,-7),new Vector2(9,.13f));
   FencePart(new Vector2(-49,-17),new Vector2(3,.13f));FencePart(new Vector2(-43,-17),new Vector2(3,.13f));
   gardenBarrier=new Barrier{rect=new Rect(-47.5f,-17.1f,3,.2f)};barriers.Add(gardenBarrier);
   gardenLeaf=new GameObject("Garden hinged door").transform;gardenLeaf.position=V(gardenGate+Vector2.left*1.5f);
   for(int n=0;n<7;n++)Cube("Garden door slat",new Vector3(.18f+n*.43f,.55f,0),new Vector3(.27f,1.1f,.10f),timber,gardenLeaf);
   for(int n=0;n<2;n++)Cube("Garden door rail",new Vector3(1.5f,.25f+n*.55f,0),new Vector3(3,.1f,.13f),timber*.85f,gardenLeaf);
   for(int row=0;row<3;row++){
    float x=-49+row*3;Cube("Raised garden bed",new Vector3(x,.13f,-12),new Vector3(1.1f,.15f,4.4f),timber);Cube("Dry garden soil",new Vector3(x,.215f,-12),new Vector3(.95f,.02f,4.2f),soil*.7f);
    for(int n=0;n<7;n++){var stalk=Cube("Wilted vegetable stalk",new Vector3(x+(n%2)*.22f,.38f,-13.7f+n*.55f),new Vector3(.035f,.34f,.035f),new Color(.32f,.32f,.16f));stalk.transform.rotation=Quaternion.Euler(12,n*47,15);Cube("Wilted leaves",new Vector3(x,.41f,-13.7f+n*.55f),new Vector3(.36f,.035f,.15f),new Color(.24f,.28f,.13f)).transform.rotation=Quaternion.Euler(0,n*47+row*15,n%2*15);}
   }
   var weeds=new System.Random(918);for(int n=0;n<45;n++){float x=-50+(float)weeds.NextDouble()*8,z=-16+(float)weeds.NextDouble()*8;if(Mathf.Abs(x+47)<.65f)continue;var stem=Cube("Dry garden weed",new Vector3(x,.18f,z),new Vector3(.035f,.25f+(float)weeds.NextDouble()*.2f,.035f),new Color(.30f,.30f,.16f));stem.transform.rotation=Quaternion.Euler(12,n*53,18);Cube("Dry weed leaves",new Vector3(x,.24f,z),new Vector3(.25f,.035f,.1f),new Color(.27f,.29f,.16f)).transform.rotation=Quaternion.Euler(0,n*53,20);}
   // A narrow path between the beds leads to the equipment cache.
   Cube("Garden trampled path",new Vector3(-47,.06f,-13),new Vector3(.8f,.02f,5.7f),new Color(.36f,.31f,.21f));
   Cube("Garden equipment crate",V(gardenCache,.36f),new Vector3(1,.7f,.7f),metal);
   cacheLid=new GameObject("Garden crate lid hinge").transform;cacheLid.position=V(gardenCache+Vector2.up*.35f,.73f);Cube("Garden crate lid",new Vector3(0,0,-.35f),new Vector3(1.05f,.07f,.75f),metal*.8f,cacheLid);
   Cube("Forgotten watering can",new Vector3(-43.2f,.3f,-8.5f),new Vector3(.4f,.5f,.35f),metal);Cube("Watering can spout",new Vector3(-43.5f,.25f,-8.5f),new Vector3(.4f,.1f,.12f),metal);
   for(int i=0;i<4;i++)Cube("Unfinished garden repairs",new Vector3(-43.4f+i*.17f,.12f,-15.7f),new Vector3(.12f,.1f,1.7f),timber);
   // Reuse the car already parked here; the switch is beside it, on clear ground.
   alarmLamps=new Renderer[2];for(int side=0;side<2;side++)alarmLamps[side]=Cube("Alarm indicator",new Vector3(25.5f+side, .85f,-11.4f),new Vector3(.22f,.12f,.06f),new Color(.6f,.25f,.06f)).GetComponent<Renderer>();
   var emitter=new GameObject("Abandoned car alarm");emitter.transform.position=V(alarmSwitch,1);carAlarm=emitter.AddComponent<AudioSource>();carAlarm.playOnAwake=false;carAlarm.spatialBlend=1;carAlarm.minDistance=3;carAlarm.maxDistance=32;carAlarm.rolloffMode=AudioRolloffMode.Linear;carAlarm.dopplerLevel=0;carAlarm.volume=.26f;carAlarm.loop=true;
   const int rate=22050;var samples=new float[rate];for(int i=0;i<samples.Length;i++){float t=(float)i/rate;float envelope=Mathf.Min(1,(t%.25f)*150)*Mathf.Min(1,(.25f-t%.25f)*150);samples[i]=Mathf.Sin(t*Mathf.PI*2*(t<.5f?740:620))*.25f*envelope;}carAlarmClip=AudioClip.Create("Authored car alarm",rate,1,rate,false);carAlarmClip.SetData(samples,0);carAlarm.clip=carAlarmClip;
   // A small roadside shelter and abandoned luggage tell the evacuation story.
   Cube("Bus stop concrete pad",new Vector3(-7,.05f,-19.4f),new Vector3(6,.1f,2.7f),new Color(.36f,.37f,.32f));
   for(int side=-1;side<=1;side+=2)Cube("Bus shelter post",new Vector3(-7+side*2.6f,1.3f,-18.2f),new Vector3(.1f,2.6f,.1f),metal);
   busRear=Cube("Bus shelter rear panel",new Vector3(-7,1.15f,-18.2f),new Vector3(5.4f,2.3f,.1f),metal*.7f);barriers.Add(new Barrier{rect=new Rect(-9.7f,-18.25f,5.4f,.1f)});busCanopy=Cube("Bus shelter roof",new Vector3(-7,2.6f,-19.4f),new Vector3(5.8f,.12f,2.6f),metal);
   Cube("Bus stop bench",new Vector3(-7,.48f,-18.9f),new Vector3(3.2f,.16f,.55f),timber);for(int side=-1;side<=1;side+=2)Cube("Bus bench leg",new Vector3(-7+side*1.2f,.23f,-18.9f),new Vector3(.12f,.46f,.4f),metal);
   Cube("Evacuation timetable board",V(busNotice+Vector2.up*.2f,1.4f),new Vector3(.8f,1.2f,.1f),metal);Cube("Torn evacuation notice",V(busNotice+Vector2.up*.13f,1.45f),new Vector3(.65f,.8f,.02f),new Color(.66f,.63f,.49f));
   for(int n=0;n<3;n++){float x=-8.7f+n*.8f;Cube("Abandoned passenger luggage",new Vector3(x,.24f,-19.7f),new Vector3(.55f,.45f,.4f),HouseColours[n]);for(int side=-1;side<=1;side+=2)Cube("Suitcase handle side",new Vector3(x+side*.09f,.51f,-19.7f),new Vector3(.035f,.12f,.05f),timber);Cube("Suitcase handle grip",new Vector3(x,.57f,-19.7f),new Vector3(.21f,.035f,.05f),timber);Cube("Suitcase fastening strap",new Vector3(x,.24f,-19.905f),new Vector3(.065f,.45f,.015f),timber);}

   RefreshExploration(true);
  }
  void RefreshExploration(bool snap){if(snap){gardenAngle=(explorationFlags&GardenOpen)!=0?100:0;cacheSearching=false;cacheSearch=0;alarmRemaining=0;if(carAlarm)carAlarm.Stop();}if(gardenLeaf)gardenLeaf.rotation=Quaternion.Euler(0,-gardenAngle,0);if(gardenBarrier!=null)gardenBarrier.active=gardenAngle<70;if(cacheLid)cacheLid.localRotation=Quaternion.Euler((explorationFlags&GardenTaken)!=0?-105:cacheSearching?-35:0,0,0);}
  bool GardenDoorOccupied(){var r=gardenBarrier.rect;r.xMin-=.35f;r.xMax+=.35f;r.yMin-=.4f;r.yMax+=.4f;return r.Contains(hero.p)||infected.Exists(z=>z.hp>0&&r.Contains(z.p));}
  void CancelCacheSearch(){cacheSearching=false;cacheSearch=0;containerAudio.SetSearching(searchSlot>=0);}
  bool UseExploration(){
   if(cacheSearching){CancelCacheSearch();return true;}
   if(NearExplore(gardenGate,2)){if((explorationFlags&GardenOpen)!=0&&GardenDoorOccupied()){Say("Проём занят");return true;}explorationFlags^=GardenOpen;Alert(gardenGate,6);foley.Door(V(gardenGate,1),(explorationFlags&GardenOpen)!=0);Save();return true;}
   if(NearExplore(gardenCache,1.7f)){if((explorationFlags&GardenTaken)==0){cacheSearching=true;cacheSearch=0;cacheStartHealth=health;containerAudio.Play(V(gardenCache,1),5,false);}else Say("Ящик пуст");return true;}
   if(NearExplore(alarmSwitch,1.5f)){if(alarmRemaining>0){alarmRemaining=0;carAlarm.Stop();}else if((explorationFlags&AlarmSpent)==0){explorationFlags|=AlarmSpent;alarmRemaining=18;alarmPulse=0;carAlarm.Play();Alert(alarmSwitch,28);Save();}else Say("Аккумулятор разряжен");return true;}
   if(NearExplore(busNotice,1.7f)){Say("ЗАПИСКА НА ОСТАНОВКЕ: «Последний автобус ушёл пустым. Люди пошли пешком к водонапорной башне. Коля оставил инструменты в огороде за крайним западным домом. Жёлтую машину не трогайте — сигнализация ещё работает»");return true;}
   return false;
  }
  void TickExploration(float dt){
   bool atStop=new Rect(-11,-23,8,7).Contains(hero.p);busCanopy.SetActive(!atStop);busRear.SetActive(!atStop);
   if(Vector2.Distance(hero.p,gardenGate)<14)explorationFlags|=GardenSeen;if(Vector2.Distance(hero.p,alarmSwitch)<14)explorationFlags|=CarSeen;if(Vector2.Distance(hero.p,busNotice)<12)explorationFlags|=StopSeen;
   bool open=(explorationFlags&GardenOpen)!=0;float target=open?100:GardenDoorOccupied()?100:0;gardenAngle=Mathf.MoveTowards(gardenAngle,target,dt*160);gardenLeaf.rotation=Quaternion.Euler(0,-gardenAngle,0);gardenBarrier.active=gardenAngle<70;
   if(cacheSearching){if(!NearExplore(gardenCache,1.7f)||heroVelocity.sqrMagnitude>.01f||attack>0||medicalAction!=MedicalAction.None||suppliesOpen||journalOpen||health<cacheStartHealth){CancelCacheSearch();}else{cacheSearch+=dt;Alert(gardenCache,5);if(cacheSearch>=3){CancelCacheSearch();if(TryPack(new[]{Ingredient(DistrictItemKind.Wood,2),Ingredient(DistrictItemKind.Wire,1),Ingredient(DistrictItemKind.Cloth,2)},true)){explorationFlags|=GardenTaken;Save();sound.Play("step");}else Say("Освободи место. Материалы остались в ящике.");}}}
   if(!cacheSearching)cacheStartHealth=health;
   cacheLid.localRotation=Quaternion.RotateTowards(cacheLid.localRotation,Quaternion.Euler((explorationFlags&GardenTaken)!=0?-105:cacheSearching?-35:0,0,0),dt*180);
   if(alarmRemaining>0){alarmRemaining=Mathf.Max(0,alarmRemaining-dt);alarmPulse-=dt;if(alarmPulse<=0){alarmPulse=1;Alert(alarmSwitch,28);}if(alarmRemaining<=0)carAlarm.Stop();}
   foreach(var lamp in alarmLamps)lamp.enabled=alarmRemaining>0&&Mathf.Repeat(alarmRemaining,.65f)<.3f;
  }
  float cacheStartHealth;
  void DrawExploration(){
   string hint=cacheSearching?"Ящик материалов · "+Mathf.RoundToInt(cacheSearch/3*100)+"% · E отмена":NearExplore(gardenGate,2)?"E · "+((explorationFlags&GardenOpen)!=0?"закрыть калитку":"открыть калитку"):NearExplore(gardenCache,1.7f)?((explorationFlags&GardenTaken)!=0?"Ящик пуст":"E · ящик материалов · негромкий шум"):NearExplore(alarmSwitch,1.5f)?"E · "+(alarmRemaining>0?"выключить сигнализацию":(explorationFlags&AlarmSpent)!=0?"аккумулятор разряжен":"сигнализация · один запуск · громко"):NearExplore(busNotice,1.7f)?"E · объявление об эвакуации":null;
   if(hint==null)return;var rect=new Rect(340,495,600,38);Panel(rect);GUI.Label(new Rect(rect.x+12,rect.y+7,rect.width-24,28),hint,small);
  }
  void ReleaseExploration(){if(carAlarmClip)Destroy(carAlarmClip);}
 }
}
