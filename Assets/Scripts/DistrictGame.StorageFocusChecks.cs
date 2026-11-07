#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckStorageFocus(){
   string folder=Environment.GetEnvironmentVariable("BEREZKI_STORAGE_FOCUS_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"storage-focus-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;playing=true;paused=false;
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}
   var nativeMice=new List<Mouse>();foreach(var device in InputSystem.devices)if(device is Mouse native&&native.enabled)nativeMice.Add(native);foreach(var native in nativeMice)InputSystem.DisableDevice(native);
   var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();
   var results=new List<string>();
   void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"storage-focus-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   var h=homes[5];h.open=true;SnapDoor(h);hero.p=h.p+new Vector2(.8f,1.9f);zoom=7;weapon=1;noticeTime=0;
   void Point(int slot,bool left=false,bool aim=false){Vector2 p=cam.WorldToScreenPoint(h.storageArt[slot][0].bounds.center);var state=new MouseState{position=p};if(left)state=state.WithButton(MouseButton.Left);if(aim)state=state.WithButton(MouseButton.Right);mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,state);}
   IEnumerator Click(int slot,bool aim=false){Point(slot,true,aim);yield return null;yield return null;Point(slot,false,aim);yield return null;yield return null;}
   IEnumerator TapUI(Vector2 point){var p=ScreenLayout.Offset+point*ScreenLayout.Scale;p.y=Screen.height-p.y;InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;yield return new WaitForEndOfFrame();InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return null;yield return new WaitForEndOfFrame();yield return null;}
   yield return new WaitForSeconds(.8f);Point(1);yield return null;yield return null;
   Check(NearbyContainer(h)==2&&hoveredStorage==1&&FocusedStorageSlot()==1,"mouse can select the farther of two reachable storage boxes");
   Check(storageCue&&storageCue.enabled,"reachable selected storage has a visible world outline");
   int sequence=strikeSequence;Point(1,true);yield return null;yield return null;
   Check(searchSlot==1&&attack<=0&&strikeSequence==sequence,"left click starts selected search through Update without striking");
   yield return new WaitForSeconds(.25f);
   Check(searchSlot==1&&searchTime>.2f&&!h.taken[1],"holding left mouse does not repeatedly restart or cancel search");Point(1);yield return null;
   yield return new WaitForEndOfFrame();var picture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"storage-focus-search.png"),picture.EncodeToPNG());Destroy(picture);
   yield return Click(1);Check(searchSlot<0&&!h.taken[1]&&!containerAudio.Searching&&attack<=0,"second click cancels search and sound without taking loot or attacking");
   Point(1,false,true);yield return null;yield return null;Check(pcAiming&&!storageCue.enabled,"aiming hides storage outline and reserves combat controls");
   yield return Click(1,true);Check(attack>0&&strikeSequence==sequence+1&&searchSlot<0,"aim plus left click attacks even when cursor is over storage");Point(1);yield return new WaitForSeconds(1.1f);
   paused=true;yield return null;yield return null;Check(!storageCue.enabled&&hoveredStorage<0,"pause hides outline and clears stale storage focus");paused=false;Point(1);yield return null;yield return null;
   h.taken[1]=true;h.taken[2]=true;inventory.Container(16)[0]=new DistrictItem(DistrictItemKind.Food,1);inventory.Container(17)[0]=new DistrictItem(DistrictItemKind.Bandage,1);
   yield return Click(1);Check(suppliesOpen&&NearbyStore()==16&&openedStorage==16&&attack<=0,"clicked searched box opens its own inventory instead of nearer neighbour");
   Point(2);yield return null;yield return null;Check(NearbyStore()==16&&!storageCue.enabled,"moving cursor cannot switch the open storage inventory");
   yield return TapUI(StoreCell(0).center);Check(inventorySelection==-10,"selected container item can be selected through graphical input");
   yield return new WaitForEndOfFrame();picture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"storage-focus-inventory.png"),picture.EncodeToPNG());Destroy(picture);
   yield return TapUI(new Vector2(1027,362));Check(food==1&&inventory.Container(16)[0].Empty&&inventory.Container(17)[0].kind==(int)DistrictItemKind.Bandage,"Take transfers from clicked box and preserves neighbouring contents");
   hero.p=h.p;yield return null;yield return null;Check(NearbyStore()==-1,"leaving selected container reach cannot silently switch to another box");
   Save();Check(Load()&&!suppliesOpen&&openedStorage<0&&hoveredStorage<0,"loading clears open-container focus without changing save format");
   hero.p=h.p+new Vector2(.8f,1.9f);h.taken[2]=false;yield return new WaitForSeconds(.6f);
   mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width-30,Screen.height/2)});yield return null;yield return null;
   Check(hoveredStorage<0&&FocusedStorageSlot()==2&&storageCue.enabled,"keyboard fallback highlights nearest reachable container when mouse is elsewhere");
   keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   Check(searchSlot==2,"E still starts highlighted nearest-container search");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));yield return new WaitForSeconds(.15f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   Check(searchSlot<0&&!h.taken[2],"movement interrupts mouse or keyboard search without revealing contents");
   heroVelocity=Vector2.zero;hero.p=h.p+Vector2.down*5;yield return new WaitForSeconds(.4f);Point(1);yield return null;yield return null;
   Check(current==null&&hoveredStorage<0&&!storageCue.enabled,"outside house cannot target or highlight storage through walls");
   InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);foreach(var native in nativeMice)InputSystem.EnableDevice(native);Application.Quit();
  }
 }
}
#endif
