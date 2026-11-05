using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  enum MedicalAction {None,Bandage,Sterile,Craft,Drink,Eat,Water,Rest}
  sealed class Supply {public Vector2 p;public string label;public int kind;public bool taken;public GameObject model;}
  readonly List<Supply> supplies=new List<Supply>();

  bool bleeding,suppliesOpen,sneakToggle;
  float infection,drunkSeconds,medicalTime,throwCooldown;
  MedicalAction medicalAction;
  static readonly Rect InventoryButton=new Rect(810,475,200,65),SneakButton=new Rect(810,395,200,65),ThrowButton=new Rect(810,315,200,65);
  void BuildSurvivalSupplies(){
   AddSupply(new Vector2(14,-13),0,"Этиловый спирт · 3 дозы");
   AddSupply(new Vector2(-15,-14),0,"Этиловый спирт · 3 дозы");
   AddSupply(new Vector2(99,43),0,"Этиловый спирт · 3 дозы");
   AddSupply(new Vector2(9,-14),1,"Перевязочный набор · 2 бинта");
   AddSupply(new Vector2(101,43),1,"Перевязочный набор · 2 бинта");
   AddSupply(new Vector2(-14,-20),2,"Камни · 3 штуки");
   AddSupply(new Vector2(61,0),2,"Камни · 3 штуки");
  }
  void AddSupply(Vector2 p,int kind,string label){
   var root=new GameObject(label);root.transform.position=V(p);var supply=new Supply{p=p,kind=kind,label=label,model=root};supplies.Add(supply);
   if(kind==0){
    Cube("Amber alcohol bottle",new Vector3(0,.36f,0),new Vector3(.28f,.55f,.28f),new Color(.29f,.23f,.12f),root.transform);
    Cube("Bottle neck",new Vector3(0,.7f,0),new Vector3(.12f,.16f,.12f),new Color(.28f,.25f,.19f),root.transform);
    Cube("White bottle label",new Vector3(0,.38f,-.145f),new Vector3(.23f,.22f,.02f),new Color(.78f,.76f,.61f),root.transform);
   }else if(kind==1){Cube("Bandage packet",new Vector3(0,.18f,0),new Vector3(.55f,.24f,.4f),new Color(.74f,.73f,.62f),root.transform);}
   else for(int i=0;i<3;i++)Cube("Loose stone",new Vector3(i*.16f,.12f,(i%2)*.13f),new Vector3(.2f,.16f,.23f),new Color(.35f,.37f,.35f),root.transform);
  }
  Supply NearbySupply(){Supply closest=null;float distance=1.25f;foreach(var s in supplies)if(!s.taken&&Vector2.Distance(hero.p,s.p)<distance&&Sight(hero.p,s.p)){closest=s;distance=Vector2.Distance(hero.p,s.p);}return closest;}
  bool UseSupply(){
   var s=NearbySupply();if(s==null)return false;
   var kind=s.kind==0?DistrictItemKind.Alcohol:s.kind==1?DistrictItemKind.Bandage:DistrictItemKind.Stone;int count=s.kind==0||s.kind==2?3:2;
   if(!TryCarry(kind,count)){Say("Нет свободных ячеек. Освободи место в рюкзаке.");return true;}
   s.taken=true;s.model.SetActive(false);Save();Say("Подобрано: "+s.label);return true;
  }
  Vector2 DrunkDirection(Vector2 direction){if(drunkSeconds<=0)return direction;float angle=Mathf.Sin(Time.time*2.2f)*.16f;return new Vector2(direction.x*Mathf.Cos(angle)-direction.y*Mathf.Sin(angle),direction.x*Mathf.Sin(angle)+direction.y*Mathf.Cos(angle));}
  void WoundHero(){bleeding=true;infection=Mathf.Min(100,Mathf.Max(12,infection+12));CancelMedical();if(foley)foley.Wound();Say("РАНЕНИЕ: кровотечение и заражение раны. Нужны бинты; I — лечение и крафт.");}
  void TickAfflictions(float dt){
   drunkSeconds=Mathf.Max(0,drunkSeconds-dt);throwCooldown=Mathf.Max(0,throwCooldown-dt);
   if(infection>0)infection=Mathf.Min(100,infection+dt*.22f);
   health-=dt*((bleeding?.4f:0)+(infection>=50?.15f:0));
   if(health<=0&&!dead){health=0;dead=true;attack=0;strikeSequence++;CancelMedical();if(hero.visual)hero.visual.Die();Say("Ранения оказались смертельными.");}
  }
  void CancelMedical(){if(foley)foley.StopTask();medicalAction=MedicalAction.None;medicalTime=0;}
  float MedicalDuration(MedicalAction action)=>action==MedicalAction.Craft?3:action==MedicalAction.Drink?1.2f:action==MedicalAction.Eat?1.8f:action==MedicalAction.Water?1.5f:action==MedicalAction.Rest?6:2.5f;
  bool CanTreat(MedicalAction action){
   switch(action){
    case MedicalAction.Bandage:return bandages>0&&(bleeding||health<100);
    case MedicalAction.Sterile:return sterileBandages>0&&(bleeding||infection>0||health<100);
    case MedicalAction.Craft:return inventory.CanCraft();
    case MedicalAction.Eat:return food>0&&nourishment<95;
    case MedicalAction.Water:return inventory.Count(DistrictItemKind.Water)>0&&hydration<98;
    case MedicalAction.Rest:return tiredness>5&&SafeToRest();
    case MedicalAction.Drink:return alcoholDoses>0;
    default:return false;
   }
  }
  bool BeginMedical(MedicalAction action){
   if(!playing||paused||dead||attack>0||heroStagger>0||medicalAction!=MedicalAction.None)return false;
   if(!CanTreat(action)){Say(MedicalUnavailable(action));return false;}
   inventory.Preferred=inventorySelection;medicalAction=action;medicalTime=0;if(foley)foley.StartTask(action==MedicalAction.Craft,action==MedicalAction.Drink||action==MedicalAction.Water);searchSlot=-1;suppliesOpen=false;heroVelocity=Vector2.zero;return true;
  }
  void TickMedical(float dt){
   if(medicalAction==MedicalAction.None)return;
   if(medicalAction==MedicalAction.Rest&&!SafeToRest()){CancelMedical();Say("Отдых прерван. Сначала обеспечь безопасность.");return;}
   medicalTime+=dt;if(medicalTime<MedicalDuration(medicalAction))return;
   var action=medicalAction;CancelMedical();if(!CanTreat(action))return;if(foley)foley.FinishTask(action==MedicalAction.Drink||action==MedicalAction.Water);
   if(action==MedicalAction.Eat){food--;nourishment=Mathf.Min(100,nourishment+35);stamina=Mathf.Min(StaminaLimit,stamina+8);Say("Поел. Голод отступил.");}
   if(action==MedicalAction.Water){inventory.Remove(DistrictItemKind.Water,1);hydration=Mathf.Min(100,hydration+28);Say("Выпил воды. В бутылке осталось "+inventory.Count(DistrictItemKind.Water)+" глотков в запасе.");}
   if(action==MedicalAction.Rest){tiredness=Mathf.Max(0,tiredness-45);stamina=Mathf.Min(StaminaLimit,stamina+40);Say("Отдохнул. Можно готовиться к новой вылазке.");}
   if(action==MedicalAction.Craft){if(!inventory.Craft())return;Say("Создан стерильный бинт: 1 бинт + 1 доза спирта.");}
   if(action==MedicalAction.Bandage){bandages--;bleeding=false;health=Mathf.Min(100,health+8);Say(infection>0?"Кровотечение остановлено. Заражение осталось: нужен стерильный бинт.":"Перевязка завершена.");}
   if(action==MedicalAction.Sterile){sterileBandages--;bleeding=false;infection=0;health=Mathf.Min(100,health+12);Say("Рана обработана: кровотечение и заражение сняты.");}
   if(action==MedicalAction.Drink){alcoholDoses--;drunkSeconds=Mathf.Min(90,drunkSeconds+45);Say("Опьянение: хуже координация и восстановление сил. Рана не вылечена.");}
   Save();
  }
  Vector2 StoneTarget(Vector2 direction){Vector2 end=hero.p;for(float t=.4f;t<=8;t+=.4f){var next=hero.p+direction*t;if(Blocked(next))break;end=next;}return end;}
  bool ThrowStone(){
   if(!playing||paused||dead||suppliesOpen||attack>0||heroStagger>0||throwCooldown>0)return false;
   if(stones<=0){Say("Нет камней. Ищи их у дороги рядом с началом района.");return false;}
   CancelMedical();Vector2 direction=face.sqrMagnitude>.01f?DrunkDirection(face.normalized):Vector2.up;Vector2 end=StoneTarget(direction);
   stones--;throwCooldown=.8f;StartCoroutine(StoneFlight(hero.p,end,worldGeneration));Save();return true;
  }
  IEnumerator StoneFlight(Vector2 start,Vector2 end,int generation){
   var stone=Cube("Thrown distraction stone",V(start,1),Vector3.one*.16f,new Color(.5f,.5f,.46f));float elapsed=0;
   while(elapsed<.55f){yield return null;if(generation!=worldGeneration||dead){Destroy(stone);yield break;}if(paused)continue;elapsed+=Mathf.Min(Time.deltaTime,.05f);float t=Mathf.Clamp01(elapsed/.55f);stone.transform.position=V(Vector2.Lerp(start,end,t),Mathf.Lerp(1,.1f,t)+Mathf.Sin(t*Mathf.PI)*1.7f);}
   HearNoise(end,14);sound.Play("block");Destroy(stone,2);Say("Камень упал. Заражённые поблизости могут пойти на звук.");
  }
  void DrawSurvivalHud(){
   if(suppliesOpen)return;
   var nearby=NearbySupply();if(nearby!=null)GUI.Label(new Rect(35,235,720,30),nearby.label+" · действие / E",small);
   if(nearby==null&&!inventory.Data.bagFound&&Vector2.Distance(hero.p,backpackPosition)<1.25f)GUI.Label(new Rect(35,235,720,30),"Школьный рюкзак · действие / E",small);
   if(Button(InventoryButton,"РЮКЗАК · I"))suppliesOpen=!suppliesOpen;
   if(Button(SneakButton,sneakToggle?"ТИХИЙ ШАГ: ВКЛ":"ТИХИЙ ШАГ"))sneakToggle=!sneakToggle;
   if(Button(ThrowButton,"БРОСИТЬ · G · "+stones)&&!suppliesOpen)ThrowStone();
   DrawMedicalProgress();
  }
 }
}
