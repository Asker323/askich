#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckWindowArt(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_WINDOW_ART_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"zonbee-window-art-"+Guid.NewGuid()+".json");Application.runInBackground=true;playing=true;paused=true;
   foreach(var device in UnityEngine.InputSystem.InputSystem.devices.ToArray())if(device is UnityEngine.InputSystem.Keyboard||device is UnityEngine.InputSystem.Mouse)UnityEngine.InputSystem.InputSystem.DisableDevice(device);
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=stamina=100;bleeding=false;infection=0;heroStagger=attack=0;RestoreFortifications(0,0);progression.Data.minute=900;TickWorldLife(0);
   var results=new List<string>();void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"window-art-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   var w=houseWindows[1];hero.p=w.home.p+new Vector2(1.5f,.4f);hero.root.position=V(hero.p);hero.root.gameObject.SetActive(false);w.home.roof.SetActive(false);foreach(var cut in w.home.cut)cut.SetActive(false);
   cam.orthographicSize=2.05f;cam.transform.rotation=Quaternion.Euler(20,65,0);cam.transform.position=V(w.p,1.25f)-cam.transform.forward*16;
   Check(w.art.GlassShaderReady,"dedicated transparent glass shader survives Windows build");Check(w.glass.GetComponent<MeshFilter>().sharedMesh.vertexCount==4,"glass is a thin surface rather than an opaque cube");Check(w.art.GetComponentsInChildren<Transform>().Count(t=>t.name=="Gathered window curtain")==2,"window has two gathered cloth curtains");
   yield return CaptureGameCamera(Path.Combine(folder,"window-closed.png"));w.state=1;w.angle=95;RefreshWindow(w);Check(w.leaf.gameObject.activeSelf&&w.glass.activeSelf&&!w.art.Fragments.activeSelf,"opening rotates the sash and preserves intact glass");yield return CaptureGameCamera(Path.Combine(folder,"window-open.png"));
   w.state=0;w.angle=0;RefreshWindow(w);w.state=2;RefreshWindow(w);Check(!w.glass.activeSelf&&w.leaf.gameObject.activeSelf&&w.art.Fragments.activeSelf,"break removes only glass and retains sash, casing and jagged edges");Check(w.art.BurstCount==1,"one impact emits one shard burst");RefreshWindow(w);Check(w.art.BurstCount==1,"repeated refresh does not retrigger the shatter");
   w.art.SetState(0,0,false);w.art.SetState(2,0,false);Check(w.art.BurstCount==1,"restoring broken glass does not replay its impact");yield return CaptureGameCamera(Path.Combine(folder,"window-broken.png"));
   Save();RestoreFortifications(0,0);Check(Load()&&houseWindows[1].state==2&&!houseWindows[1].glass.activeSelf&&houseWindows[1].leaf.gameObject.activeSelf,"saved broken window restores glass damage without deleting its frame");paused=true;
   hero.root.gameObject.SetActive(true);Application.Quit();
  }
 }
}
#endif
