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
  IEnumerator CheckEnvironmentArt(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_ENVIRONMENT_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"environment-"+Guid.NewGuid()+".json");playing=true;paused=false;Application.runInBackground=true;
   foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);
   foreach(var enemy in infected){enemy.hp=0;enemy.root.gameObject.SetActive(false);}health=100;bleeding=false;infection=0;hero.p=new Vector2(-12,-20);heroVelocity=Vector2.zero;
   var results=new List<string>();void Check(bool ok,string text){results.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines(Path.Combine(folder,"environment-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(text);}}
   foreach(string asset in new[]{"asphalt_01","plaster_stone_wall_02","plastered_wall_02","wood_floor_deck","brown_mud_leaves_01"}){
    var maps=new[]{"diff","nor_gl","rough"}.Select(k=>Resources.Load<Texture2D>("Survival/Surfaces/"+asset+"_"+k+"_1k")).ToArray();Check(maps.All(t=>t&&t.width==1024&&t.mipmapCount>1),asset+": colour, normal and roughness maps loaded with mipmaps");
   }
   var road=GameObject.Find("Street").GetComponent<Renderer>().sharedMaterial;Check(road.shader.name=="Zonbee/ScannedSurface"&&road.shader.isSupported&&road.mainTexture,"main road uses scanned material rather than a flat colour");
   Check(GameObject.Find("House floor").GetComponent<Renderer>().sharedMaterial.shader.name=="Zonbee/ScannedSurface","interior floor uses scanned wood");
   Check(environmentArtMeshes.Count==1&&environmentArtMeshes[0].vertexCount<2000,"kerbstones share one compact mesh");
   Check(GameObject.Find("Road marking").GetComponent<Renderer>().sharedMaterial.shader.isSupported,"worn road paint shader supported");
   Check(FindObjectsByType<DistrictWindowArt>(FindObjectsSortMode.None).Count(w=>w.name=="Fixed glazed facade window")==4,"starting homes have four glazed facade openings");
   foreach(int id in new[]{5,6})foreach(int side in new[]{-1,1})Check(Blocked(homes[id].p+new Vector2(side*2.5f,-4)),"fixed facade window remains a solid wall footprint");
   progression.Data.minute=990;TickWorldLife(0);yield return new WaitForSeconds(.5f);cam.transform.rotation=Quaternion.Euler(42,45,0);yield return CaptureGameCamera(Path.Combine(folder,"street-day.png"),8);
   hero.p=homes[5].p+new Vector2(0,-1);yield return new WaitForSeconds(.5f);yield return CaptureGameCamera(Path.Combine(folder,"radio-interior.png"),5);
   hero.p=homes[6].p+new Vector2(0,-1);yield return new WaitForSeconds(.5f);yield return CaptureGameCamera(Path.Combine(folder,"clinic-interior.png"),5);
   hero.p=new Vector2(-12,-20);progression.Data.minute=1110;TickWorldLife(0);yield return new WaitForSeconds(.4f);yield return CaptureGameCamera(Path.Combine(folder,"street-dusk.png"),8);
   if(!atmosphere.FlashlightOn)atmosphere.ToggleFlashlight();progression.Data.minute=1380;TickWorldLife(0);yield return new WaitForSeconds(.4f);yield return CaptureGameCamera(Path.Combine(folder,"street-night.png"),8);
   atmosphere.SetLowQuality(true);Check(cam.depthTextureMode==DepthTextureMode.None,"low graphics disables extra depth/normal rendering");yield return CaptureGameCamera(Path.Combine(folder,"street-low.png"),8);
   atmosphere.SetLowQuality(false);Check(cam.depthTextureMode==DepthTextureMode.DepthNormals&&atmosphere.ShaderReady,"high graphics restores contact shading and supported colour pass");
   if(atmosphere.FlashlightOn)atmosphere.ToggleFlashlight();progression.Data.minute=990;TickWorldLife(0);yield return new WaitForSeconds(.4f);
   CheckStreet(Check);Save();Check(Load(),"world save still loads after environment changes");
   var samples=new List<float>();for(int i=0;i<60;i++){yield return null;samples.Add(Time.unscaledDeltaTime*1000);}samples.Sort();File.WriteAllText(Path.Combine(folder,"environment-performance.txt"),$"GPU: {SystemInfo.graphicsDeviceName}\nResolution: {Screen.width}x{Screen.height}\nDevelopment Player; stationary starting street, no active enemies.\nMedian frame: {samples[30]:F2} ms; p95: {samples[57]:F2} ms\nNot a full-world benchmark.\n");Application.Quit();
  }
 }
}
#endif
