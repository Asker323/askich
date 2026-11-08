using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 // Authored in metre-space on the final rig; damage and movement stay in DistrictGame.
 public static class CharacterMotionAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  static void Store(UnityEngine.Object value,string path){var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);}else AssetDatabase.CreateAsset(value,path);}
  static float Smooth(float a,float b,float u)=>Mathf.SmoothStep(a,b,u);
  static Vector3 Path(float u,float[] times,Vector3[] points){int i=0;while(i<times.Length-2&&u>times[i+1])i++;return Vector3.Lerp(points[i],points[i+1],Mathf.SmoothStep(0,1,Mathf.InverseLerp(times[i],times[i+1],u)));}
  static void Limb(Transform upper,Transform lower,Transform end,Vector3 goal,Vector3 pole){
   float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,end.position);Vector3 d=goal-upper.position;float distance=Mathf.Clamp(d.magnitude,.015f,a+b-.003f);Vector3 direction=d.normalized;
   float x=(a*a-b*b+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;
   Vector3 joint=upper.position+direction*x+bend*height;upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,joint-upper.position)*upper.rotation;lower.rotation=Quaternion.FromToRotation(end.position-lower.position,goal-lower.position)*lower.rotation;
  }
  static void Surface(){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Surfaces/SkinAlbedo.png");if(!texture)throw new Exception("Missing authored skin texture");
   var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));importer.maxTextureSize=1024;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.SaveAndReimport();
   var skin=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Body.mat");skin.mainTexture=texture;skin.color=Color.white;skin.SetFloat("_Saturation",.93f);skin.SetFloat("_Glossiness",.19f);EditorUtility.SetDirty(skin);
   foreach(string name in new[]{"Shorts","Shirt","Trousers","Shoes","Hair"}){var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat");mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Surfaces/"+name+"Albedo.png");mat.color=Color.white;EditorUtility.SetDirty(mat);}
   foreach(var pair in new[]{("EyeL",new Color(.45f,.43f,.38f)),("EyeR",new Color(.45f,.43f,.38f))}){
    var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+pair.Item1+".mat");if(!mat)continue;mat.color=pair.Item2;mat.SetFloat("_Glossiness",pair.Item1.StartsWith("Eye")?.32f:.07f);EditorUtility.SetDirty(mat);
   }
  }
  public static void BakeAndBuild(){AdultCharacterAuthor.Bake();Surface();CharacterToolAuthor.Bake();Bake("Hero");Bake("Lean");AssetDatabase.SaveAssets();BuildDistrict.Pair();}
  static void Bake(string type){
   bool zombie=type=="Lean";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+type+".prefab");var root=UnityEngine.Object.Instantiate(prefab);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   try{
    var player=root.GetComponent<Animation>();AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Hero_Idle.anim").SampleAnimation(root,0);
    var bones=root.GetComponentsInChildren<Transform>(true);var map=bones.GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());var restP=bones.Select(t=>t.localPosition).ToArray();var restQ=bones.Select(t=>t.localRotation).ToArray();var restS=bones.Select(t=>t.localScale).ToArray();
    Vector3 hip=map["Hips"].position;var feet=new[]{map["Foot.L"].position,map["Foot.R"].position};var footQ=new[]{map["Foot.L"].rotation,map["Foot.R"].rotation};var hands=new[]{map["Hand.L"].position,map["Hand.R"].position};
    Debug.Log($"Art rig {type}: hips={hip}, feet={feet[0]},{feet[1]}, hands={hands[0]},{hands[1]}");
    var durations=zombie?new Dictionary<string,float>{{"Idle",3.2f},{"Walk",1.12f},{"Punch",1.2f},{"HitReact",.36f}}:new Dictionary<string,float>{{"Idle",3.4f},{"Walk",.72f},{"Run",.64f},{"Slash",.85f},{"Stab",.8f},{"Punch",.55f},{"HitReact",.36f}};
    foreach(var entry in durations){
     string key=entry.Key;float duration=entry.Value;int frames=Mathf.CeilToInt(duration*40);var output=new AnimationClip{name=key,legacy=true,frameRate=40};var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();
     for(int frame=0;frame<=frames;frame++){
      float u=(float)frame/frames,time=duration*u,wave=Mathf.Sin(u*Mathf.PI*2),breath=Mathf.Sin(u*Mathf.PI*2);
      for(int i=0;i<bones.Length;i++){bones[i].localPosition=restP[i];bones[i].localRotation=restQ[i];bones[i].localScale=restS[i];}
      bool gait=key=="Walk"||key=="Run",run=key=="Run";float crouch=zombie?.095f:gait?.035f:0;
      map["Hips"].position=hip+new Vector3(gait?wave*.012f:breath*.004f,-crouch+(gait?Mathf.Cos(u*Mathf.PI*4)*.013f:breath*.003f),0);
      map["Chest"].rotation=Quaternion.Euler(zombie?17:run?10:gait?3:0,gait?wave*(zombie?5:7):breath*.6f,zombie?4:0)*map["Chest"].rotation;
      map["Head"].rotation=Quaternion.Euler(zombie?-8:-1,0,zombie?-5:breath*.35f)*map["Head"].rotation;
      for(int side=0;side<2;side++){
       string suffix=side==0?"L":"R";float phase=Mathf.Repeat(u+side*.5f,1),stance=zombie?.56f:run?.31f:.46f;float nominal=zombie?1.45f:run?5:2.6f;
       Vector3 goal=feet[side];
       if(gait){
        float length=nominal*duration*stance;
        if(phase<stance)goal.z+=Mathf.Lerp(length/2,-length/2,phase/stance);
        else{float swing=(phase-stance)/(1-stance);goal.z+=Mathf.Lerp(-length/2,length/2,Mathf.SmoothStep(0,1,swing));goal.y+=Mathf.Sin(swing*Mathf.PI)*(zombie?.075f:run?.24f:.12f);}
       }
       Limb(map["UpperLeg."+suffix],map["LowerLeg."+suffix],map["Foot."+suffix],goal,Vector3.forward);
       map["Foot."+suffix].rotation=footQ[side]*Quaternion.Euler(gait&&phase>stance?Mathf.Sin((phase-stance)/(1-stance)*Mathf.PI)*(run?-28:-12):0,0,0);
       Vector3 hand=hands[side]+new Vector3(0,breath*.003f,0);
       if(gait)hand+=new Vector3(0,run?.18f:.015f,Mathf.Sin((u+side*.5f)*Mathf.PI*2)*(run?.23f:.15f));
       if(zombie)hand=new Vector3(hands[side].x*(side==0?1.1f:.9f),1.08f+wave*.025f,.25f+(side==0?.04f:-.02f));
       if(!zombie&&key=="Slash"&&side==0)hand=Path(u,new[]{0f,.20f,.36f,.58f,.86f,1f},new[]{hands[0],new Vector3(hands[0].x,1.53f,-.08f),new Vector3(-.08f,1.24f,.52f),new Vector3(.08f,.97f,.38f),hands[0],hands[0]});
       if(!zombie&&key=="Stab"&&side==0)hand=Path(u,new[]{0f,.2f,.4f,.55f,1f},new[]{hands[0],new Vector3(hands[0].x,1.17f,.02f),new Vector3(-.08f,1.23f,.58f),new Vector3(-.1f,1.21f,.55f),hands[0]});
       if(key=="Punch"){
        float weight=u<.47f?Smooth(0,1,u/.47f):Smooth(1,0,(u-.47f)/.53f);var target=new Vector3(side==0?-.20f:.20f,zombie?1.30f:1.28f,zombie?.64f:.57f);hand=Vector3.Lerp(hand,target,zombie&&side==0?weight*.55f:weight);
       }
       if(key=="HitReact"){float w=Mathf.Sin(u*Mathf.PI);hand+=Vector3.forward*.12f*w;}
       Limb(map["UpperArm."+suffix],map["LowerArm."+suffix],map["Hand."+suffix],hand,new Vector3(side==0?-1:1,-.2f,-.4f));
      }
      if(key=="HitReact")map["Chest"].rotation=Quaternion.Euler(-13*Mathf.Sin(u*Mathf.PI),0,3*Mathf.Sin(u*Mathf.PI))*map["Chest"].rotation;
      if(!zombie){
       foreach(string toolName in new[]{"Axe","Spear"}){
        var tool=map[toolName];Vector3 axis=Vector3.up;Vector3 direction=toolName=="Spear"?new Vector3(0,.1f,1):new Vector3(0,-1,.20f);
        if(toolName=="Axe"&&key=="Slash")direction=Path(u,new[]{0f,.20f,.36f,.58f,.86f,1f},new[]{direction,new Vector3(0,1,-.3f),new Vector3(.1f,-.16f,1),new Vector3(.2f,-.9f,.25f),direction,direction});
        if(toolName=="Axe"&&key=="Slash"||toolName=="Spear"&&key=="Stab"){
         var hand=map["Hand.L"];var finger=map["Index1.L"].position-hand.position;
         var target=Quaternion.FromToRotation(finger,Vector3.Cross(Vector3.left,direction).normalized)*hand.rotation;
         float gripWeight=Mathf.SmoothStep(0,1,u/.15f)*Mathf.SmoothStep(1,0,(u-.82f)/.18f);
         hand.rotation=Quaternion.Slerp(hand.rotation,target,gripWeight);
        }
        tool.rotation=Quaternion.FromToRotation(axis,direction.normalized);
        tool.position=map["Hand.L"].position+(map["Index1.L"].position-map["Hand.L"].position)*.68f;
       }
      }
      foreach(var bone in bones){if(bone==root.transform)continue;string path=AnimationUtility.CalculateTransformPath(bone,root.transform);
       void Track(string property,float value){var binding=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);if(!curves.TryGetValue(binding,out var curve))curves[binding]=curve=new AnimationCurve();curve.AddKey(time,value);}
       Vector3 p=bone.localPosition;Quaternion q=bone.localRotation;Track("localPosition.x",p.x);Track("localPosition.y",p.y);Track("localPosition.z",p.z);Track("localRotation.x",q.x);Track("localRotation.y",q.y);Track("localRotation.z",q.z);Track("localRotation.w",q.w);Vector3 scale=bone.localScale;Track("localScale.x",scale.x);Track("localScale.y",scale.y);Track("localScale.z",scale.z);
      }
     }
     foreach(var pair in curves){
      var curve=pair.Value;var keys=curve.keys;
      if(keys.All(k=>Mathf.Abs(k.value-keys[0].value)<.000001f))curve=new AnimationCurve(new Keyframe(0,keys[0].value),new Keyframe(duration,keys[0].value));
      AnimationUtility.SetEditorCurve(output,pair.Key,curve);
     }
     output.EnsureQuaternionContinuity();string pathOut=Folder+type+"_"+key+".anim";Store(output,pathOut);var stored=AssetDatabase.LoadAssetAtPath<AnimationClip>(pathOut);player.RemoveClip(key);player.AddClip(stored,key);
    }
    AssetDatabase.SaveAssets();AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+type+"_Idle.anim").SampleAnimation(root,0);PrefabUtility.SaveAsPrefabAsset(root,Folder+type+".prefab");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
