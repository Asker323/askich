#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckInfectedArt(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_INFECTED_ART_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"infected-art-"+Guid.NewGuid()+".json");playing=true;paused=true;Application.runInBackground=true;
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}hero.p=new Vector2(0,-24);hero.root.gameObject.SetActive(false);
   var enemy=infected.First(z=>z.id%3==0);enemy.root.gameObject.SetActive(true);enemy.root.position=V(hero.p);enemy.root.rotation=Quaternion.identity;enemy.visual.SetPaused(true);
   var results=new List<string>();void Check(bool ok,string label){results.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines(Path.Combine(folder,"infected-art-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(label);}}
   Vector3 Point(string bone)=>enemy.root.InverseTransformPoint(enemy.visual.ArtBone(bone).position);
   var shirt=enemy.root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Shirt");Check(shirt.sharedMesh==Resources.Load<Mesh>("Survival/Human/LeanShirt")&&shirt.sharedMaterial.shader.isSupported&&shirt.sharedMaterial.shader.name=="Zonbee/InfectedCloth","lean infected uses independent worn garment with supported material");
   enemy.visual.ArtSample("Idle",0);Check(Mathf.Abs(Point("Hand.L").y-Point("Hand.R").y)>.10f,"idle silhouette has visibly asymmetric arms");
   float left=0,right=0,low=100;for(int i=0;i<=80;i++){enemy.visual.ArtSample("Walk",i/80f);left=Mathf.Max(left,Point("Foot.L").y);right=Mathf.Max(right,Point("Foot.R").y);low=Mathf.Min(low,Point("Foot.L").y,Point("Foot.R").y);}
   Check(left>right+.025f&&low>-.02f,"uneven recovery heights distinguish the gait while ankles stay above ground");
   enemy.visual.ArtSample("Walk",0);var start=Point("Hand.R");enemy.visual.ArtSample("Walk",1);Check(Vector3.Distance(start,Point("Hand.R"))<.003f,"walk cycle joins without a hand jump");
   enemy.visual.ArtSample("Punch",0);float rest=Point("Hand.R").z;enemy.visual.ArtSample("Punch",.28f);float windup=Point("Hand.R").z;enemy.visual.ArtSample("Punch",.47f);float contact=Point("Hand.R").z;
   Check(windup<rest-.08f&&contact>windup+.35f,"attack has a backward preparation and forward reach at the damage phase");
   Check(Mathf.Abs(EnemyImpact/EnemyDuration-.47f)<.001f&&Mathf.Abs(enemy.visual.ArtClip("Punch").length-EnemyDuration)<.001f,"authored contact phase matches existing combat timing");
   enemy.visual.ArtSample("HitReact",.5f);Check(enemy.visual.Ready,"hit reaction remains available on the revised skeleton");
   cam.transform.rotation=Quaternion.Euler(12,180,0);enemy.visual.ArtSample("Idle",.15f);yield return CaptureGameCamera(Path.Combine(folder,"infected-idle.png"),1.3f);
   enemy.visual.ArtSample("Punch",.28f);yield return CaptureGameCamera(Path.Combine(folder,"infected-windup.png"),1.3f);enemy.visual.ArtSample("Punch",.47f);yield return CaptureGameCamera(Path.Combine(folder,"infected-contact.png"),1.3f);
   string frames=Path.Combine(folder,"frames");Directory.CreateDirectory(frames);foreach(string key in new[]{"Walk","Punch"})for(int i=0;i<40;i++){enemy.visual.ArtSample(key,i/40f);yield return CaptureGameCamera(Path.Combine(frames,key+"-"+i.ToString("D3")+".png"),1.5f);}
   hero.root.gameObject.SetActive(true);enemy.root.gameObject.SetActive(false);Save();Check(Load(),"save still restores world after visual revision");Application.Quit();
  }
 }
}
#endif
