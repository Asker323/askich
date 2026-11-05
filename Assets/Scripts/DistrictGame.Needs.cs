using System;
using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  float nourishment=70,hydration=60,tiredness=15;
  readonly Vector2[] waterPositions={new Vector2(-9,-11.7f),new Vector2(14,-13.8f),new Vector2(99,41)};
  readonly bool[] waterTaken=new bool[3];
  readonly List<GameObject> waterModels=new List<GameObject>();
  static readonly Rect RestButton=new Rect(35,310,260,54);
  float StaminaLimit=>Mathf.Min(infection>=50?65:100,hydration<20?70:100,nourishment<20?80:100,tiredness>=70?65:100);
  float StaminaRecovery=>Mathf.Clamp(1-tiredness*.006f-(hydration<20?.2f:0)-(nourishment<20?.15f:0),.25f,1);
  void TickNeeds(float dt,bool running){
   nourishment=Mathf.Max(0,nourishment-dt*(running?.033f:.022f));
   hydration=Mathf.Max(0,hydration-dt*(running?.052f:.035f));
   if(medicalAction!=MedicalAction.Rest)tiredness=Mathf.Min(100,tiredness+dt*(running?.032f:.017f));
  }
  bool SafeToRest()=>campEstablished&&Vector2.Distance(hero.p,refuge)<4&&!bleeding&&infection<50&&!infected.Exists(z=>z.hp>0&&Vector2.Distance(z.p,hero.p)<10&&Sight(z.p,hero.p));
  string NeedsAdvice=>hydration<35?"Пора выпить воды":nourishment<35?"Пора поесть":tiredness>=60?"Нужен отдых у убежища":"Силы для вылазки есть";
  Vector2 cistern=>refuge+new Vector2(3,-2);
  bool NearCistern=>Vector2.Distance(hero.p,cistern)<2&&Sight(hero.p,cistern);
  bool UseCistern(){if(!NearCistern)return false;BeginMedical(MedicalAction.Refill);return true;}
  void BuildWaterFinds(){
   var reservoir=new GameObject("Drinking water reserve");reservoir.transform.position=V(cistern,.7f);
   var barrel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(barrel.GetComponent<Collider>());barrel.transform.SetParent(reservoir.transform,false);barrel.transform.localScale=new Vector3(1,.7f,1);barrel.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.25f,.4f,.44f));
   Cube("Water tap",new Vector3(0,-.3f,-.58f),new Vector3(.15f,.1f,.23f),new Color(.6f,.6f,.5f),reservoir.transform);
   Cube("Blue water marker",new Vector3(0,.2f,-.51f),new Vector3(.38f,.4f,.025f),new Color(.48f,.69f,.77f),reservoir.transform);
   for(int i=0;i<waterPositions.Length;i++){
    var root=new GameObject("Drinking water bottle");root.transform.position=V(waterPositions[i],.45f);
    MakeWaterModel(root.transform);waterModels.Add(root);
   }
  }
  void MakeWaterModel(Transform parent){
   var body=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(body.GetComponent<Collider>());
   body.name="Blue water bottle";body.transform.SetParent(parent,false);body.transform.localScale=new Vector3(.24f,.25f,.24f);body.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.25f,.47f,.55f));
   Cube("Water label",new Vector3(0,0,-.125f),new Vector3(.2f,.18f,.025f),new Color(.71f,.81f,.78f),parent);
   Cube("Blue bottle cap",new Vector3(0,.29f,0),new Vector3(.13f,.1f,.13f),new Color(.18f,.31f,.45f),parent);
  }
  int NearbyWater(){
   for(int i=0;i<waterPositions.Length;i++)if(!waterTaken[i]&&Vector2.Distance(hero.p,waterPositions[i])<.95f&&Sight(hero.p,waterPositions[i]))return i;
   return -1;
  }
  bool UseWaterFind(){
   int i=NearbyWater();if(i<0)return false;
   if(!inventory.TryAdd(DistrictItemKind.Water,3)){Say("Освободи ячейку для воды.");return true;}
   waterTaken[i]=true;waterModels[i].SetActive(false);Save();Say("Питьевая вода · 3 глотка. Выбери бутылку в рюкзаке.");
   return true;
  }
  void DrawNeedsAction(){
   if(suppliesOpen)return;
   if(NearCistern)GUI.Label(new Rect(35,235,720,30),"Запас питьевой воды · наполнить бутылку / E",small);
   if(NearbyWater()>=0)GUI.Label(new Rect(35,235,720,30),"Питьевая вода · действие / E",small);
   if(campEstablished&&Vector2.Distance(hero.p,refuge)<4){
    if(Button(RestButton,"Отдохнуть · R · 6 с"))BeginMedical(MedicalAction.Rest);
   }
  }
 }
}
