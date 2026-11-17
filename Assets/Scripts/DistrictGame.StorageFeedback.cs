using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  static readonly Rect SearchProgressArea=new Rect(475,540,330,46);
  static readonly Rect SearchCancelArea=new Rect(759,540,46,46);
  void PoseStorage(Home h,int i,float dt,bool snap){
   bool working=searchSlot==i&&searchHome==h;
   float reach=working?Mathf.SmoothStep(0,1,Mathf.Clamp01(searchTime/.35f)):0;
   float rattle=working?Mathf.Sin(Mathf.Clamp01(searchNoiseClock/.18f)*Mathf.PI)*(h.noise[i]>=9?2.5f:.6f):0;
   if(h.drawers[i]){
    h.lids[i].localRotation=Quaternion.identity;
    float slide=h.taken[i]?.43f:working?.38f*reach+rattle*.004f:0;
    Vector3 target=h.storageRest[i]+Vector3.back*slide;
    h.lids[i].position=snap?target:Vector3.MoveTowards(h.lids[i].position,target,dt*1.8f);
   }else{
    float angle=h.taken[i]?105:working?55*reach+rattle:0;
    var target=Quaternion.Euler(angle,0,0);
    h.lids[i].localRotation=snap?target:Quaternion.RotateTowards(h.lids[i].localRotation,target,dt*240);
   }
  }
  void DrawSearchRisk(Rect area,float noise,bool pulse){
   int count=noise>=9?3:noise<=3?1:2;
   var old=GUI.color;
   Color ink=count==3?new Color(.83f,.58f,.3f):new Color(.63f,.74f,.68f);
   for(int i=0;i<3;i++){
    float height=6+i*5;
    GUI.color=i<count?ink:new Color(.28f,.32f,.3f);
    if(pulse&&i<count)GUI.color=Color.Lerp(GUI.color,Color.white,Mathf.Max(0,1-searchNoiseClock/.28f)*.4f);
    GUI.DrawTexture(new Rect(area.x+i*12,area.y+area.height-height,7,height),Texture2D.whiteTexture);
   }
   GUI.color=old;
   if(area.Contains(pointer.Position))GUI.Label(new Rect(area.x,area.y+21,200,26),count==3?"Громкий обыск":count==1?"Тихий обыск":"Умеренный шум",small);
  }
  void DrawSearchProgress(){
   if(searchSlot<0)return;
   Panel(SearchProgressArea);
   DrawSearchRisk(new Rect(487,551,40,20),searchHome.noise[searchSlot],true);
   var old=GUI.color;GUI.color=new Color(.22f,.27f,.25f);GUI.DrawTexture(new Rect(541,560,202,6),Texture2D.whiteTexture);
   GUI.color=new Color(.72f,.77f,.65f);GUI.DrawTexture(new Rect(541,560,202*Mathf.Clamp01(searchTime/SearchDuration(searchHome,searchSlot)),6),Texture2D.whiteTexture);GUI.color=old;
   if(Button(SearchCancelArea,"×"))CancelSearch();
  }
 }
}
