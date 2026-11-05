#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckInventory(Action<bool,string> check,string folder){
   foreach(var enemy in infected)enemy.hp=0;
   health=100;bleeding=false;infection=drunkSeconds=0;CancelMedical();inventory.Load(new DistrictInventoryData());
   check(inventory.Capacity==6&&!inventory.Data.backpack,"fresh survivor has six pockets and no backpack");
   hero.p=backpackPosition;
   check(UseBackpackFind()&&inventory.Data.bagFound&&inventory.Count(DistrictItemKind.Backpack)==1,"world backpack is collected once through normal interaction");
   check(!UseBackpackFind(),"world backpack cannot duplicate");
   inventory.Load(new DistrictInventoryData());
   foreach(var kind in new[]{DistrictItemKind.Axe,DistrictItemKind.Spear,DistrictItemKind.Food,DistrictItemKind.Bandage,DistrictItemKind.Sterile,DistrictItemKind.Alcohol})check(inventory.TryAdd(kind,DistrictInventory.Limit((int)kind)),"fill inventory with stack "+kind);
   string full=JsonUtility.ToJson(inventory.Data);
   check(!inventory.TryAdd(DistrictItemKind.Stone,3)&&JsonUtility.ToJson(inventory.Data)==full,"full inventory rejects entire pickup without losing or granting items");
   check(!inventory.CanCraft()&&!inventory.Craft(),"full stack inventory rejects crafting when output has no space");
   var store=inventory.Container(15);
   check(DistrictInventory.Transfer(inventory.Data.slots,0,store,0)&&inventory.Data.slots[0].Empty,"deposit item in searched container frees a real pocket");
   check(inventory.Craft()&&sterileBandages==6&&bandages==4&&alcoholDoses==2,"craft atomically spends ingredients and places output");
   int sterileSlot=Array.FindIndex(inventory.Data.slots,i=>i.kind==5&&i.amount==1);
   check(DistrictInventory.Transfer(inventory.Data.slots,sterileSlot,store,1),"store crafted dressing");
   check(inventory.TryAdd(DistrictItemKind.Backpack,1),"found bag fits freed pocket");
   int bag=Array.FindIndex(inventory.Data.slots,i=>i.kind==8);
   check(inventory.Equip(bag)&&inventory.Capacity==18,"equipping school backpack unlocks twelve extra slots");
   check(DistrictInventory.Transfer(inventory.Data.slots,1,inventory.Data.slots,12),"item can move into backpack row");
   check(!inventory.UnequipBag()&&inventory.Data.backpack,"occupied backpack refuses unequip");
   check(DistrictInventory.Transfer(inventory.Data.slots,12,inventory.Data.slots,1),"item returns from bag to pockets");
   check(inventory.UnequipBag()&&inventory.Capacity==6,"empty bag can be removed into pockets");
   check(!DistrictInventory.Transfer(inventory.Data.slots,-1,store,0),"invalid drag leaves model intact");

   hero.p=homes[5].p+new Vector2(2,1);Save();string snapshot=File.ReadAllText(SavePath);
   inventory.Load(new DistrictInventoryData());
   check(Load()&&inventory.Data.containers.Find(c=>c.index==15).items[0].kind==1&&alcoholDoses==2,"v5 round trip preserves container stash, bag and partial bottle");
   var old=JsonUtility.FromJson<SaveData>(snapshot);old.version=4;old.food=100;old.bandages=80;old.alcohol=11;old.sterile=12;old.stones=25;old.weapon=2;
   File.WriteAllText(SavePath,JsonUtility.ToJson(old));check(Load()&&weapon==2&&inventory.Data.overflow.Count>0,"large old inventory migrates with recoverable overflow");
   int Total(DistrictItemKind kind){int n=inventory.Count(kind);foreach(var item in inventory.Data.overflow)if(item.kind==(int)kind)n+=item.amount;return n;}
   check(Total(DistrictItemKind.Food)==100&&Total(DistrictItemKind.Bandage)==80&&Total(DistrictItemKind.Alcohol)==11&&Total(DistrictItemKind.Sterile)==12&&Total(DistrictItemKind.Stone)==25,"migration preserves every legacy item and alcohol dose");
   var bad=JsonUtility.FromJson<SaveData>(snapshot);bad.inventory.slots[0]=new DistrictItem((DistrictItemKind)99,1);
   File.WriteAllText(SavePath,JsonUtility.ToJson(bad));File.WriteAllText(SavePath+".bak","broken");int before=food;
   check(!Load()&&food==before,"invalid slot is rejected before mutating world");
   File.WriteAllText(SavePath,snapshot);check(Load(),"valid slot save restores after rejected payload");

   var gesture=new MenuPointer();gesture.Sample(new Vector2(420,210),true);gesture.Sample(new Vector2(670,290),true);gesture.Sample(new Vector2(670,290),false);
   check(gesture.TakeDrop()&&gesture.Dragged&&!gesture.TakeClick(ItemCell(0)),"drag release is distinct from a click and consumed once");
   File.WriteAllText(Path.Combine(folder,"inventory-note.txt"),"Capacity, atomic pickups, crafting, equipment, persistent stash and v4 migration tested in Windows Player. Grid gestures additionally require graphical inspection.");
   yield return null;
  }
 }
}
#endif
