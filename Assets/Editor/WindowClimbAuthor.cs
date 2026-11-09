using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class WindowClimbAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  public static void BakeAndBuild(){Bake();AssetDatabase.SaveAssets();BuildDistrict.Pair();}
  static void Limb(Transform upper,Transform lower,Transform end,Vector3 goal,Vector3 pole){
   float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,end.position);var d=goal-upper.position;float distance=Mathf.Clamp(d.magnitude,.015f,a+b-.003f);var direction=d.normalized;
   float x=(a*a-b*b+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));var bend=Vector3.ProjectOnPlane(pole,direction).normalized;
   var joint=upper.position+direction*x+bend*height;upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,joint-upper.position)*upper.rotation;lower.rotation=Quaternion.FromToRotation(end.position-lower.position,goal-lower.position)*lower.rotation;
  }
  public static void Bake(){
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Hero.prefab"));root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   try{
    var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Hero_Idle.anim");idle.SampleAnimation(root,0);var bones=root.GetComponentsInChildren<Transform>(true);var map=bones.GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
    var restP=bones.Select(t=>t.localPosition).ToArray();var restQ=bones.Select(t=>t.localRotation).ToArray();var hip=map["Hips"].position;
    var feet=new[]{map["Foot.L"].position,map["Foot.R"].position};var footQ=new[]{map["Foot.L"].rotation,map["Foot.R"].rotation};var hands=new[]{map["Hand.L"].position,map["Hand.R"].position};var handQ=new[]{map["Hand.L"].rotation,map["Hand.R"].rotation};
    var palmFrames=new Quaternion[2];for(int side=0;side<2;side++){string s=side==0?"L":"R";var fingers=map["Index1."+s].position-hands[side];var thumb=map["Thumb1."+s].position-map["Index1."+s].position;palmFrames[side]=Quaternion.LookRotation(fingers,thumb);}
    var output=new AnimationClip{name="WindowClimb",legacy=true,frameRate=40};var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();
    float R(float u,float a,float b)=>WindowClimbMotion.Ramp(u,a,b);
    for(int f=0;f<=112;f++){
     float u=f/112f,time=u*2.8f,travel=1.44f*WindowClimbMotion.Travel(u),weight=R(u,.08f,.3f)*(1-R(u,.78f,1));
     for(int i=0;i<bones.Length;i++){bones[i].localPosition=restP[i];bones[i].localRotation=restQ[i];}
     map["Hips"].position=hip+new Vector3(-.035f*Mathf.Sin(u*Mathf.PI*2),(.08f-.12f*(1-R(u,.30f,.42f)))*weight,.08f*weight);
     map["Spine"].rotation=Quaternion.Euler(52*weight,0,0)*map["Spine"].rotation;
     map["Chest"].rotation=Quaternion.Euler(8*weight,-8*Mathf.Sin(u*Mathf.PI)*weight,0)*map["Chest"].rotation;
     map["Head"].rotation=Quaternion.Euler(-14*weight,0,0)*map["Head"].rotation;
     for(int side=0;side<2;side++){
      string suffix=side==0?"L":"R";float lift=side==0?.14f:.33f,begin=side==0?.30f:.49f,end=side==0?.60f:.78f,land=side==0?.74f:.94f;
      float footTravel=1.44f*R(u,begin,end),height=.98f*R(u,lift,begin)*(1-R(u,end,land));
      var goal=feet[side]+new Vector3(0,height,footTravel-travel);
      Limb(map["UpperLeg."+suffix],map["LowerLeg."+suffix],map["Foot."+suffix],goal,Vector3.forward);
      map["Foot."+suffix].rotation=Quaternion.Euler(-8*height,0,0)*footQ[side];
      float contact=R(u,.04f+side*.035f,.23f)*(1-R(u,.48f,.74f));var hand=Vector3.Lerp(hands[side],new Vector3(side==0?-.24f:.24f,.87f,.72f-travel),contact);
      Limb(map["UpperArm."+suffix],map["LowerArm."+suffix],map["Hand."+suffix],hand,new Vector3(side==0?-.7f:.7f,-.35f,-1));
      // Fingers point across the sill; thumbs point inward, palms rest downwards.
      var support=Quaternion.LookRotation(Vector3.forward,side==0?Vector3.right:Vector3.left)*Quaternion.Inverse(palmFrames[side])*handQ[side];
      map["Hand."+suffix].rotation=Quaternion.Slerp(handQ[side],support,contact);
     }
     foreach(var bone in bones){if(bone==root.transform)continue;string path=AnimationUtility.CalculateTransformPath(bone,root.transform);
      void Track(string property,float value){var binding=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);if(!curves.TryGetValue(binding,out var curve))curves[binding]=curve=new AnimationCurve();curve.AddKey(time,value);}
      var p=bone.localPosition;var q=bone.localRotation;Track("localPosition.x",p.x);Track("localPosition.y",p.y);Track("localPosition.z",p.z);Track("localRotation.x",q.x);Track("localRotation.y",q.y);Track("localRotation.z",q.z);Track("localRotation.w",q.w);
     }
    }
    foreach(var pair in curves){var c=pair.Value;if(c.keys.All(k=>Mathf.Abs(k.value-c.keys[0].value)<.000001f))c=new AnimationCurve(new Keyframe(0,c.keys[0].value),new Keyframe(2.8f,c.keys[0].value));AnimationUtility.SetEditorCurve(output,pair.Key,c);}output.EnsureQuaternionContinuity();
    string pathOut=Folder+"Hero_WindowClimb.anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(pathOut);if(old){EditorUtility.CopySerialized(output,old);UnityEngine.Object.DestroyImmediate(output);}else AssetDatabase.CreateAsset(output,pathOut);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
