using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  readonly Vector2[] clothingPositions={new Vector2(-13.5f,-13.05f),new Vector2(-38.2f,37.2f),new Vector2(95,7.2f)};
  readonly GameObject[] clothingModels=new GameObject[3];
  static Rect ClothingSlot(int index)=>new Rect(322,193+index*81,60,60);
  int ClothingTarget(Vector2 p){for(int i=0;i<3;i++)if(ClothingSlot(i).Contains(p))return 11+i;return -1;}
  void BuildClothingFinds(){for(int i=0;i<3;i++){var obj=new GameObject(ItemNames[11+i]+" to find");obj.transform.position=V(clothingPositions[i],i==2?.14f:.25f);MakeClothingIcon(obj.transform,11+i);if(i<2)obj.transform.rotation=Quaternion.Euler(90,0,i==0?15:-12);clothingModels[i]=obj;}}
  void MakeClothingIcon(Transform root,int kind){
   var color=kind==11?new Color(.31f,.37f,.32f):kind==12?new Color(.26f,.31f,.35f):new Color(.25f,.23f,.20f);
   if(kind==11){Cube("Shirt body",Vector3.zero,new Vector3(.38f,.48f,.08f),color,root);for(int side=-1;side<=1;side+=2)Cube("Short sleeve",new Vector3(side*.24f,.12f,0),new Vector3(.18f,.2f,.08f),color,root).transform.localRotation=Quaternion.Euler(0,0,-side*20);}
   else if(kind==12){for(int side=-1;side<=1;side+=2)Cube("Trouser leg",new Vector3(side*.1f,-.1f,0),new Vector3(.17f,.56f,.09f),color,root);Cube("Waistband",new Vector3(0,.2f,0),new Vector3(.38f,.12f,.1f),color,root);}
   else for(int side=-1;side<=1;side+=2){Cube("Trainer",new Vector3(side*.14f,0,0),new Vector3(.2f,.15f,.4f),color,root);Cube("Sole",new Vector3(side*.14f,-.08f,0),new Vector3(.21f,.04f,.41f),new Color(.49f,.48f,.43f),root);}
  }
  int NearbyClothing(){for(int i=0;i<3;i++)if((inventory.Data.clothingFound&(1<<i))==0&&Vector2.Distance(hero.p,clothingPositions[i])<1.05f&&Sight(hero.p,clothingPositions[i]))return i;return -1;}
  bool UseClothingFind(){int i=NearbyClothing();if(i<0)return false;if(!inventory.TryAdd((DistrictItemKind)(11+i),1)){Say("Освободи ячейку для одежды.");return true;}inventory.Data.clothingFound|=1<<i;clothingModels[i].SetActive(false);Save();Say("Найдено: "+ItemNames[11+i]);return true;}
  void UpdateClothing(){if(hero?.visual)hero.visual.ApplyClothing(inventory.Data);for(int i=0;i<3;i++)if(clothingModels[i])clothingModels[i].SetActive((inventory.Data.clothingFound&(1<<i))==0);}
  void DrawClothingHint(){int i=NearbyClothing();if(i>=0)GUI.Label(new Rect(35,235,720,30),ItemNames[11+i]+(TouchControls.Enabled?" · действие":" · E"),small);}
  void DrawClothingSlots(){
   string[] labels={"Тело","Ноги","Обувь"};
   for(int i=0;i<3;i++){
    InventoryCell(ClothingSlot(i),inventory.HasClothing(11+i)?new DistrictItem((DistrictItemKind)(11+i),1):default,-21-i,false);
    GUI.Label(new Rect(322,ClothingSlot(i).yMax+2,70,22),labels[i],small);
    if(Event.current.type==EventType.Repaint&&pointer.TakeClick(ClothingSlot(i)))inventorySelection=-21-i;
   }
  }
  void DrawClothingSelection(){
   if(inventorySelection>-21||inventorySelection<-23)return;int kind=11+(-inventorySelection-21);
   if(!inventory.HasClothing(kind))return;GUI.Label(new Rect(905,190,245,64),ItemNames[kind],text);
   if(Button(new Rect(905,334,245,56),"Снять")&&medicalAction==MedicalAction.None){if(inventory.UnequipClothing(kind)){inventorySelection=-1;UpdateClothing();Save();}else InventoryFeedback("Нет свободной ячейки.");}
  }
 }
}
