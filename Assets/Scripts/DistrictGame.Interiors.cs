using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Askich {
 public sealed partial class DistrictGame {
  DistrictContainerAudio containerAudio;
  void BuildContainers(Home h,int id){
   if(id==5){
    h.title="Дом связиста";
    h.containers=new[]{new Vector2(2,-1.9f),new Vector2(2,2.4f),new Vector2(0,2.4f)};
    h.labels=new[]{"Ящик с инструментами","Кухонный ящик","Дорожная аптечка"};
    h.seconds=new[]{3.4f,2.2f,1.3f};h.noise=new[]{13f,6f,3f};
    h.note="ЗАПИСКА НИНЫ: «Саша велел оставить приёмник на этой частоте. Ключи сняли с крючков, сумку бросили. Взяли только документы. Топор заедает в железном ящике у кровати — не греми. Анна через улицу ещё перевязывала людей. Потом все шли к башне»";
   }else if(id==6){
    h.title="Временный медпункт";h.labels=new[]{"Перевязочный ящик","Медицинская сумка","Опустевший шкаф"};
    h.seconds=new[]{2.1f,1.3f,2.8f};h.noise=new[]{5f,3f,9f};
    h.note="ЗАПИСКА АННЫ: «Остались только бинты. Антибиотики увезли вчера. Нина и Саша ушли к башне, я задержалась с последним пациентом. Если придёшь позже — не ищи нас здесь. Не включай соседское радио надолго»";
   }
   ConfigureStorage(h,id);for(int i=0;i<3;i++)BuildStorageModel(h,id,i);
   RefreshContainers(h);
  }
  void RefreshContainers(Home h){for(int i=0;i<3;i++)if(h.lids[i])PoseStorage(h,i,0,true);}
  void BeginSearch(Home h,int slot,bool preserveFacing=false){
   if(searchSlot==slot&&searchHome==h)return;
   searchHome=h;searchSlot=slot;searchTime=0;searchNoiseClock=0;heroVelocity=Vector2.zero;
   Vector2 direction=h.p+h.containers[slot]-hero.p;
   if(direction.sqrMagnitude>.001f){face=direction.normalized;if(!preserveFacing)hero.root.rotation=Quaternion.LookRotation(V(face));}
   if(containerAudio)containerAudio.StartStorage(V(h.p+h.containers[slot],1),h.noise[slot]);SearchBeat();
  }
  int NearbyContainer(Home h){
   int found=-1;float nearest=1.6f;
   for(int i=0;i<3;i++){
    Vector2 position=h.p+h.containers[i];float distance=Vector2.Distance(hero.p,position);
    if(distance>=nearest||!Sight(hero.p,position))continue;
    nearest=distance;found=i;
   }
   return found;
  }
  void DrawInteriorHint(){
   if(current==null)return;

   int i=FocusedStorageSlot();if(i>=0){
    if(searchSlot<0){GUI.Label(new Rect(35,130,520,30),"E · "+current.labels[i],small);if(!current.taken[i])DrawSearchRisk(new Rect(35,160,42,18),current.noise[i],false);}return;
   }
   if(current.note!=null&&Vector2.Distance(hero.p,current.p+new Vector2(-2,0))<1.7f)
    GUI.Label(new Rect(35,130,520,30),"E · записка",small);
  }
  void DressInterior(Home h,int id){
   if(id!=5&&id!=6)return;
   GameObject Part(string name,float x,float y,float z,float sx,float sy,float sz,Color color)=>Cube(name,V(h.p+new Vector2(x,z),y),new Vector3(sx,sy,sz),color);
   var wood=new Color(.32f,.25f,.18f);var linen=new Color(.55f,.55f,.43f);var metal=new Color(.31f,.38f,.38f);
   for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part("Writing table leg",-2+x*.65f,.4f,z*.48f,.09f,.6f,.09f,wood);
   // All additions are authored geometry; no new asset or voice licenses are required.
   if(id==5){
    // An open bag, empty hooks and a scrape lead from the threshold to the tool chest.
    Part("Open travel bag",-1.45f,.36f,-3.1f,.72f,.35f,.48f,new Color(.28f,.22f,.16f));
    Part("Bag dark opening",-1.45f,.55f,-3.1f,.59f,.02f,.35f,new Color(.08f,.09f,.07f));
    for(int i=0;i<2;i++)Part("Bag folded clothes",-1.6f+i*.28f,.55f,-3.1f,.2f,.08f,.25f,linen);
    Part("Empty key rack",2.3f,1.55f,-3.79f,.8f,.15f,.06f,wood);
    for(int i=0;i<3;i++)Part("Bare key hook",2.05f+i*.24f,1.52f,-3.7f,.04f,.08f,.12f,metal);
    for(int i=0;i<2;i++)Part("Toolbox drag mark",1.74f+i*.4f,.193f,-2.8f,.035f,.009f,1.55f,new Color(.58f,.49f,.32f));
    Part("Box abandoned lid",.95f,.23f,-2.4f,.5f,.07f,.4f,wood);
    DistrictAtmosphere.Point("Tool chest window light",V(h.p+new Vector2(2,-2),1.8f),new Color(.82f,.76f,.59f),.42f,3);
    for(int i=0;i<20;i++)Part("Uneven floorboards",-3.8f+i*.4f,.17f,0,.38f,.02f,7.8f,Color.Lerp(wood,new Color(.42f,.35f,.26f),(i%4)*.18f));
    Part("Faded rug",0,.19f,-.5f,2.3f,.025f,3.3f,new Color(.28f,.31f,.28f));
    for(int i=-1;i<=1;i+=2)Part("Rug border",i*1.03f,.21f,-.5f,.09f,.01f,3.1f,linen);
    Part("Abandoned mattress",-2,.34f,-2.4f,1.5f,.3f,2.2f,new Color(.39f,.42f,.37f));
    Part("Folded blanket",-2,.51f,-2.7f,1.45f,.09f,1.25f,new Color(.3f,.28f,.24f));
    Part("Pillow",-2,.55f,-1.65f,.9f,.12f,.4f,linen);
    Part("Radio desk",-2,1,3,1.7f,.14f,1.1f,wood);
    for(int x=-1;x<=1;x+=2)Part("Desk leg",-2+x*.68f,.55f,3,.1f,1,.1f,wood);
    Part("Kitchen hob",3.25f,.7f,1,1,.95f,1.2f,new Color(.39f,.43f,.39f));
    for(int i=0;i<2;i++)Part("Cold stove plate",3.25f,1.19f,.7f+i*.6f,.62f,.03f,.4f,new Color(.12f,.14f,.14f));
    Part("Packing case",-3,.38f,-.8f,.65f,.4f,.45f,new Color(.26f,.23f,.2f));
    for(int i=0;i<5;i++)Part("Scattered letters",-.4f+i*.24f,.23f,1+(i%2)*.3f,.23f,.015f,.3f,linen);
    Part("Family photo frame",.9f,1.5f,3.8f,.85f,.6f,.05f,wood);
    Part("Faded family photo",.9f,1.5f,3.76f,.7f,.45f,.025f,new Color(.52f,.48f,.36f));
    for(int i=0;i<3;i++)Part("Photo silhouette",.65f+i*.24f,1.48f,3.74f,.12f,.26f,.015f,new Color(.26f,.29f,.26f));
   }else{
    for(int x=0;x<8;x++)for(int z=0;z<8;z++)Part("Worn clinic tile",-3.5f+x,.17f,-3.5f+z,.97f,.025f,.97f,(x+z)%2==0?new Color(.4f,.46f,.42f):new Color(.31f,.37f,.34f));
    Part("Examination couch frame",2,.6f,-1.4f,1.4f,.14f,2.8f,metal);
    Part("Examination couch mattress",2,.76f,-1.4f,1.3f,.18f,2.6f,new Color(.45f,.51f,.46f));
    Part("Couch pillow",2,.91f,-.45f,.9f,.1f,.42f,linen);
    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part("Couch leg",2+x*.55f,.34f,-1.4f+z*1.1f,.09f,.55f,.09f,metal);
    Part("Folded medical screen",3.35f,1.2f,-1.5f,.08f,2.1f,2.3f,new Color(.47f,.54f,.49f));
    for(int z=-1;z<=1;z+=2)Part("Screen stand",3.35f,1.1f,-1.5f+z*1.2f,.06f,2.2f,.06f,metal);
    Part("Wash basin stand",-3.1f,.62f,-2.6f,.85f,.95f,.7f,metal);
    Part("Empty basin",-3.1f,1.11f,-2.6f,.7f,.06f,.55f,new Color(.65f,.67f,.6f));
    for(int i=0;i<4;i++)Part("Empty medicine cartons",-2.5f+i*.28f,.83f,.18f,.18f,.12f,.23f,linen);
    Part("Patient ledger",-1.6f,.81f,-.3f,.48f,.04f,.36f,new Color(.34f,.29f,.22f));
    DistrictAtmosphere.Point("Clinic emergency lamp",V(h.p+new Vector2(1,0),2.5f),new Color(.69f,.81f,.73f),.6f,5);
   }
   // Chipped plaster and skirting retain room silhouettes at normal camera distance.
   for(int i=0;i<7;i++)Part("Plaster damage",-3+i,.5f+(i%3)*.15f,3.82f,.45f+(i%2)*.2f,.22f,.015f,new Color(.35f,.36f,.3f));
   Part("North skirting",0,.3f,3.8f,7.6f,.18f,.08f,wood);
   Part("East skirting",3.8f,.3f,0,.08f,.18f,7.6f,wood);
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  IEnumerator CheckInteriors(Action<bool,string> check,string folder){
   var home=homes[5];var clinic=homes[6];
   check(home.title!=clinic.title&&home.labels[0]!=clinic.labels[0],"radio home and clinic have distinct container identities");
   check(home.noise[0]>clinic.noise[1]&&home.seconds[0]>clinic.seconds[1],"toolbox trades longer exposure and noise for weapon");
   foreach(var h in new[]{home,clinic}){h.open=true;SnapDoor(h);for(int i=0;i<3;i++)check(ContainerReachable(h,i),"container approach accessible: "+h.labels[i]);}
   foreach(var e in infected){e.rest=100;e.alert=0;}
   var witness=infected[0];witness.hp=witness.maxHp;witness.p=home.p+new Vector2(2,8);witness.path.Clear();
   home.taken[0]=false;hero.p=home.p+home.containers[0]+Vector2.up;current=home;Use();
   check(searchSlot==0&&witness.alert>0,"using toolbox starts search and alerts distant infected");
   searchSlot=-1;check(!home.taken[0],"interrupted search does not grant loot");
   hero.p=clinic.p+clinic.containers[1]+Vector2.down;current=clinic;clinic.taken[1]=false;witness.p=hero.p+Vector2.down*8;witness.alert=0;Use();
   check(searchSlot==1&&witness.alert==0,"quiet medical bag leaves distant infected unaware");
   // Exercise the real timed Update path, then save/load its visible result.
   foreach(var e in infected){e.hp=0;if(e.visual)e.visual.Die();}
   int previousBandages=bandages;yield return new WaitForSeconds(1.65f);
   check(clinic.taken[1]&&bandages==previousBandages+1,"timed search completes through player Update");
   Save();clinic.taken[1]=false;RefreshContainers(clinic);check(Load()&&clinic.taken[1]&&clinic.lids[1].TransformPoint(new Vector3(0,0,-.26f)).y>clinic.lids[1].position.y+.2f,"searched container appearance restored from existing save data");
   hero.p=clinic.p+new Vector2(-1,1);zoom=8;yield return new WaitForSeconds(1);Use();yield return new WaitForEndOfFrame();
   var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"clinic.png"),image.EncodeToPNG());Destroy(image);
   hero.p=home.p+new Vector2(-1,1);yield return new WaitForSeconds(1);Use();yield return new WaitForEndOfFrame();
   image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"radio-home.png"),image.EncodeToPNG());Destroy(image);
  }
#endif
 }

}
