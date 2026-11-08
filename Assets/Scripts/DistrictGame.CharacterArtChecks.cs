#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckCharacterArt(){
   string folder=Environment.GetEnvironmentVariable("BEREZKI_CHARACTER_ART_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"character-art-"+Guid.NewGuid()+".json");Application.runInBackground=true;playing=true;paused=false;yield return null;yield return null;paused=true;
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}hero.p=new Vector2(0,-24);hero.root.position=V(hero.p);hero.root.rotation=Quaternion.identity;inventory.Load(new DistrictInventoryData());UpdateClothing();item.gameObject.SetActive(false);axeHead.gameObject.SetActive(false);spearHead.gameObject.SetActive(false);yield return null;
   var results=new System.Collections.Generic.List<string>();
   void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"character-art-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   var body=hero.root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Body");
   File.WriteAllText(Path.Combine(folder,"character-material.txt"),$"Material {body.sharedMaterial.name}, shader {body.sharedMaterial.shader.name}, colour {body.sharedMaterial.color}, saturation {body.sharedMaterial.GetFloat("_Saturation")}, texture {body.sharedMaterial.mainTexture.name}\n");
   Check(body.sharedMaterial.mainTexture&&body.sharedMaterial.mainTexture.width==1024,"authored 1024 skin colour and ambient occlusion texture present in Player");
   Check(hero.visual.ArtClip("Walk").frameRate==40&&hero.visual.ArtClip("Run").frameRate==40,"survivor uses newly authored locomotion clips");
   hero.visual.ArtSample("Walk",.08f);var foot=hero.visual.ArtBone("Foot.L");var a=hero.root.InverseTransformPoint(foot.position);hero.visual.ArtSample("Walk",.18f);var b=hero.root.InverseTransformPoint(foot.position);float stanceSpeed=(a.z-b.z)/(hero.visual.ArtClip("Walk").length*.1f);
   File.WriteAllText(Path.Combine(folder,"character-art-measurements.txt"),$"Walk stance foot speed {stanceSpeed:F3} m/s; nominal 1.8. Foot y {a.y:F3}/{b.y:F3}.\n");
   Check(Mathf.Abs(stanceSpeed-1.8f)<.55f,"walk stance foot travel matches world movement within measured tolerance");
   float low=100;for(int i=0;i<=40;i++){hero.visual.ArtSample("Walk",i/40f);low=Mathf.Min(low,hero.visual.ArtBone("Foot.L").position.y,hero.visual.ArtBone("Foot.R").position.y);}
   Check(low>-.02f,"walking ankles remain above street ground throughout cycle");
   hero.visual.ArtSample("Slash",.20f);a=hero.root.InverseTransformPoint(hero.visual.Hand.position);hero.visual.ArtSample("Slash",.36f);b=hero.root.InverseTransformPoint(hero.visual.Hand.position);
   Check(b.z-a.z>.25f,"axe hand moves from readable windup to forward impact at damage phase");
   var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();keyboard.MakeCurrent();UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
   paused=false;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));yield return new WaitForSeconds(.35f);
   Check(hero.visual.State=="Walk"&&hero.visual.ArtPhase("Walk")>0,"actual Player Animation advances the authored walk");
   float phase=Mathf.Repeat(hero.visual.ArtPhase("Walk"),1);hero.visual.Tick(5,.016f,0,true);
   Check(Mathf.Abs(Mathf.Repeat(hero.visual.ArtPhase("Run"),1)-phase)<.002f,"actual walk to run transition preserves gait phase");
   paused=true;hero.visual.SetPaused(true);UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);heroVelocity=Vector2.zero;hero.p=new Vector2(0,-24);hero.root.position=V(hero.p);
   int grip=Enumerable.Range(0,body.sharedMesh.blendShapeCount).Where(i=>body.sharedMesh.GetBlendShapeName(i).EndsWith("GripL")).DefaultIfEmpty(-1).First();
   hero.visual.Equip(1);Check(grip>=0&&body.GetBlendShapeWeight(grip)>99,"equipped tool closes authored hand shape in actual Player");
   hero.visual.Equip(0);Check(body.GetBlendShapeWeight(grip)>0&&body.GetBlendShapeWeight(grip)<50,"unequipping restores relaxed fingers");
   hero.visual.ArtSample("Idle",.1f);cam.transform.rotation=Quaternion.Euler(12,0,0);hero.root.rotation=Quaternion.Euler(0,195,0);yield return CaptureGameCamera(Path.Combine(folder,"character-art-hero.png"),1.25f);
   hero.visual.Equip(1);hero.root.rotation=Quaternion.Euler(0,225,0);hero.visual.ArtSample("Slash",.36f);yield return CaptureGameCamera(Path.Combine(folder,"character-art-axe.png"),1.6f);
   var lean=infected.First(z=>z.id%3==0);lean.root.gameObject.SetActive(true);lean.root.position=V(hero.p);lean.root.rotation=Quaternion.Euler(0,195,0);lean.visual.SetPaused(true);lean.visual.ArtSample("Idle",.2f);hero.root.gameObject.SetActive(false);
   Check(lean.visual.ArtClip("Walk").frameRate==40&&lean.visual.ArtClip("Punch").frameRate==40,"lean infected has distinct authored gait and attack");
   yield return CaptureGameCamera(Path.Combine(folder,"character-art-infected.png"),1.25f);
   hero.root.gameObject.SetActive(true);lean.root.gameObject.SetActive(false);hero.root.rotation=Quaternion.Euler(0,225,0);cam.transform.rotation=Quaternion.Euler(18,0,0);
   string film=Path.Combine(folder,"motion-frames");Directory.CreateDirectory(film);
   foreach(string key in new[]{"Walk","Run","Slash"})for(int frame=0;frame<32;frame++){hero.visual.ArtSample(key,frame/32f);yield return CaptureGameCamera(Path.Combine(film,key+"-"+frame.ToString("D3")+".png"),1.6f);}
   Save();Check(Load()&&inventory.Data.weapon==0&&!inventory.Data.shirt&&!inventory.Data.trousers&&!inventory.Data.shoes,"visual stage preserves ordinary save loading and shorts-only initial equipment");Application.Quit();
  }
 }
}
#endif
