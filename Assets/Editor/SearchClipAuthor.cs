using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Askich.Editor {
 public static class SearchClipAuthor {
  [MenuItem("Tools/Berezki/Bake search animation")]
  public static void Bake(){
   var rig=Object.Instantiate(Resources.Load<GameObject>("Survival/Art/Characters_Matt"));
   try{
    var player=rig.GetComponentInChildren<Animation>();AnimationClip idle=null;
    foreach(AnimationState s in player)if(s.name.EndsWith("|Idle"))idle=s.clip;
    if(!idle)throw new System.InvalidOperationException("Survivor idle clip missing");
    var bones=new Dictionary<string,Transform>();foreach(var bone in player.GetComponentsInChildren<Transform>())bones[bone.name]=bone;
    var bindings=AnimationUtility.GetCurveBindings(idle);var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();
    var clip=new AnimationClip{name="Search",legacy=true,frameRate=24,wrapMode=WrapMode.Loop};
    for(int frame=0;frame<=36;frame++){
     float t=frame/24f;idle.SampleAnimation(player.gameObject,t%idle.length);
     foreach(var side in new[]{"L","R"}){
      float mirror=side=="L"?-1:1;
      float pulse=Mathf.Sin(t*Mathf.PI*2/1.5f+(side=="L"?0:Mathf.PI));
      var arm=bones["UpperArm."+side];var elbow=bones["LowerArm."+side];var hand=bones["Index1."+side];
      void Aim(Transform joint,Transform end,Vector3 direction){
       joint.rotation=Quaternion.FromToRotation((end.position-joint.position).normalized,direction.normalized)*joint.rotation;
      }
      Aim(arm,elbow,new Vector3(mirror*.18f,-.56f,.83f));
      Aim(elbow,hand,new Vector3(-mirror*.12f,-.18f+pulse*.13f,1));
     }
     foreach(var bone in bones["Root"].GetComponentsInChildren<Transform>()){
      if(bone.name.EndsWith("_end"))continue;
      string path=AnimationUtility.CalculateTransformPath(bone,player.transform);
      void Track(string property,float value){
       var key=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);
       if(!curves.TryGetValue(key,out var curve))curves[key]=curve=new AnimationCurve();
       curve.AddKey(t,value);
      }
      var q=bone.localRotation;Track("localRotation.x",q.x);Track("localRotation.y",q.y);Track("localRotation.z",q.z);Track("localRotation.w",q.w);
      // Imported poses also contain translation curves. Preserve their absolute values.
      bool translated=System.Array.Exists(bindings,b=>b.path==path&&b.propertyName.Contains("LocalPosition"));
      if(translated){var p=bone.localPosition;Track("localPosition.x",p.x);Track("localPosition.y",p.y);Track("localPosition.z",p.z);}
     }
    }
    foreach(var entry in curves)AnimationUtility.SetEditorCurve(clip,entry.Key,entry.Value);
    clip.EnsureQuaternionContinuity();
    const string pathAsset="Assets/Resources/Survival/Search.anim";
    var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(pathAsset);
    if(existing){EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);}
    else AssetDatabase.CreateAsset(clip,pathAsset);
    AssetDatabase.SaveAssets();
   }finally{Object.DestroyImmediate(rig);}
  }
 }
}
