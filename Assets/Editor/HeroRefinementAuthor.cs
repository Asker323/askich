using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class HeroRefinementAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  static float G(float v,float center,float width)=>Mathf.Exp(-Mathf.Pow((v-center)/width,2));
  static void Store(UnityEngine.Object value,string path){var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(old){EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(value);}else AssetDatabase.CreateAsset(value,path);}
  public static void BakeAndBuild(){Bake();BuildDistrict.Pair();}
  public static void Bake(){AssetDatabase.ImportAsset("Assets/Resources/Survival/Weathered.shader",ImportAssetOptions.ForceUpdate);Model();Gait();HeroArmMotionAuthor.Bake();AssetDatabase.SaveAssets();}
  static void Model(){
   var root=PrefabUtility.LoadPrefabContents(Folder+"Hero.prefab");
   try{
    var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterSources/Adult.fbx");var sourceSkins=source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
     string name=skin.name;if(name!="Body"&&name!="Shorts")continue;
     var original=name=="Body"?AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"HeroHands.asset"):sourceSkins.First(s=>s.name==name).sharedMesh;
     var mesh=UnityEngine.Object.Instantiate(original);mesh.name="Refined hero "+name;var vertices=original.vertices;var anatomical=new List<Vector3>();var frame=skin.transform.localToWorldMatrix;
     Vector3 Edit(Vector3 local){var p=frame.MultiplyPoint3x4(local);
      if(name=="Body"){
       float torso=G(p.y,1.27f,.22f)*Mathf.Clamp01(1-Mathf.Pow(Mathf.Abs(p.x)/.27f,6));p.x*=1-.055f*torso;p.z*=1-.035f*torso;
       float calf=G(p.y,.34f,.14f);p.x=Mathf.Sign(p.x)*.105f+(p.x-Mathf.Sign(p.x)*.105f)*(1-.07f*calf);p.z*=1-.05f*calf;
      }else{
       // Level actual garment boundaries, then loosen the thigh fabric.
       if(p.y>.965f)p.y=1.015f-(1.025f-p.y)*.10f;
       if(p.y<.715f)p.y=.675f+(p.y-.67f)*.12f;
       float leg=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.77f,.98f,p.y));float center=Mathf.Sign(p.x)*.105f;
       p.x=center+(p.x-center)*(1+.13f*leg);p.z*=1+.15f*leg;p.z+=Mathf.Sign(p.z)*.005f;
      }return frame.inverse.MultiplyPoint3x4(p);
     }
     for(int i=0;i<vertices.Length;i++){vertices[i]=Edit(vertices[i]);anatomical.Add(frame.MultiplyPoint3x4(vertices[i]));}mesh.vertices=vertices;mesh.SetUVs(2,anatomical);
     // Preserve all existing shape keys, including the two hand grips.
     mesh.ClearBlendShapes();for(int shape=0;shape<original.blendShapeCount;shape++)for(int f=0;f<original.GetBlendShapeFrameCount(shape);f++){
      var delta=new Vector3[vertices.Length];var normal=new Vector3[vertices.Length];var tangent=new Vector3[vertices.Length];original.GetBlendShapeFrameVertices(shape,f,delta,normal,tangent);var basis=original.vertices;for(int i=0;i<delta.Length;i++)delta[i]=Edit(basis[i]+delta[i])-vertices[i];mesh.AddBlendShapeFrame(original.GetBlendShapeName(shape),original.GetBlendShapeFrameWeight(shape,f),delta,normal,tangent);
     }
     // Preserve the source split normals across UV seams; recalculation makes skin faceted.
     mesh.RecalculateBounds();string path=Folder+"Hero"+name+"Refined.asset";Store(mesh,path);skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     string materialPath=Folder+"Hero"+name+"Refined.mat";
     var sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+(name=="Body"?"Body":"Shorts")+".mat");
     var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
     if(!mat){mat=new Material(sourceMaterial);AssetDatabase.CreateAsset(mat,materialPath);}
     else{mat.shader=sourceMaterial.shader;mat.CopyPropertiesFromMaterial(sourceMaterial);}
     mat.name="Hero "+name;
     if(name=="Body"){
      if(!mat.HasProperty("_HeroDetail"))throw new InvalidOperationException("Hero shader lacks anatomical detail property: "+mat.shader.name);
      mat.SetFloat("_HeroDetail",1f);mat.SetFloat("_Glossiness",.10f);mat.color=new Color(.89f,.83f,.78f);
      // The source material predates this shader property. Persist the new entry
      // explicitly: CopyPropertiesFromMaterial can leave it out of saved properties.
      var serialized=new SerializedObject(mat);var floats=serialized.FindProperty("m_SavedProperties.m_Floats");int index=-1;
      for(int i=0;i<floats.arraySize;i++)if(floats.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue=="_HeroDetail")index=i;
      if(index<0){index=floats.arraySize;floats.InsertArrayElementAtIndex(index);}
      var entry=floats.GetArrayElementAtIndex(index);entry.FindPropertyRelative("first").stringValue="_HeroDetail";entry.FindPropertyRelative("second").floatValue=1f;serialized.ApplyModifiedPropertiesWithoutUndo();
     }
     else{mat.shader=Resources.Load<Shader>("Survival/HeroCloth");mat.color=new Color(.20f,.25f,.27f);}
     EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);skin.sharedMaterial=mat;
     if(name=="Body"&&!System.IO.File.ReadAllText(materialPath).Contains("_HeroDetail: 1"))throw new InvalidOperationException("Hero skin detail did not persist in material");
    }
    PrefabUtility.SaveAsPrefabAsset(root,Folder+"Hero.prefab");
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  static void Limb(Transform upper,Transform lower,Transform foot,Vector3 goal){float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,foot.position);var d=goal-upper.position;float distance=Mathf.Clamp(d.magnitude,.02f,a+b-.002f);var direction=d.normalized;float x=(a*a-b*b+distance*distance)/(2*distance);var knee=upper.position+direction*x+Vector3.ProjectOnPlane(Vector3.forward,direction).normalized*Mathf.Sqrt(Mathf.Max(0,a*a-x*x));upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,knee-upper.position)*upper.rotation;lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,goal-lower.position)*lower.rotation;}
  static void Gait(){
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Hero.prefab"));root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   try{
    var bones=root.GetComponentsInChildren<Transform>(true);var map=bones.GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Hero_Idle.anim").SampleAnimation(root,0);
    var hip=map["Hips"].position;var feet=new[]{map["Foot.L"].position,map["Foot.R"].position};var rotations=new[]{map["Foot.L"].rotation,map["Foot.R"].rotation};
    foreach(string key in new[]{"Walk","Run"}){
     var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Hero_"+key+".anim");var output=UnityEngine.Object.Instantiate(source);bool run=key=="Run";float duration=run?.68f:.80f,stance=run?.30f:.52f,nominal=run?4.6f:1.8f;
     // Rescale all source curves to keep arm, tool and body phases aligned.
     foreach(var binding in AnimationUtility.GetCurveBindings(output)){var curve=AnimationUtility.GetEditorCurve(output,binding);var keys=curve.keys;for(int i=0;i<keys.Length;i++){keys[i].time*=duration/source.length;keys[i].inTangent*=source.length/duration;keys[i].outTangent*=source.length/duration;}curve.keys=keys;AnimationUtility.SetEditorCurve(output,binding,curve);}
     var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();int frames=run?68:80;
     for(int f=0;f<=frames;f++){
      float u=f/(float)frames;source.SampleAnimation(root,u*source.length);float support=.5f+.5f*Mathf.Cos(u*Mathf.PI*4);
      map["Hips"].position=hip+new Vector3(-Mathf.Sin(u*Mathf.PI*2)*(run?.016f:.009f),run?-.105f+.045f*(1-support):-.060f-.025f*support,0);
      for(int side=0;side<2;side++){
       string suffix=side==0?"L":"R";float phase=Mathf.Repeat(u+side*.5f,1);var goal=feet[side];float length=nominal*duration*stance;
       if(phase<stance){goal.z+=Mathf.Lerp(length/2,-length/2,phase/stance);goal.y+=Mathf.SmoothStep(0,.025f,Mathf.InverseLerp(.80f,1,phase/stance));}
       else{float swing=(phase-stance)/(1-stance);goal.z+=Mathf.Lerp(-length/2,length/2,Mathf.SmoothStep(0,1,swing));goal.y+=Mathf.Pow(Mathf.Sin(swing*Mathf.PI),run?1.25f:1)*(run?.30f:.07f);}
       Limb(map["UpperLeg."+suffix],map["LowerLeg."+suffix],map["Foot."+suffix],goal);
       float roll=phase<stance?Mathf.Lerp(-6,0,Mathf.Clamp01(phase/stance/.18f))+Mathf.SmoothStep(0,17,Mathf.InverseLerp(.8f,1,phase/stance)):-Mathf.Sin((phase-stance)/(1-stance)*Mathf.PI)*(run?22:8);
       map["Foot."+suffix].rotation=Quaternion.Euler(roll,0,0)*rotations[side];
      }
      foreach(var bone in bones.Where(t=>t.name=="Hips"||t.name.StartsWith("UpperLeg.")||t.name.StartsWith("LowerLeg.")||t.name.StartsWith("Foot."))){string path=AnimationUtility.CalculateTransformPath(bone,root.transform);var p=bone.localPosition;var q=bone.localRotation;Track("localPosition.x",p.x);Track("localPosition.y",p.y);Track("localPosition.z",p.z);Track("localRotation.x",q.x);Track("localRotation.y",q.y);Track("localRotation.z",q.z);Track("localRotation.w",q.w);
       void Track(string property,float value){var binding=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);if(!curves.TryGetValue(binding,out var curve))curves[binding]=curve=new AnimationCurve();curve.AddKey(u*duration,value);}
      }
     }
     foreach(var curve in curves)AnimationUtility.SetEditorCurve(output,curve.Key,curve.Value);output.EnsureQuaternionContinuity();Store(output,Folder+"Hero_"+key+".anim");
    }
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
