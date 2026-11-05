#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckRefill(Action<bool,string> check,string folder){
   foreach(var z in infected)z.hp=0;
   inventory.Load(new DistrictInventoryData());inventory.TryAdd(DistrictItemKind.Water,1);
   check(inventory.DrinkWater()&&inventory.Data.slots[0].kind==10&&inventory.Data.slots[0].amount==1,"last sip leaves empty bottle in the same slot");
   inventory.TryAdd(DistrictItemKind.Water,2);inventory.TryAdd(DistrictItemKind.Water,1);
   int a=1,b=2;DistrictInventory.Transfer(inventory.Data.slots,a,inventory.Data.slots,b);
   check(inventory.Data.slots[a].kind==9&&inventory.Data.slots[b].kind==9,"moving partial bottles preserves both physical bottles");
   CancelMedical();health=100;attack=heroStagger=0;bleeding=false;infection=0;heroVelocity=Vector2.zero;hero.p=cistern;inventorySelection=0;
   check(BeginMedical(MedicalAction.Refill),"empty bottle starts filling at reserve");
   TickMedical(1);CancelMedical();check(inventory.Data.slots[0].kind==10,"cancelled refill leaves bottle empty");
   check(BeginMedical(MedicalAction.Refill),"filling can restart");yield return new WaitForSeconds(3.3f);
   check(inventory.Data.slots[0].kind==9&&inventory.Data.slots[0].amount==3,"completed refill fills selected bottle to three sips");
   hero.p=cistern+new Vector2(4,-3);face=Vector2.down;yield return new WaitForSeconds(1);
   hydration=20;inventorySelection=0;check(BeginMedical(MedicalAction.Water),"animated water action starts");
   yield return new WaitForSeconds(.6f);
   check(hero.visual.State=="DrinkProvision"&&heldProvision&&heldProvision.activeSelf,"drinking uses dedicated animation with held bottle");
   yield return CaptureGameCamera(Path.Combine(folder,"provision-drinking.png"));
   CancelMedical();yield return null;
   check(!heldProvision||!heldProvision.activeSelf,"cancelled drinking clears held prop");
   inventory.TryAdd(DistrictItemKind.Food,1);nourishment=20;check(BeginMedical(MedicalAction.Eat),"animated eating starts");
   yield return new WaitForSeconds(.7f);check(hero.visual.State=="EatProvision","food uses dedicated eating pose");CancelMedical();yield return null;
   hero.p=refuge+new Vector2(2,-3);face=Vector2.down;campEstablished=true;tiredness=80;yield return new WaitForSeconds(1);
   check(BeginMedical(MedicalAction.Rest),"animated rest starts");
   yield return new WaitForSeconds(2);check(hero.visual.State=="RestProvision","rest uses crouched recovery pose");
   yield return CaptureGameCamera(Path.Combine(folder,"provision-rest.png"));CancelMedical();
   Save();check(Load()&&inventory.Data.slots[0].amount==3,"refilled bottle survives v7 save load");
  }
  IEnumerator CheckTouchInventory(){
   testSave=Path.Combine(Application.temporaryCachePath,"touch-inventory-"+Guid.NewGuid()+".json");
   string folder=Environment.GetEnvironmentVariable("GOIDA_TOUCH_INVENTORY_CHECK");Directory.CreateDirectory(folder);
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;EnsureInventoryArt();
   var results=new System.Collections.Generic.List<string>();var trace=new System.Collections.Generic.List<string>();
   void Trace(string phase){trace.Add($"{phase}: screen={Screen.width}x{Screen.height}, offset={ScreenLayout.Offset}, scale={ScreenLayout.Scale}, held={pointer.Held}, pos={pointer.Position}, origin={pointer.Origin}, dragged={pointer.Dragged}, slot={dragSlot}, open={suppliesOpen}");File.WriteAllLines(Path.Combine(folder,"touch-trace.txt"),trace);}
   void Require(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"touch-inventory-checks.txt"),results);if(!ok){var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"failed-touch.png"),frame.EncodeToPNG());Destroy(frame);Application.Quit(1);throw new Exception(message);}}
   playing=true;paused=false;Application.runInBackground=true;
   foreach(var z in infected)z.hp=0;inventory.Load(new DistrictInventoryData());inventory.TryAdd(DistrictItemKind.Water,3);inventory.TryAdd(DistrictItemKind.Backpack,1);inventory.Data.bagFound=true;
   hero.p=homes[5].p+homes[5].containers[1]+Vector2.down;homes[5].taken[1]=true;suppliesOpen=true;pointer.Reset();
   var screen=InputSystem.AddDevice<Touchscreen>();TouchControls.ForceEnabled=true;
   void Finger(Vector2 ui,UnityEngine.InputSystem.TouchPhase phase){var pos=ScreenLayout.Offset+ui*ScreenLayout.Scale;pos.y=Screen.height-pos.y;InputSystem.QueueStateEvent(screen,new TouchState{touchId=21,phase=phase,position=pos,pressure=phase==UnityEngine.InputSystem.TouchPhase.Ended?0:1});}
   IEnumerator Drag(Vector2 from,Vector2 to){
    Finger(from,UnityEngine.InputSystem.TouchPhase.Began);yield return new WaitForSeconds(.15f);Trace("sample");
    Finger(Vector2.Lerp(from,to,.5f),UnityEngine.InputSystem.TouchPhase.Moved);yield return new WaitForSeconds(.15f);Trace("sample");
    Finger(to,UnityEngine.InputSystem.TouchPhase.Moved);yield return new WaitForSeconds(.15f);Trace("sample");
    Finger(to,UnityEngine.InputSystem.TouchPhase.Ended);yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();
   }
   yield return new WaitForSeconds(.5f);
   yield return Drag(ItemCell(1).center,BagSlot.center);Require(inventory.Data.backpack&&inventory.Capacity==18,"touch equips backpack");
   yield return Drag(ItemCell(0).center,ItemCell(7).center);Require(inventory.Data.slots[7].kind==9&&inventory.Data.slots[7].amount==3,"touch transfers bottle to backpack row");
   yield return Drag(ItemCell(7).center,StoreCell(2).center);Require(inventory.Container(16)[2].kind==9,"touch deposits bottle in searched container");
   yield return Drag(StoreCell(2).center,ItemCell(8).center);Require(inventory.Data.slots[8].kind==9&&inventory.Container(16)[2].Empty,"touch retrieves bottle without duplication");
   for(int i=0;i<3;i++){
    var kind=(DistrictItemKind)(11+i);inventory.TryAdd(kind,1);
    int slot=Array.FindIndex(inventory.Data.slots,v=>v.kind==(int)kind);
    yield return Drag(ItemCell(slot).center,ClothingSlot(i).center);
    Require(inventory.HasClothing((int)kind)&&inventory.Count(kind)==0,"touch equips clothing slot "+i+" without duplication");
   }
   var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"touch-inventory.png"),image.EncodeToPNG());Destroy(image);
   TouchControls.ForceEnabled=false;InputSystem.RemoveDevice(screen);Application.Quit();
  }
 }
}
#endif
