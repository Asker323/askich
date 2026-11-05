#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator PreviewMedicalUI(){
   testSave=Path.Combine(Application.temporaryCachePath,"medical-preview-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;playing=true;paused=false;
   foreach(var enemy in infected){enemy.hp=0;enemy.visual.Die();}
   hero.p=homes[6].p;health=68;bleeding=true;infection=26;
   bandages=2;sterileBandages=1;alcoholDoses=2;inventory.TryAdd(DistrictItemKind.Backpack,1);inventory.Data.bagFound=true;hero.p=homes[6].p+homes[6].containers[0]+Vector2.down;homes[6].taken[0]=true;suppliesOpen=true;
   bleeding=false;infection=0;nourishment=35;hydration=25;tiredness=70;inventory.TryAdd(DistrictItemKind.Food,1);inventory.TryAdd(DistrictItemKind.Water,3);
   yield return null;
  }
  IEnumerator CheckMedicalUI(Action<bool,string> check,string folder){
   InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   foreach(var enemy in infected)enemy.hp=0;
   hero.p=homes[6].p;heroVelocity=Vector2.zero;attack=heroStagger=0;CancelMedical();
   health=65;bleeding=true;infection=20;drunkSeconds=0;
   bandages=2;sterileBandages=0;alcoholDoses=3;suppliesOpen=true;pointer.Reset();
   var nativeMice=new System.Collections.Generic.List<Mouse>();
   foreach(var device in InputSystem.devices)if(device is Mouse native&&native.enabled)nativeMice.Add(native);
   foreach(var native in nativeMice)InputSystem.DisableDevice(native);
   var mouse=InputSystem.AddDevice<Mouse>();
   string trace=Path.Combine(folder,"medical-ui-trace.txt");File.WriteAllText(trace,"");
   void Trace(string phase){File.AppendAllText(trace,$"{phase}: focus={Application.isFocused}; held={pointer.Held}; dragged={pointer.Dragged}; pos={pointer.Position}; origin={pointer.Origin}; inventory={suppliesOpen}; selected={inventorySelection}; action={medicalAction}; unavailable={MedicalUnavailable(MedicalAction.Craft)}\n");}
   IEnumerator Tap(Rect area){
    Vector2 p=ScreenLayout.Offset+area.center*ScreenLayout.Scale;p.y=Screen.height-p.y;
    mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));
    yield return null;yield return new WaitForEndOfFrame();Trace("press rendered");
    mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=p});
    yield return null;yield return new WaitForEndOfFrame();Trace("release rendered");yield return null;
   }
   IEnumerator Invoke(Rect area,MedicalAction action=MedicalAction.None){
    if(!Application.isBatchMode&&area!=CancelTreatment){
     DistrictItemKind kind=action==MedicalAction.Sterile?DistrictItemKind.Sterile:action==MedicalAction.Drink?DistrictItemKind.Alcohol:DistrictItemKind.Bandage;
     inventorySelection=System.Array.FindIndex(inventory.Data.slots,i=>i.kind==(int)kind);
     area=action==MedicalAction.Craft?new Rect(905,402,245,56):action==MedicalAction.Drink?new Rect(905,470,245,56):new Rect(905,334,245,56);
    }
    if(Application.isBatchMode){
     if(area==CancelTreatment)CancelMedical();
     else if(MedicalUnavailable(action)==null)BeginMedical(action);
     yield return null;
    }else yield return Tap(area);
   }
   check(MedicalUnavailable(MedicalAction.Sterile)=="Нет стерильных бинтов"&&TreatmentAdvice().Contains("Стерилизуй"),"wounded inventory explains missing dressing and available recipe");
   yield return Invoke(SterileAction,MedicalAction.Sterile);
   check(suppliesOpen&&medicalAction==MedicalAction.None&&bandages==2&&alcoholDoses==3&&attack<=0,"disabled inventory button cannot spend supplies or trigger world melee");
   yield return Invoke(CraftAction,MedicalAction.Craft);
   check(medicalAction==MedicalAction.Craft&&!suppliesOpen,"craft action enters live timed crafting");
   check(PointerOnHud(CancelTreatment.center),"medical progress buttons exclude world attacks");
   yield return Invoke(CancelTreatment);
   check(medicalAction==MedicalAction.None&&bandages==2&&alcoholDoses==3,"cancel button interrupts craft and preserves its materials");

   suppliesOpen=true;yield return Invoke(CraftAction,MedicalAction.Craft);
   yield return new WaitForSeconds(3.2f);
   check(sterileBandages==1&&bandages==1&&alcoholDoses==2,"inventory craft completes with one bandage and one dose spent");
   check(QuickDressing()==MedicalAction.Sterile,"touch quick dressing chooses sterile bandage for infected wound");
   suppliesOpen=true;yield return Invoke(SterileAction,MedicalAction.Sterile);yield return new WaitForSeconds(2.7f);
   check(!bleeding&&infection==0&&sterileBandages==0,"inventory treatment button stops bleeding and treats wound through Update");
   suppliesOpen=true;yield return Invoke(DrinkAction,MedicalAction.Drink);yield return new WaitForSeconds(1.4f);
   check(drunkSeconds>40&&alcoholDoses==1,"separate drinking action spends one dose and causes intoxication");
   bandages=1;sterileBandages=1;bleeding=true;infection=0;
   check(QuickDressing()==MedicalAction.Bandage,"quick dressing uses ordinary bandage for uncomplicated bleeding");
   bandages=0;
   check(QuickDressing()==MedicalAction.Sterile,"quick dressing falls back to available sterile bandage");
   alcoholDoses=5;check(AlcoholSummary().Contains("1")&&AlcoholSummary().Contains("2"),"alcohol stock explains full bottle and remaining doses");
   suppliesOpen=false;InputSystem.RemoveDevice(mouse);foreach(var native in nativeMice)InputSystem.EnableDevice(native);
   File.WriteAllText(Path.Combine(folder,"medical-ui-note.txt"),Application.isBatchMode?"Headless run checks medical commands and states. Graphical button clicks need a windowed run.":"Graphical inventory buttons tested through Input System mouse events. Phone testing remains pending.");
  }
 }
}
#endif
