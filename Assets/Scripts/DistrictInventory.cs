using System;
using System.Collections.Generic;

namespace Askich {
 public enum DistrictItemKind {Empty,Axe,Spear,Food,Bandage,Sterile,Alcohol,Stone,Backpack}
 [Serializable] public struct DistrictItem {
  public int kind,amount;
  public DistrictItem(DistrictItemKind type,int count){kind=(int)type;amount=count;}
  public bool Empty=>kind==0;
 }
 [Serializable] public sealed class DistrictContainerItems {
  public int index;
  public DistrictItem[] items=new DistrictItem[6];
 }
 [Serializable] public sealed class DistrictInventoryData {
  public DistrictItem[] slots=new DistrictItem[18];
  public List<DistrictItem> overflow=new List<DistrictItem>();
  public List<DistrictContainerItems> containers=new List<DistrictContainerItems>();
  public int weapon;
  public bool backpack,bagFound;
 }
 public sealed class DistrictInventory {
  public DistrictInventoryData Data {get;private set;}=new DistrictInventoryData();
  public int Preferred=-1;
  public int Capacity=>Data.backpack?18:6;
  public int Count(DistrictItemKind kind){int total=0;for(int i=0;i<Capacity;i++)if(Data.slots[i].kind==(int)kind)total+=Data.slots[i].amount;return total;}
  public static int Limit(int kind){switch((DistrictItemKind)kind){case DistrictItemKind.Food:case DistrictItemKind.Bandage:case DistrictItemKind.Sterile:return 5;case DistrictItemKind.Alcohol:return 3;case DistrictItemKind.Stone:return 10;default:return 1;}}
  static bool ValidItem(DistrictItem item)=>item.kind>=0&&item.kind<=8&&(item.kind==0?item.amount==0:item.amount>0&&item.amount<=Limit(item.kind));
  public static void Validate(DistrictInventoryData data,int containerCount){
   if(data==null||data.slots==null||data.slots.Length!=18||data.overflow==null||data.overflow.Count>60000||data.containers==null||data.containers.Count>containerCount||data.weapon<0||data.weapon>2)throw new System.IO.InvalidDataException("Повреждён инвентарь");
   int capacity=data.backpack?18:6;
   for(int i=0;i<18;i++)if(!ValidItem(data.slots[i])||(i>=capacity&&!data.slots[i].Empty))throw new System.IO.InvalidDataException("Неверная ячейка");
   foreach(var item in data.overflow)if(!ValidItem(item)||item.Empty)throw new System.IO.InvalidDataException("Неверный перенос вещей");
   var indices=new HashSet<int>();
   foreach(var c in data.containers){
    if(c==null||c.index<0||c.index>=containerCount||!indices.Add(c.index)||c.items==null||c.items.Length!=6)throw new System.IO.InvalidDataException("Неверное хранилище");
    foreach(var item in c.items)if(!ValidItem(item))throw new System.IO.InvalidDataException("Неверный предмет хранилища");
   }
   if(data.backpack&&!data.bagFound)throw new System.IO.InvalidDataException("Не найден надетый рюкзак");
  }
  public void Load(DistrictInventoryData data){Data=data;Preferred=-1;}
  static int AddTo(DistrictItem[] slots,int capacity,DistrictItem item){
   for(int i=0;i<capacity;i++)if(slots[i].kind==item.kind&&slots[i].amount<Limit(item.kind)){
    int n=Math.Min(item.amount,Limit(item.kind)-slots[i].amount);slots[i].amount+=n;item.amount-=n;
   }
   for(int i=0;i<capacity&&item.amount>0;i++)if(slots[i].Empty){
    int n=Math.Min(item.amount,Limit(item.kind));slots[i]=new DistrictItem((DistrictItemKind)item.kind,n);item.amount-=n;
   }
   return item.amount;
  }
  public bool TryAdd(DistrictItemKind kind,int amount){
   if(amount<=0)return false;
   var copy=(DistrictItem[])Data.slots.Clone();
   if(AddTo(copy,Capacity,new DistrictItem(kind,amount))>0)return false;
   Data.slots=copy;return true;
  }
  public void SetCount(DistrictItemKind kind,int amount){
   if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
   int old=Count(kind);
   if(amount<old){Remove(kind,old-amount);return;}
   int remaining=AddTo(Data.slots,Capacity,new DistrictItem(kind,amount-old));
   while(remaining>0){int n=Math.Min(remaining,Limit((int)kind));Data.overflow.Add(new DistrictItem(kind,n));remaining-=n;}
  }
  public bool Remove(DistrictItemKind kind,int amount){
   if(amount<0||Count(kind)<amount)return false;
   void Take(int i){if(i<0||i>=Capacity||Data.slots[i].kind!=(int)kind)return;int n=Math.Min(amount,Data.slots[i].amount);Data.slots[i].amount-=n;amount-=n;if(Data.slots[i].amount==0)Data.slots[i]=default;}
   Take(Preferred);
   for(int i=0;i<Capacity&&amount>0;i++)if(i!=Preferred)Take(i);
   return true;
  }
  public bool CanCraft(){var candidate=new DistrictInventory();candidate.Data.slots=(DistrictItem[])Data.slots.Clone();candidate.Data.backpack=Data.backpack;return candidate.Craft();}
  public bool Craft(){
   var copy=new DistrictInventory();copy.Data.slots=(DistrictItem[])Data.slots.Clone();copy.Data.backpack=Data.backpack;copy.Preferred=Preferred;
   if(!copy.Remove(DistrictItemKind.Bandage,1)||!copy.Remove(DistrictItemKind.Alcohol,1)||!copy.TryAdd(DistrictItemKind.Sterile,1))return false;
   Data.slots=copy.Data.slots;return true;
  }
  public static bool Transfer(DistrictItem[] from,int source,DistrictItem[] to,int target){
   if(source<0||source>=from.Length||target<0||target>=to.Length||ReferenceEquals(from,to)&&source==target||from[source].Empty)return false;
   var item=from[source];
   if(to[target].kind==item.kind&&to[target].amount<Limit(item.kind)){
    int n=Math.Min(item.amount,Limit(item.kind)-to[target].amount);to[target].amount+=n;from[source].amount-=n;if(from[source].amount==0)from[source]=default;
   }else{from[source]=to[target];to[target]=item;}
   return true;
  }
  public bool Equip(int index){
   if(index<0||index>=Capacity)return false;
   var item=Data.slots[index];
   if(item.kind==8&&!Data.backpack){Data.slots[index]=default;Data.backpack=Data.bagFound=true;return true;}
   if(item.kind!=1&&item.kind!=2)return false;
   Data.slots[index]=Data.weapon==0?default:new DistrictItem((DistrictItemKind)Data.weapon,1);
   Data.weapon=item.kind;return true;
  }
  public bool UnequipWeapon(){if(Data.weapon==0||!TryAdd((DistrictItemKind)Data.weapon,1))return false;Data.weapon=0;return true;}
  public bool UnequipBag(){
   if(!Data.backpack)return false;
   for(int i=6;i<18;i++)if(!Data.slots[i].Empty)return false;
   var copy=(DistrictItem[])Data.slots.Clone();
   if(AddTo(copy,6,new DistrictItem(DistrictItemKind.Backpack,1))>0)return false;
   Data.slots=copy;Data.backpack=false;return true;
  }
  public DistrictItem[] Container(int index){
   var store=Data.containers.Find(c=>c.index==index);
   if(store==null){store=new DistrictContainerItems{index=index};Data.containers.Add(store);}
   return store.items;
  }
 }
}
