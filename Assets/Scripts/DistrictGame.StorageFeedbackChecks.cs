#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckStorageFeedback(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_STORAGE_FEEDBACK_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"storage-feedback-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;playing=true;paused=false;
   foreach(var d in InputSystem.devices.ToArray())if(d is Keyboard||d is Mouse)InputSystem.DisableDevice(d);
   var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=stamina=100;heroStagger=attack=0;bleeding=false;infection=0;weapon=1;zoom=5;
   var results=new List<string>();void Check(bool ok,string text){results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines(Path.Combine(folder,"storage-feedback-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(text);}}
   var h=homes[5];h.open=true;SnapDoor(h);hero.p=h.p+h.containers[1]+Vector2.down;current=h;heroVelocity=Vector2.zero;h.taken[1]=false;RefreshContainers(h);
   yield return new WaitForSeconds(.5f);BeginSearch(h,1);
   Check(containerAudio.PulseCount==1&&containerAudio.LastClip.StartsWith("Wood scrape"),"wood search starts one material-specific audible event");
   yield return new WaitForSeconds(.45f);
   Check(searchSlot==1&&!h.taken[1]&&h.drawers[1]&&h.lids[1].position.z<h.storageRest[1].z-.3f&&Quaternion.Angle(h.lids[1].rotation,Quaternion.identity)<1,"kitchen drawer slides horizontally while loot remains undiscovered");
   Check(hero.visual.State=="Search"&&hero.visual.EquippedWeapon==0,"survivor searches with free hands rather than waving the equipped axe");
   Check(containerAudio.PulseCount==1,"rummage includes a quiet gap instead of an endless audio loop");
   yield return new WaitForSeconds(.4f);
   Check(containerAudio.PulseCount==2&&containerAudio.LastClip.EndsWith("1"),"next timed search beat selects another sound variation");
   Check(Mathf.Abs(hero.visual.ArtPhase("Search")-searchTime/1.5f)<.06f,"hand animation follows the same elapsed search time as sound beats");
   yield return new WaitForEndOfFrame();var picture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"drawer-search.png"),picture.EncodeToPNG());Destroy(picture);
   Check(PointerOnHud(SearchCancelArea.center),"cancel button is excluded from world attack input");
   var pos=ScreenLayout.Offset+SearchCancelArea.center*ScreenLayout.Scale;pos.y=Screen.height-pos.y;
   InputSystem.QueueStateEvent(mouse,new MouseState{position=pos}.WithButton(MouseButton.Left));yield return null;yield return new WaitForEndOfFrame();InputSystem.QueueStateEvent(mouse,new MouseState{position=pos});yield return null;yield return new WaitForEndOfFrame();
   Check(searchSlot<0&&!h.taken[1]&&!containerAudio.Searching&&!containerAudio.Audible&&attack<=0,"actual cancel click stops sound without looting or attacking");
   yield return new WaitForSeconds(.4f);Check(Vector3.Distance(h.lids[1].position,h.storageRest[1])<.01f,"cancelled drawer closes smoothly");
   BeginSearch(h,1);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   Check(paused&&searchSlot<0&&!containerAudio.Searching&&!containerAudio.Audible,"keyboard pause cancels search and does not leave hidden rummage playing");
   paused=false;yield return null;BeginSearch(h,1);suppliesOpen=true;yield return null;yield return null;
   Check(searchSlot<0&&!h.taken[1]&&!containerAudio.Searching,"inventory interrupts search without discovering the contents");suppliesOpen=false;
   BeginSearch(h,1);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));yield return new WaitForSeconds(.15f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.2f);
   Check(searchSlot<0&&!containerAudio.Searching&&!h.taken[1],"walking away interrupts search immediately");
   hero.p=h.p+h.containers[0]+Vector2.down;heroVelocity=Vector2.zero;h.taken[0]=false;BeginSearch(h,0);yield return new WaitForSeconds(.4f);
   Check(containerAudio.LastClip.StartsWith("Metal rattle")&&!h.drawers[0]&&Quaternion.Angle(h.lids[0].rotation,Quaternion.identity)>40,"tool chest keeps a hinged lid and a distinct metal sound");
   CancelSearch();hero.p=h.p+h.containers[2]+Vector2.down;heroVelocity=Vector2.zero;h.taken[2]=false;BeginSearch(h,2);yield return new WaitForSeconds(.2f);
   Check(containerAudio.LastClip.StartsWith("Cloth rustle"),"medical storage uses soft rustling rather than a metal rattle");CancelSearch();
   hero.p=h.p+h.containers[1]+Vector2.down;heroVelocity=Vector2.zero;BeginSearch(h,1);yield return new WaitForSeconds(SearchDuration(h,1)+.2f);
   Check(h.taken[1]&&searchSlot<0&&!containerAudio.Searching&&h.lids[1].position.z<h.storageRest[1].z-.4f,"completed search leaves an open drawer with no ongoing noise");
   Save();h.taken[1]=false;RefreshContainers(h);Check(Load()&&h.taken[1]&&h.lids[1].position.z<h.storageRest[1].z-.4f,"existing save data restores the new open drawer appearance");
   InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);Application.Quit();
  }
 }
}
#endif
