using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 // Replace only relaxed locomotion arms. The bind pose is immutable and avoids
 // feeding a previously bent elbow back into the next animation bake.
 public static class HeroArmMotionAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  public static void BakeAndBuild(){Bake();AssetDatabase.SaveAssets();BuildDistrict.Pair();}
  public static void Bake(){
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Hero.prefab"));root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   try {
    var map=root.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
    var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Body");var bind=new Dictionary<Transform,Matrix4x4>();
    for(int i=0;i<body.bones.Length;i++)bind[body.bones[i]]=body.localToWorldMatrix*body.sharedMesh.bindposes[i].inverse;
    Quaternion BindRotation(Transform bone)=>bind[bone].rotation;
    Quaternion LocalBind(Transform bone)=>Quaternion.Inverse(bind.TryGetValue(bone.parent,out var parent)?parent.rotation:bone.parent.rotation)*BindRotation(bone);
    var neutral=new Dictionary<Transform,Quaternion>();foreach(string side in new[]{"L","R"})foreach(string part in new[]{"Shoulder.","UpperArm.","LowerArm.","Hand."}){var bone=map[part+side];neutral[bone]=LocalBind(bone);}
    foreach(string key in new[]{"Idle","Walk","Run"}){
     var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Hero_"+key+".anim");var output=UnityEngine.Object.Instantiate(source);output.name=source.name;int frames=Mathf.CeilToInt(source.length*40);var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();
     for(int frame=0;frame<=frames;frame++){
      float phase=(float)frame/frames,time=source.length*phase;source.SampleAnimation(root,time);Quaternion torso=map["Chest"].rotation*Quaternion.Inverse(BindRotation(map["Chest"]));
      foreach(string side in new[]{"L","R"}){
       var shoulder=map["Shoulder."+side];var upper=map["UpperArm."+side];var lower=map["LowerArm."+side];var hand=map["Hand."+side];foreach(var bone in new[]{shoulder,upper,lower,hand})bone.localRotation=neutral[bone];
       float sign=Mathf.Sign(bind[upper].m03-bind[map["Chest"]].m03);float swing=-Mathf.Cos((phase+(side=="L"?0:.5f))*Mathf.PI*2);bool run=key=="Run",walk=key=="Walk";
       float cycle=(phase+(side=="L"?0:.5f))*Mathf.PI*2;
       // The forearm follows the upper arm with a small delay rather than forming
       // a rigid pendulum. Leave air under the armpit without flaring the elbows.
       float angle=(run?-8+swing*32:walk?-5+swing*23:1.5f*Mathf.Sin(cycle))*Mathf.Deg2Rad;
       float flex=(run?76+12*Mathf.Sin(cycle-.4f):walk?18+9*Mathf.Sin(cycle-.45f):14+2*Mathf.Sin(cycle+.3f))*Mathf.Deg2Rad;
       float lateral=run?.19f:walk?.20f:.17f;
       Vector3 upperDirection=torso*new Vector3(sign*(lateral+.025f*Mathf.Sin(cycle-.3f)),-Mathf.Cos(angle),Mathf.Sin(angle)).normalized;
       Vector3 lowerDirection=torso*new Vector3(sign*.045f,-Mathf.Cos(angle+flex),Mathf.Sin(angle+flex)).normalized;
       upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,upperDirection)*upper.rotation;
       lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,lowerDirection)*lower.rotation;
       Vector3 bindFingers=bind[map["Index1."+side]].GetColumn(3)-bind[hand].GetColumn(3);
       Vector3 bindThumb=bind[map["Thumb1."+side]].GetColumn(3)-bind[map["Index1."+side]].GetColumn(3);
       Quaternion palmFrame=Quaternion.LookRotation(bindFingers,bindThumb);
       // A little pronation and delayed wrist flex keep the palms from staying
       // locked to the thighs. Use an anatomical palm frame, not bone Euler axes.
       float wrist=(run?6:walk?5:2)*Mathf.Sin(cycle-.65f);
       float roll=6+(run?8:walk?7:3)*Mathf.Sin(cycle-.8f);
       Vector3 fingers=Quaternion.AngleAxis(wrist,torso*Vector3.right)*lowerDirection;
       Vector3 thumbDirection=Quaternion.AngleAxis(sign*roll,fingers)*Vector3.Cross(fingers,torso*Vector3.right);
       hand.rotation=Quaternion.LookRotation(fingers,thumbDirection)*Quaternion.Inverse(palmFrame)*BindRotation(hand);
       foreach(var bone in new[]{shoulder,upper,lower,hand}){
        string path=AnimationUtility.CalculateTransformPath(bone,root.transform);Quaternion q=bone.localRotation;
        Track(path,"localRotation.x",q.x);Track(path,"localRotation.y",q.y);Track(path,"localRotation.z",q.z);Track(path,"localRotation.w",q.w);
       }
      }
      // Equipment stays in the palm after changing the arm and wrist pose.
      foreach(string toolName in new[]{"Axe","Spear"}){
       var tool=map[toolName];tool.position=map["Hand.L"].position+(map["Index1.L"].position-map["Hand.L"].position)*.68f;
       Vector3 direction=torso*(toolName=="Axe"?new Vector3(0,-1,.20f):new Vector3(0,.1f,1));tool.rotation=Quaternion.FromToRotation(Vector3.up,direction.normalized)*(toolName=="Axe"?Quaternion.AngleAxis(180,Vector3.up):Quaternion.identity);
       string path=AnimationUtility.CalculateTransformPath(tool,root.transform);Vector3 p=tool.localPosition;Quaternion q=tool.localRotation;Track(path,"localPosition.x",p.x);Track(path,"localPosition.y",p.y);Track(path,"localPosition.z",p.z);Track(path,"localRotation.x",q.x);Track(path,"localRotation.y",q.y);Track(path,"localRotation.z",q.z);Track(path,"localRotation.w",q.w);
      }
      void Track(string path,string property,float value){var binding=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);if(!curves.TryGetValue(binding,out var curve))curves[binding]=curve=new AnimationCurve();curve.AddKey(time,value);}
     }
     foreach(var pair in curves)AnimationUtility.SetEditorCurve(output,pair.Key,pair.Value);output.EnsureQuaternionContinuity();EditorUtility.CopySerialized(output,source);EditorUtility.SetDirty(source);UnityEngine.Object.DestroyImmediate(output);
    }
    AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Hero_Idle.anim").SampleAnimation(root,0);PrefabUtility.SaveAsPrefabAsset(root,Folder+"Hero.prefab");AssetDatabase.SaveAssets();
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
