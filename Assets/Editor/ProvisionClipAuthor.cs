using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class ProvisionClipAuthor {
  public static void BakeAndBuild(){Bake();BuildDistrict.Pair();}
  public static void Bake(){foreach(string name in new[]{"DrinkProvision","EatProvision","RestProvision"})BakeOne(name);}
  static void BakeOne(string name){
   var rig=Object.Instantiate(Resources.Load<GameObject>("Survival/Art/Characters_Matt"));
   try{
    var player=rig.GetComponentInChildren<Animation>();AnimationClip source=null;
    foreach(AnimationState s in player)if(s.name.EndsWith(name=="RestProvision"?"|Duck":"|Idle"))source=s.clip;
    var bones=new Dictionary<string,Transform>();foreach(var b in player.GetComponentsInChildren<Transform>())bones[b.name]=b;
    var bindings=AnimationUtility.GetCurveBindings(source);var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();
    var clip=new AnimationClip{name=name,legacy=true,frameRate=24,wrapMode=WrapMode.ClampForever};
    for(int frame=0;frame<=24;frame++){
     float t=frame/24f;
     float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.23f))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,t)));
     source.SampleAnimation(player.gameObject,name=="RestProvision"?.65f*weight:0);
     foreach(string side in new[]{"L","R"}){
      var upper=bones["UpperArm."+side];var lower=bones["LowerArm."+side];var hand=bones["Index1."+side];
      Vector3 target=name=="RestProvision"?new Vector3(side=="L"?-.25f:.25f,.6f,.35f):side=="L"?bones["Head"].position+new Vector3(0,-.14f,.19f):new Vector3(.22f,1.05f,.4f);
      if(name=="DrinkProvision"&&side=="R")continue;
      float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
      Vector3 delta=target-upper.position;float distance=Mathf.Clamp(delta.magnitude,.02f,a+b-.005f);var direction=delta.normalized;
      float x=(a*a-b*b+distance*distance)/(2*distance);float h=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));
      Vector3 bend=Vector3.ProjectOnPlane(side=="L"?Vector3.left:Vector3.right,direction).normalized;
      var elbow=upper.position+direction*x+bend*h;
      var original=upper.rotation;upper.rotation=Quaternion.Slerp(original,Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*original,weight);
      original=lower.rotation;lower.rotation=Quaternion.Slerp(original,Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*original,weight);
     }
     foreach(var bone in bones["Root"].GetComponentsInChildren<Transform>()){
      if(bone.name.EndsWith("_end"))continue;string path=AnimationUtility.CalculateTransformPath(bone,player.transform);
      void Track(string property,float value){var key=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);if(!curves.TryGetValue(key,out var curve))curves[key]=curve=new AnimationCurve();curve.AddKey(t,value);}
      var q=bone.localRotation;Track("localRotation.x",q.x);Track("localRotation.y",q.y);Track("localRotation.z",q.z);Track("localRotation.w",q.w);
      if(System.Array.Exists(bindings,v=>v.path==path&&v.propertyName.Contains("LocalPosition"))){var p=bone.localPosition;Track("localPosition.x",p.x);Track("localPosition.y",p.y);Track("localPosition.z",p.z);}
     }
    }
    foreach(var c in curves)AnimationUtility.SetEditorCurve(clip,c.Key,c.Value);clip.EnsureQuaternionContinuity();
    string output="Assets/Resources/Survival/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
    if(existing){EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,output);
    AssetDatabase.SaveAssets();
   }finally{Object.DestroyImmediate(rig);}
  }
 }
}
