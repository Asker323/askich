using System;
using System.IO;
using System.Collections;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  GroundSound SurfaceAt(Vector2 position){
   for(int i=0;i<homes.Count;i++)if(Mathf.Abs(position.x-homes[i].p.x)<3.85f&&Mathf.Abs(position.y-homes[i].p.y)<3.85f)return i==6?GroundSound.Tile:GroundSound.Wood;
   for(int i=-24;i<=24;i+=24)if(Mathf.Abs(position.y-i)<3.5f||(position.x<60&&Mathf.Abs(position.x-i)<3.5f))return GroundSound.Asphalt;
   if(new Rect(91,31,20,16).Contains(position))return GroundSound.Asphalt;
   return GroundSound.Grass;
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  RenderTexture diagnosticView;
  void PrepareDiagnosticView(){
   if(!Application.isBatchMode)return;
   // Give the camera a live render target so skinned animations are evaluated off-screen.
   diagnosticView=new RenderTexture(1280,720,24);
   cam.targetTexture=diagnosticView;
  }
  IEnumerator HoldDiagnosticKey(UnityEngine.InputSystem.Keyboard device,UnityEngine.InputSystem.Key key,float seconds){
   for(float elapsed=0;elapsed<seconds;elapsed+=Mathf.Min(Time.deltaTime,.05f)){
    device.MakeCurrent();
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    yield return null;
   }
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.KeyboardState());
  }
  void ReleaseDiagnosticView(){
   if(!diagnosticView)return;
   if(cam)cam.targetTexture=null;
   diagnosticView.Release();
   Destroy(diagnosticView);
  }
  void CheckFoley(Action<bool,string> check,string folder){
   check(SurfaceAt(homes[5].p)==GroundSound.Wood,"radio house selects wooden footsteps");
   check(SurfaceAt(homes[6].p)==GroundSound.Tile,"clinic selects tiled footsteps");
   check(SurfaceAt(new Vector2(0,-24))==GroundSound.Asphalt&&SurfaceAt(new Vector2(-52,-52))==GroundSound.Grass,"road and verge select distinct surfaces");
   var yardPatrol=infected.Find(z=>z.id==8);
   check(yardPatrol!=null&&yardPatrol.spawn.y>homes[5].p.y+4,"crawler starts outside the radio house in new attempts");
   check(foley.StepClipCount==12,"three footstep variants exist for each of four surfaces");
   foley.StartTask(true);check(foley.TaskActive,"crafting starts continuous sound feedback");CancelMedical();check(!foley.TaskActive,"interruption stops medical sound feedback");
   foley.ExportDemo(Path.Combine(folder,"foley-demo.wav"));
  }
#endif
 }
}
