#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckScavenge(){
   string folder=Environment.GetEnvironmentVariable("BEREZKI_SCAVENGE_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"scavenge-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;playing=true;paused=false;foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}
   var results=new System.Collections.Generic.List<string>();
   void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"scavenge-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   var h=homes[5];var clinic=homes[6];
   Check(homes.Count(v=>v.loot.Any(x=>x=="Топор"||x=="Копьё"))==3,"only three of 22 houses contain weapons");
   Check(homes.Where(v=>v!=clinic).All(v=>v.loot[1]=="Пусто"||Edible((int)StorageKind(v,1))),"kitchen drawers never contain weapons or bandages");
   Check(clinic.loot.All(x=>x=="Бинт"||x=="Пусто"),"clinic storage contains medical supplies");
   hero.p=h.p+h.containers[0]+Vector2.down;current=h;inventory.Load(new DistrictInventoryData());for(int i=0;i<6;i++)inventory.TryAdd(DistrictItemKind.Water,3);h.taken[0]=false;
   Loot(h,0);Check(h.taken[0]&&weapon==1,"unarmed survivor can carry found tool directly in hand");
   weapon=2;h.taken[0]=false;Loot(h,0);Check(h.taken[0]&&inventory.Container(15)[0].kind==1,"full pockets leave discovered axe in actual storage");
   Loot(h,0);Check(inventory.Container(15).Count(v=>v.kind==1)==1,"repeated interaction cannot duplicate discovered loot");
   Save();inventory.Container(15)[0]=default;Check(Load()&&inventory.Container(15)[0].kind==1,"uncollected loot persists with searched state");current=h;
   Check(!TakeStoredItem(15,0)&&inventory.Container(15)[0].kind==1,"failed transfer does not consume storage item");
   inventory.Data.slots[0]=default;Check(TakeStoredItem(15,0)&&inventory.Container(15)[0].Empty&&inventory.Count(DistrictItemKind.Axe)==1,"freeing pocket permits one atomic transfer");
   Save();Check(Load()&&inventory.Container(15)[0].Empty&&inventory.Count(DistrictItemKind.Axe)==1,"taken item cannot respawn after reload");
   current=h;hero.p=h.p+h.containers[1]+Vector2.down;h.taken[1]=false;BeginSearch(h,1);yield return new WaitForSeconds(.35f);Use();Check(searchSlot<0&&!h.taken[1]&&!containerAudio.Searching,"E cancels search without revealing items and stops sound immediately");
   var witness=infected[0];witness.hp=witness.maxHp;witness.p=hero.p+Vector2.down*8;witness.stagger=100;witness.alert=0;BeginSearch(h,1);witness.p=h.p+h.containers[1]+Vector2.down*4;witness.alert=0;TickSearchNoise(1.1f);Check(witness.alert>0,"ongoing search can attract an enemy arriving after the initial noise");CancelSearch();witness.hp=0;
   inventory.Load(new DistrictInventoryData());hero.p=clinic.p+clinic.containers[1]+Vector2.down;current=clinic;clinic.taken[1]=false;BeginSearch(clinic,1);yield return new WaitForSeconds(1.6f);Check(clinic.taken[1]&&bandages==1,"quiet medical search completes through real Update");
   hero.p=h.p+new Vector2(-1,0);zoom=6;yield return new WaitForSeconds(.6f);yield return CaptureGameCamera(Path.Combine(folder,"scavenge-home.png"));
   hero.p=clinic.p+new Vector2(-1,0);yield return new WaitForSeconds(.6f);yield return CaptureGameCamera(Path.Combine(folder,"scavenge-clinic.png"));
   hero.p=h.p+h.containers[1]+Vector2.down;current=h;h.taken[1]=true;inventory.Container(16)[0]=new DistrictItem(DistrictItemKind.Food,1);suppliesOpen=true;yield return new WaitForSeconds(.5f);
   var nativeMice=new System.Collections.Generic.List<Mouse>();foreach(var device in InputSystem.devices)if(device is Mouse native&&native.enabled)nativeMice.Add(native);foreach(var native in nativeMice)InputSystem.DisableDevice(native);
   var mouse=InputSystem.AddDevice<Mouse>();var pos=ScreenLayout.Offset+StoreCell(0).center*ScreenLayout.Scale;pos.y=Screen.height-pos.y;mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=pos}.WithButton(MouseButton.Left));yield return null;yield return new WaitForEndOfFrame();InputSystem.QueueStateEvent(mouse,new MouseState{position=pos});yield return null;yield return new WaitForEndOfFrame();yield return null;
   Check(inventorySelection==-10,"clicking storage icon selects the real stored item");
   var button=ScreenLayout.Offset+new Vector2(1027,362)*ScreenLayout.Scale;button.y=Screen.height-button.y;InputSystem.QueueStateEvent(mouse,new MouseState{position=button}.WithButton(MouseButton.Left));yield return null;yield return new WaitForEndOfFrame();InputSystem.QueueStateEvent(mouse,new MouseState{position=button});yield return null;yield return new WaitForEndOfFrame();yield return null;
   Check(food==1&&inventory.Container(16)[0].Empty,"Take button transfers selected item through graphical input");
   inventory.Container(16)[1]=new DistrictItem(DistrictItemKind.Bandage,1);yield return new WaitForEndOfFrame();var picture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"scavenge-inventory.png"),picture.EncodeToPNG());Destroy(picture);
   button=ScreenLayout.Offset+new Vector2(1027,555)*ScreenLayout.Scale;button.y=Screen.height-button.y;InputSystem.QueueStateEvent(mouse,new MouseState{position=button}.WithButton(MouseButton.Left));yield return null;yield return new WaitForEndOfFrame();InputSystem.QueueStateEvent(mouse,new MouseState{position=button});yield return null;yield return new WaitForEndOfFrame();yield return null;
   Check(bandages==2&&inventory.Container(16)[1].Empty,"Take all button merges stored supplies without replacing pockets");
   hero.p=h.p;inventory.Container(16)[0]=new DistrictItem(DistrictItemKind.Food,1);Check(!TakeStoredItem(16,0)&&!inventory.Container(16)[0].Empty,"walking away prevents remote storage transfer");
   InputSystem.RemoveDevice(mouse);foreach(var native in nativeMice)InputSystem.EnableDevice(native);Application.Quit();
  }
 }
}
#endif
