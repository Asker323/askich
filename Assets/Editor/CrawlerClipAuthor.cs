using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Askich.Editor {
 public static class CrawlerClipAuthor {
  const string AssetPath="Assets/Resources/Survival/CrawlerLunge.anim";
  const float Brace=.54f, Contact=.76f, End=1.68f;

  // Bake onto the same legacy skeleton as the locomotion clips. No runtime IK or allocations.
  [MenuItem("Tools/Berezki/Bake crawler attack")]
  public static void Bake(){
   var rig=Object.Instantiate(Resources.Load<GameObject>("Survival/Art/Zombie_Basic"));
   try{
    var player=rig.GetComponentInChildren<Animation>();
    AnimationClip crawl=null;
    foreach(AnimationState state in player)if(state.name.EndsWith("|Crawl"))crawl=state.clip;
    if(!crawl)throw new System.InvalidOperationException("Crawler locomotion clip missing");
    var bones=new Dictionary<string,Transform>();
    foreach(var bone in player.GetComponentsInChildren<Transform>())bones[bone.name]=bone;
    var translations=new HashSet<string>();
    foreach(var binding in AnimationUtility.GetCurveBindings(crawl))
     if(binding.propertyName.Contains("LocalPosition"))translations.Add(binding.path);
    var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();
    var clip=new AnimationClip{name="CrawlerLunge",legacy=true,frameRate=30,wrapMode=WrapMode.ClampForever};

    void Track(Transform bone,string property,float time,float value){
     var path=AnimationUtility.CalculateTransformPath(bone,player.transform);
     var key=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);
     if(!curves.TryGetValue(key,out var curve))curves[key]=curve=new AnimationCurve();
     curve.AddKey(time,value);
    }
    for(int frame=0;frame<=51;frame++){
     float t=Mathf.Min(frame/30f,End);
     float brace=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.33f));
     float thrust=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Brace,Contact,t));
     float release=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Contact+.15f,End,t));
     float weight=brace*(1-release);
     crawl.SampleAnimation(player.gameObject,crawl.length*Mathf.Lerp(.12f,.38f,thrust)*(1-release));
     bones["Hips"].position+=weight*(Vector3.up*Mathf.Lerp(.07f,-.005f,thrust)+Vector3.forward*Mathf.Lerp(-.12f,.10f,thrust));
     foreach(var side in new[]{"L","R"}){
      float mirror=side=="L"?-1:1;
      var arm=bones["UpperArm."+side];var elbow=bones["LowerArm."+side];var finger=bones["Index1."+side];
      void Aim(Transform joint,Transform end,Vector3 direction){
       var swing=Quaternion.FromToRotation((end.position-joint.position).normalized,direction.normalized);
       joint.rotation=Quaternion.Slerp(Quaternion.identity,swing,weight)*joint.rotation;
      }
      Aim(arm,elbow,Vector3.Lerp(new Vector3(mirror*.6f,-.8f,-.3f),new Vector3(mirror*.24f,-.2f,1),thrust));
      float forearmLength=Vector3.Distance(finger.position,elbow.position);
      float vertical=Mathf.Clamp((.045f-elbow.position.y)/forearmLength,-.99f,.99f);
      Vector3 plant=new Vector3(-mirror*.25f,0,.62f).normalized*Mathf.Sqrt(1-vertical*vertical)+Vector3.up*vertical;
      Aim(elbow,finger,Vector3.Lerp(plant,new Vector3(-mirror*.10f,-.12f,1),thrust));
     }
     // Store only skeleton transforms. Meshes, materials and the gameplay root stay independent.
     foreach(var bone in bones["Root"].GetComponentsInChildren<Transform>()){
      if(bone.name.EndsWith("_end"))continue;
      var r=bone.localRotation;
      Track(bone,"localRotation.x",t,r.x);Track(bone,"localRotation.y",t,r.y);
      Track(bone,"localRotation.z",t,r.z);Track(bone,"localRotation.w",t,r.w);
      if(bone==bones["Hips"]||translations.Contains(AnimationUtility.CalculateTransformPath(bone,player.transform))){
       var p=bone.localPosition;
       Track(bone,"localPosition.x",t,p.x);Track(bone,"localPosition.y",t,p.y);Track(bone,"localPosition.z",t,p.z);
      }
     }
    }
    foreach(var entry in curves)AnimationUtility.SetEditorCurve(clip,entry.Key,entry.Value);
    clip.EnsureQuaternionContinuity();
    foreach(float t in new[]{.36f,Contact}){
     clip.SampleAnimation(player.gameObject,t);
     Debug.Log($"Baked pose at {t}: head {bones["Head"].position}, hand {bones["Index1.R"].position}, foot {bones["Foot.R"].position}");
    }
    var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetPath);
    if(existing){EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);}
    else AssetDatabase.CreateAsset(clip,AssetPath);
    AssetDatabase.SaveAssets();
   }finally{Object.DestroyImmediate(rig);}
  }
 }
}
