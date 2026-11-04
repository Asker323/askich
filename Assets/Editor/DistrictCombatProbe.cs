using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class DistrictCombatProbe {
  public static void Run(){
   var report=new StringBuilder("Clip phase; hand position in normalized model coordinates\n");
   foreach(string prefab in new[]{"Characters_Matt","Zombie_Basic"}){
    var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Survival/Art/"+prefab));
    var anim=model.GetComponentInChildren<Animation>();Transform hand=null,left=null;
    foreach(var bone in model.GetComponentsInChildren<Transform>()){if(bone.name.ToLowerInvariant()=="index1.r")hand=bone;if(bone.name.ToLowerInvariant()=="index1.l")left=bone;}
    foreach(AnimationState clip in anim){
     string key=clip.name.Substring(clip.name.LastIndexOf('|')+1);
     if(key!="Slash"&&key!="Stab"&&key!="Punch")continue;
     report.AppendLine(prefab+" "+key+" length="+clip.length);
     for(int n=0;n<=10;n++){anim.Stop();clip.clip.SampleAnimation(anim.gameObject,clip.length*n/10f);Vector3 p=model.transform.InverseTransformPoint(hand.position);report.AppendLine(n/10f+" R="+p.ToString("F3")+" L="+model.transform.InverseTransformPoint(left.position).ToString("F3"));}
    }
    UnityEngine.Object.DestroyImmediate(model);
   }
   File.WriteAllText(Environment.GetEnvironmentVariable("GOIDA_COMBAT_PROBE"),report.ToString());
  }
 }
}
