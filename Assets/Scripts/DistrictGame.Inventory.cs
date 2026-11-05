using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  readonly DistrictInventory inventory=new DistrictInventory();
  int weapon {get=>inventory.Data.weapon;set=>inventory.Data.weapon=value;}
  int food {get=>inventory.Count(DistrictItemKind.Food);set=>inventory.SetCount(DistrictItemKind.Food,value);}
  int bandages {get=>inventory.Count(DistrictItemKind.Bandage);set=>inventory.SetCount(DistrictItemKind.Bandage,value);}
  int sterileBandages {get=>inventory.Count(DistrictItemKind.Sterile);set=>inventory.SetCount(DistrictItemKind.Sterile,value);}
  int alcoholDoses {get=>inventory.Count(DistrictItemKind.Alcohol);set=>inventory.SetCount(DistrictItemKind.Alcohol,value);}
  int stones {get=>inventory.Count(DistrictItemKind.Stone);set=>inventory.SetCount(DistrictItemKind.Stone,value);}
  string hoveredInventoryItem;
  int inventorySelection=-1,dragSlot=-1,dragArea,overflowPage;
  GameObject backpackFind,wornBackpack;
  bool inventoryMouseHeld,inventoryDropPending;
  Vector2 inventoryMouseOrigin,inventoryMouseEnd;
  void ReadInventoryMouse(InputEventPtr e,InputDevice device){
   if(!(device is Mouse mouse)||(!e.IsA<StateEvent>()&&!e.IsA<DeltaStateEvent>()))return;
   if(!mouse.leftButton.ReadValueFromEvent(e,out float button))return;
   if(!mouse.position.ReadValueFromEvent(e,out Vector2 position))position=mouse.position.ReadValue();
   bool down=button>.5f;
   if(down&&!inventoryMouseHeld)inventoryMouseOrigin=ScreenLayout.ToUI(position);
   if(!down&&inventoryMouseHeld&&suppliesOpen){inventoryMouseEnd=ScreenLayout.ToUI(position);inventoryDropPending=true;}
   inventoryMouseHeld=down;
  }
  readonly Vector2 backpackPosition=new Vector2(-14.5f,-14.8f);
  static readonly Rect GridPanel=new Rect(100,100,1080,530),HandSlot=new Rect(155,454,80,80),BagSlot=new Rect(265,454,80,80);
  static Rect ItemCell(int i)=>new Rect(400+i%6*80,190+i/6*80,72,72);
  static Rect StoreCell(int i)=>new Rect(400+i*80,470,72,72);
  static readonly string[] ItemNames={"","Топор","Копьё","Консервы","Бинт","Стерильный бинт","Этиловый спирт","Камни","Школьный рюкзак"};
  bool TryCarry(DistrictItemKind kind,int amount){
   if((kind==DistrictItemKind.Axe||kind==DistrictItemKind.Spear)&&weapon==0&&amount==1){weapon=(int)kind;return true;}
   return inventory.TryAdd(kind,amount);
  }
  GameObject MakeBackpack(string name,Vector3 position,Transform parent=null){
   var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=position;
   Cube("Canvas pack",Vector3.zero,new Vector3(.47f,.57f,.24f),new Color(.19f,.25f,.29f),root.transform);
   Cube("Front pocket",new Vector3(0,-.11f,-.15f),new Vector3(.37f,.22f,.12f),new Color(.24f,.3f,.33f),root.transform);
   for(int side=-1;side<=1;side+=2)Cube("Shoulder strap",new Vector3(side*.16f,0,.15f),new Vector3(.06f,.6f,.05f),new Color(.13f,.16f,.17f),root.transform);
   Cube("Worn cloth label",new Vector3(.06f,.16f,-.13f),new Vector3(.16f,.08f,.015f),new Color(.62f,.58f,.43f),root.transform);
   Cube("Handle",new Vector3(0,.32f,0),new Vector3(.18f,.05f,.06f),new Color(.13f,.16f,.17f),root.transform);
   return root;
  }
  void BuildBackpackFind(){backpackFind=MakeBackpack("School backpack to find",V(backpackPosition,.7f));}
  bool UseBackpackFind(){
   if(inventory.Data.bagFound||Vector2.Distance(hero.p,backpackPosition)>=1.25f||!Sight(hero.p,backpackPosition))return false;
   if(!inventory.TryAdd(DistrictItemKind.Backpack,1)){Say("Освободи ячейку для рюкзака.");return true;}
   inventory.Data.bagFound=true;backpackFind.SetActive(false);Save();Say("Найден школьный рюкзак. I — надень его на персонажа.");return true;
  }
  void UpdateWornBackpack(){
   if(!wornBackpack){wornBackpack=MakeBackpack("Worn school backpack",new Vector3(0,1.2f,-.32f),hero.root);foreach(var child in wornBackpack.GetComponentsInChildren<Transform>())child.gameObject.layer=30;}
   wornBackpack.SetActive(inventory.Data.backpack);
   if(backpackFind)backpackFind.SetActive(!inventory.Data.bagFound);
  }
  int NearbyStore(){
   if(current==null)return -1;int index=NearbyContainer(current);
   return index>=0&&current.taken[index]?homes.IndexOf(current)*3+index:-1;
  }
  void InventoryCell(Rect area,DistrictItem item,int index,bool locked){
   FillRect(area,locked?new Color(.07f,.09f,.09f):index==inventorySelection?new Color(.28f,.38f,.31f):new Color(.13f,.18f,.18f));
   if(!item.Empty&&!locked){
    if(area.Contains(pointer.Position))hoveredInventoryItem=ItemNames[item.kind]+" · "+item.amount+(item.kind==6?" доз":"");
    GUI.DrawTexture(new Rect(area.x+7,area.y+4,58,58),ItemIcon(item.kind),ScaleMode.ScaleToFit,true);
    if(item.amount>1||item.kind==6)GUI.Label(new Rect(area.x+40,area.y+47,30,24),item.amount.ToString(),small);
   }else if(locked){GUI.color=new Color(.34f,.38f,.35f);GUI.Label(new Rect(area.x+16,area.y+22,50,30),"—",text);GUI.color=Color.white;}
  }
  void DrawSuppliesMenu(){
   if(!suppliesOpen){dragSlot=-1;return;}
   hoveredInventoryItem=null;EnsureInventoryArt();
   FillRect(new Rect(0,0,1280,720),new Color(.01f,.02f,.025f,.72f));FillRect(GridPanel,new Color(.04f,.065f,.07f));
   GUI.Label(new Rect(130,112,600,45),"СНАРЯЖЕНИЕ",large);
   if(Button(new Rect(1000,116,150,48),"Закрыть · I")){suppliesOpen=false;dragSlot=-1;}
   GUI.Label(new Rect(400,147,490,30),(inventory.Data.backpack?"Школьный рюкзак":"Карманы")+" · "+inventory.Capacity+" ячеек",text);
   DrawBodyPreview(new Rect(132,175,225,260));
   InventoryCell(HandSlot,weapon==0?default:new DistrictItem((DistrictItemKind)weapon,1),-2,false);
   InventoryCell(BagSlot,inventory.Data.backpack?new DistrictItem(DistrictItemKind.Backpack,1):default,-3,false);
   GUI.Label(new Rect(155,536,90,24),"В руках",small);GUI.Label(new Rect(265,536,95,24),"На спине",small);
   if(Button(new Rect(145,568,95,40),"Снять")&&medicalAction==MedicalAction.None){if(inventory.UnequipWeapon())Save();else if(weapon>0)Say("Нет свободной ячейки для оружия.");}
   if(Button(new Rect(255,568,105,40),"Снять")&&medicalAction==MedicalAction.None&&inventory.Data.backpack){if(inventory.UnequipBag())Save();else Say("Освободи дополнительные ячейки и место в карманах.");}
   for(int i=0;i<18;i++)InventoryCell(ItemCell(i),inventory.Data.slots[i],i,i>=inventory.Capacity);
   int store=NearbyStore();bool overflow=store<0&&inventory.Data.overflow.Count>0;DistrictItem[] external=null;
   if(store>=0)external=inventory.Container(store);
   else if(overflow){external=new DistrictItem[6];overflowPage=Mathf.Clamp(overflowPage,0,(inventory.Data.overflow.Count-1)/6);for(int i=0;i<6;i++)if(overflowPage*6+i<inventory.Data.overflow.Count)external[i]=inventory.Data.overflow[overflowPage*6+i];}
   GUI.Label(new Rect(400,438,480,30),store>=0?"Обысканный контейнер":overflow?"Вещи из прежнего сохранения":"Подойди к обысканному контейнеру",small);
   if(external!=null)for(int i=0;i<6;i++)InventoryCell(StoreCell(i),external[i],-10-i,false);
   if(overflow){
    if(Button(new Rect(900,550,70,40),"←"))overflowPage=Mathf.Max(0,overflowPage-1);
    if(Button(new Rect(980,550,70,40),"→"))overflowPage++;
   }
   if(inventorySelection>=0&&inventorySelection<inventory.Capacity&&!inventory.Data.slots[inventorySelection].Empty){
    var item=inventory.Data.slots[inventorySelection];
    GUI.Label(new Rect(905,190,245,64),ItemNames[item.kind],text);
    if(item.kind<=2||item.kind==8){
     GUI.Label(new Rect(905,260,245,80),item.kind==8?"+12 ячеек. Освободи карманы, чтобы снять.":"Перенеси на силуэт или возьми в руки.",small);
     if(Button(new Rect(905,355,245,58),item.kind==8?"Надеть":"Взять в руки")&&medicalAction==MedicalAction.None&&inventory.Equip(inventorySelection)){inventorySelection=-1;Save();}
    }else{
     GUI.Label(new Rect(905,257,245,60),item.kind==6?"В бутылке "+item.amount+" из 3 доз":item.kind==7?"Отвлекают заражённых":item.kind==3?"Еда пригодится в убежище":"Перевязка занимает 2,5 с",small);
     if(item.kind==4)MedicalButton(new Rect(905,334,245,56),"Перевязать · B",MedicalAction.Bandage);
     if(item.kind==5)MedicalButton(new Rect(905,334,245,56),"Обработать · H",MedicalAction.Sterile);
     if(item.kind==4||item.kind==6)MedicalButton(new Rect(905,402,245,56),"Стерилизовать",MedicalAction.Craft);
     if(item.kind==6)MedicalButton(new Rect(905,470,245,56),"Выпить · опьянение",MedicalAction.Drink);
    }
   }else GUI.Label(new Rect(905,195,245,120),"Выбери предмет. Перетаскивай вещи между ячейками и открытым контейнером.",small);
   GUI.color=MedicalWarning;GUI.Label(new Rect(400,565,475,48),medicalAction!=MedicalAction.None?"Действие выполняется. Перенос после завершения.":hoveredInventoryItem??"Мир продолжает жить. Держи вход закрытым.",small);GUI.color=Color.white;
   if(Event.current.type!=EventType.Repaint)return;
   Vector2 dropPosition=pointer.Position;bool dropDragged=pointer.Dragged;
   bool rawDrop=false;
   if(inventoryDropPending){
    inventoryDropPending=false;
    if(medicalAction==MedicalAction.None){
     for(int i=0;i<inventory.Capacity;i++)if(ItemCell(i).Contains(inventoryMouseOrigin)&&!inventory.Data.slots[i].Empty){dragSlot=i;dragArea=0;rawDrop=true;}
     if(external!=null)for(int i=0;i<6;i++)if(StoreCell(i).Contains(inventoryMouseOrigin)&&!external[i].Empty){dragSlot=i;dragArea=1;rawDrop=true;}
    }
    if(rawDrop){dropPosition=inventoryMouseEnd;dropDragged=Vector2.Distance(inventoryMouseOrigin,inventoryMouseEnd)>25;pointer.TakeDrop();}
   }
   if(pointer.Held&&dragSlot<0&&medicalAction==MedicalAction.None){
    for(int i=0;i<inventory.Capacity;i++)if(ItemCell(i).Contains(pointer.Origin)&&!inventory.Data.slots[i].Empty){dragSlot=i;dragArea=0;}
    if(external!=null)for(int i=0;i<6;i++)if(StoreCell(i).Contains(pointer.Origin)&&!external[i].Empty){dragSlot=i;dragArea=1;}
   }
   if(dragSlot>=0&&pointer.Held&&pointer.Dragged){
    var moving=dragArea==0?inventory.Data.slots[dragSlot]:external!=null?external[dragSlot]:default;
    if(!moving.Empty)GUI.DrawTexture(new Rect(pointer.Position.x-28,pointer.Position.y-28,56,56),ItemIcon(moving.kind));
   }
   if(dragSlot>=0&&(rawDrop||!pointer.Held)){
    int source=dragSlot;dragSlot=-1;
    if(!rawDrop&&!pointer.TakeDrop())return;
    if(!dropDragged){inventorySelection=dragArea==0?source:-1;return;}
    bool changed=false;
    if(dragArea==0&&(HandSlot.Contains(dropPosition)||BagSlot.Contains(dropPosition))){
     int kind=inventory.Data.slots[source].kind;
     if(HandSlot.Contains(dropPosition)&&(kind==1||kind==2)||BagSlot.Contains(dropPosition)&&kind==8)changed=inventory.Equip(source);
    }else for(int i=0;i<inventory.Capacity;i++)if(ItemCell(i).Contains(dropPosition)){
     if(dragArea==0)changed=DistrictInventory.Transfer(inventory.Data.slots,source,inventory.Data.slots,i);
     else if(external!=null&&(!overflow||inventory.Data.slots[i].Empty||inventory.Data.slots[i].kind==external[source].kind))changed=DistrictInventory.Transfer(external,source,inventory.Data.slots,i);
     if(changed)inventorySelection=i;
    }
    if(dragArea==0&&store>=0)for(int i=0;i<6;i++)if(StoreCell(i).Contains(dropPosition))changed=DistrictInventory.Transfer(inventory.Data.slots,source,external,i);
    if(changed){
     if(overflow){int offset=overflowPage*6;int count=Math.Min(6,inventory.Data.overflow.Count-offset);inventory.Data.overflow.RemoveRange(offset,count);for(int i=5;i>=0;i--)if(!external[i].Empty)inventory.Data.overflow.Insert(offset,external[i]);}
     Save();
    }
   }
  }
 }
}
