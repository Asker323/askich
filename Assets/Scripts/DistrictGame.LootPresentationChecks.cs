#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckLootPresentation(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_LOOT_PRESENTATION_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"loot-view-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;playing=true;paused=false;foreach(var d in InputSystem.devices.ToArray())if(d is Keyboard||d is Mouse)InputSystem.DisableDevice(d);
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=stamina=100;heroStagger=attack=0;heroVelocity=Vector2.zero;bleeding=false;infection=0;weapon=0;
   var checks=new List<string>();void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines(Path.Combine(folder,"loot-presentation-checks.txt"),checks);if(!ok){Application.Quit(1);throw new Exception(label);}}
   var h=homes[5];h.open=true;SnapDoor(h);int slot=2;int store=17;hero.p=StorageStand(h,slot);current=h;
   void Reset(){CancelSearch();inventory.Load(new DistrictInventoryData());weapon=0;h.taken[slot]=false;h.loot[slot]="Бинт";hero.p=StorageStand(h,slot);heroVelocity=Vector2.zero;RefreshContainers(h);}
   Reset();yield return new WaitForSeconds(.3f);BeginSearch(h,slot);yield return new WaitForSeconds(.25f);
   Check(!lootPreview&&inventory.Count(DistrictItemKind.Bandage)==0,"early search neither displays a find nor grants it");
   yield return new WaitForSeconds(SearchDuration(h,slot)-.55f);yield return new WaitForEndOfFrame();
   Check(lootPreview&&lootPreview.gameObject.activeSelf&&lootWillFit&&lootLift>.2f,"final search phase displays a bandage before transfer");
   Check(lootPreview.GetComponentsInChildren<Renderer>().Length>=3&&lootPreview.transform.Find("Cloth bandage"),"held find uses the actual bandage model shared with inventory artwork");
   Check(Vector3.Distance(lootPreview.transform.position,hero.visual.StorageHand.position)<.12f,"visible find follows the animated hand");
   Check(!h.taken[slot]&&bandages==0&&inventory.Container(store)[0].Empty,"visible preview cannot grant or duplicate inventory contents");
   yield return CaptureGameCamera(Path.Combine(folder,"bandage-in-hand.png"),2.3f);
   CancelSearch();Check(!lootPreview&&bandages==0&&!h.taken[slot],"cancelling the visible preview grants no item");
   Save();Check(Load()&&!h.taken[slot]&&bandages==0,"cancelled preview remains uncollected after reload");
   current=h;BeginSearch(h,slot);yield return new WaitForSeconds(SearchDuration(h,slot)+.15f);
   Check(h.taken[slot]&&bandages==1&&inventory.Container(store)[0].Empty&&!lootPreview,"completed search transfers exactly one previewed item");
   RevealStorage(h,slot);Check(bandages==1,"repeated reveal does not duplicate the inspected item");
   Save();Check(Load()&&bandages==1&&h.taken[slot]&&!lootPreview,"completed transfer survives reload without a visual ghost");
   Reset();for(int i=0;i<6;i++)inventory.TryAdd(DistrictItemKind.Water,3);weapon=2;BeginSearch(h,slot);yield return new WaitForSeconds(SearchDuration(h,slot)-.35f);
   Check(lootPreview&&!lootWillFit,"full pockets preview the find without claiming capacity");float lifted=lootLift;yield return new WaitForSeconds(.24f);
   Check(lootPreview&&lootLift<lifted&&bandages==0,"full pockets lower the inspected item back before completion");
   yield return new WaitForSeconds(.25f);Check(h.taken[slot]&&bandages==0&&inventory.Container(store)[0].kind==(int)DistrictItemKind.Bandage,"full pockets leave the actual item in its container");
   Save();Check(Load()&&inventory.Container(store)[0].kind==(int)DistrictItemKind.Bandage,"returned item persists after reload");
   current=h;inventory.Data.slots[0]=default;Check(TakeStoredItem(store,0)&&bandages==1&&inventory.Container(store)[0].Empty,"freeing one slot permits a single later transfer");
   Reset();h.loot[slot]="Пусто";BeginSearch(h,slot);yield return new WaitForSeconds(SearchDuration(h,slot)-.2f);Check(!lootPreview,"empty container never displays a fabricated find");yield return new WaitForSeconds(.3f);Check(h.taken[slot]&&bandages==0,"empty search completes without an inventory reward");
   Reset();h.loot[slot]="Еда";BeginSearch(h,slot);yield return new WaitForSeconds(SearchDuration(h,slot)-.25f);Check(lootPreview&&lootPreview.GetComponentsInChildren<Renderer>().Any(r=>r.name=="Pull tab"),"food uses its own tin model rather than a bandage placeholder");CancelSearch();
   Reset();for(int i=0;i<6;i++)inventory.TryAdd(DistrictItemKind.Water,3);Check(CanCarryFound(DistrictItemKind.Axe)&&!CanCarryFound(DistrictItemKind.Bandage),"capacity preview preserves the existing free weapon-hand exception");
   Reset();BeginSearch(h,slot);yield return new WaitForSeconds(SearchDuration(h,slot)-.25f);dead=true;yield return null;yield return null;Check(!lootPreview&&!h.taken[slot]&&bandages==0,"death removes the transient held object without granting unfinished loot");dead=false;
   Application.Quit();
  }
 }
}
#endif
