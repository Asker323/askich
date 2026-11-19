using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  readonly Dictionary<int,DistrictLootPresentation> lootViews=new Dictionary<int,DistrictLootPresentation>();
  DistrictLootPresentation lootPreview;float lootLift;bool lootWillFit;
  bool CanCarryFound(DistrictItemKind kind){
   if((kind==DistrictItemKind.Axe||kind==DistrictItemKind.Spear)&&weapon==0)return true;
   for(int i=0;i<inventory.Capacity;i++){var slot=inventory.Data.slots[i];if(slot.Empty||kind!=DistrictItemKind.Water&&slot.kind==(int)kind&&slot.amount<DistrictInventory.Limit(slot.kind))return true;}
   return false;
  }
  void ClearLootPreview(){if(lootPreview)lootPreview.gameObject.SetActive(false);lootPreview=null;lootLift=0;if(hero!=null&&hero.visual)hero.visual.SetStorageLift(0);}
  void TickLootPreview(Home h,int slot){
   var kind=StorageKind(h,slot);float duration=SearchDuration(h,slot);float window=Mathf.Min(.7f,duration*.5f);float phase=Mathf.Clamp01((searchTime-(duration-window))/window);
   if(kind==DistrictItemKind.Empty||h.taken[slot]||phase<=0){ClearLootPreview();return;}
   if(!lootPreview){
    if(!lootViews.TryGetValue((int)kind,out lootPreview)){
     var model=MakeItemVisual((int)kind,null);model.name="Inspected find: "+ItemNames[(int)kind];model.transform.localScale=Vector3.one*(kind==DistrictItemKind.Axe||kind==DistrictItemKind.Spear?.8f:.5f);
     lootPreview=model.AddComponent<DistrictLootPresentation>();lootViews[(int)kind]=lootPreview;
    }
    lootPreview.Bind(hero.visual.StorageHand,hero.root);lootPreview.gameObject.SetActive(true);
   }
   lootWillFit=CanCarryFound(kind);
   lootLift=lootWillFit?Mathf.SmoothStep(0,1,Mathf.Min(1,phase*1.5f)):Mathf.Sin(phase*Mathf.PI);
   hero.visual.SetStorageLift(lootLift);
  }
 }
}
