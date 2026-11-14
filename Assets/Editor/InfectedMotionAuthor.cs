using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 // Authored in metre-space on the final rig; damage and movement stay in DistrictGame.
 public static class InfectedMotionAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  static void Store(UnityEngine.Object value,string path){var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(old){EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(value);}else AssetDatabase.CreateAsset(value,path);}
  static float Smooth(float a,float b,float u)=>Mathf.SmoothStep(a,b,u);
  static Vector3 Path(float u,float[] times,Vector3[] points){int i=0;while(i<times.Length-2&&u>times[i+1])i++;return Vector3.Lerp(points[i],points[i+1],Mathf.SmoothStep(0,1,Mathf.InverseLerp(times[i],times[i+1],u)));}
  static void Limb(Transform upper,Transform lower,Transform end,Vector3 goal,Vector3 pole){
   float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,end.position);Vector3 d=goal-upper.position;float distance=Mathf.Clamp(d.magnitude,.015f,a+b-.003f);Vector3 direction=d.normalized;
   float x=(a*a-b*b+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;
   Vector3 joint=upper.position+direction*x+bend*height;upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,joint-upper.position)*upper.rotation;lower.rotation=Quaternion.FromToRotation(end.position-lower.position,goal-lower.position)*lower.rotation;
  }
  public static void BakeAndBuild(){Bake("Lean");Dress();AssetDatabase.SaveAssets();BuildDistrict.Pair();}
  static void Dress(){
   var root=PrefabUtility.LoadPrefabContents(Folder+"Lean.prefab");
   try{
    var shirt=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Shirt");
    var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterSources/Adult.fbx").GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Body").sharedMesh;
    var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Frayed infected shirt";var vertices=mesh.vertices;var uv=new List<Vector3>();var frame=shirt.transform.localToWorldMatrix;var triangles=mesh.triangles;var keep=new List<int>();bool InShirt(int index){var p=frame.MultiplyPoint3x4(vertices[index]);return p.y>1.025f&&p.y<1.56f&&Mathf.Abs(p.x)<(p.y>1.27f?.31f:.23f);}
    for(int i=0;i<triangles.Length;i+=3)if(InShirt(triangles[i])&&InShirt(triangles[i+1])&&InShirt(triangles[i+2])){keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);}mesh.triangles=keep.ToArray();var normals=mesh.normals;
    for(int i=0;i<vertices.Length;i++){var v=frame.MultiplyPoint3x4(vertices[i])+frame.MultiplyVector(normals[i]).normalized*.014f;float hem=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.0f,1.12f,v.y));v.y-=hem*(.012f+.018f*Mathf.Sin(v.x*89+v.z*73));vertices[i]=frame.inverse.MultiplyPoint3x4(v);uv.Add(v);}
    mesh.vertices=vertices;mesh.SetUVs(2,uv);mesh.RecalculateBounds();Store(mesh,Folder+"LeanShirt.asset");shirt.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"LeanShirt.asset");var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Body");shirt.bones=body.bones;shirt.rootBone=body.rootBone;
    string path=Folder+"LeanShirt.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Resources.Load<Shader>("Survival/InfectedCloth"));AssetDatabase.CreateAsset(mat,path);}mat.shader=Resources.Load<Shader>("Survival/InfectedCloth");mat.mainTexture=Texture2D.whiteTexture;mat.color=new Color(.48f,.52f,.45f);EditorUtility.SetDirty(mat);shirt.sharedMaterial=mat;
    PrefabUtility.SaveAsPrefabAsset(root,Folder+"Lean.prefab");
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  static void Bake(string type){
   bool zombie=type=="Lean";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+type+".prefab");var root=UnityEngine.Object.Instantiate(prefab);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   try{
    var player=root.GetComponent<Animation>();AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Hero_Idle.anim").SampleAnimation(root,0);
    var bones=root.GetComponentsInChildren<Transform>(true);var map=bones.GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());var restP=bones.Select(t=>t.localPosition).ToArray();var restQ=bones.Select(t=>t.localRotation).ToArray();var restS=bones.Select(t=>t.localScale).ToArray();
    Vector3 hip=map["Hips"].position;var feet=new[]{map["Foot.L"].position,map["Foot.R"].position};var footQ=new[]{map["Foot.L"].rotation,map["Foot.R"].rotation};var hands=new[]{map["Hand.L"].position,map["Hand.R"].position};var handQ=new[]{map["Hand.L"].rotation,map["Hand.R"].rotation};
    Debug.Log($"Art rig {type}: hips={hip}, feet={feet[0]},{feet[1]}, hands={hands[0]},{hands[1]}");
    var durations=zombie?new Dictionary<string,float>{{"Idle",3.2f},{"Walk",1.12f},{"Punch",1.2f},{"HitReact",.36f}}:new Dictionary<string,float>{{"Idle",3.4f},{"Walk",.86f},{"Run",.72f},{"Slash",.85f},{"Stab",.8f},{"Punch",.55f},{"HitReact",.36f}};
    foreach(var entry in durations){
     string key=entry.Key;float duration=entry.Value;int frames=Mathf.CeilToInt(duration*40);var output=new AnimationClip{name=key,legacy=true,frameRate=40};var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();
     for(int frame=0;frame<=frames;frame++){
      float u=(float)frame/frames,time=duration*u,wave=Mathf.Sin(u*Mathf.PI*2),breath=Mathf.Sin(u*Mathf.PI*2);
      for(int i=0;i<bones.Length;i++){bones[i].localPosition=restP[i];bones[i].localRotation=restQ[i];bones[i].localScale=restS[i];}
      bool gait=key=="Walk"||key=="Run",run=key=="Run";float support=.5f+.5f*Mathf.Cos(u*Mathf.PI*4);float crouch=zombie?.085f+(gait?.018f*(1-wave):0):gait?(run?.10f:.025f+.09f*support):0;
      map["Hips"].position=hip+new Vector3(gait?-wave*(run?.018f:.012f):breath*.004f,-crouch+(run?Mathf.Sin(u*Mathf.PI*4)*.018f:gait?0:breath*.003f),0);
      if(gait)map["Hips"].rotation=Quaternion.Euler(0,wave*(run?6:3.5f),0)*map["Hips"].rotation;
      map["Chest"].rotation=Quaternion.Euler(zombie?19+wave*2:run?12:gait?2:0,gait?-wave*(zombie?5:run?8:5):breath*.6f,zombie?4:0)*map["Chest"].rotation;
      map["Head"].rotation=Quaternion.Euler(zombie?-8:-1,0,zombie?-7+breath*2:breath*.35f)*map["Head"].rotation;
      for(int side=0;side<2;side++){
       string suffix=side==0?"L":"R";float phase=Mathf.Repeat(u+side*.5f,1),stance=zombie?.54f:run?.27f:.54f;float nominal=zombie?1.45f:run?4.6f:1.8f;
       Vector3 goal=feet[side];
       if(gait){
        float length=nominal*duration*stance;
        if(phase<stance){goal.z+=Mathf.Lerp(length/2,-length/2,phase/stance);goal.y+=Mathf.SmoothStep(0,.035f,Mathf.InverseLerp(.75f,1,phase/stance));}
        else{float swing=(phase-stance)/(1-stance);goal.z+=Mathf.Lerp(-length/2,length/2,Mathf.SmoothStep(0,1,swing));goal.y+=Mathf.Sin(swing*Mathf.PI)*(zombie?(side==0?.085f:.035f):run?.15f:.065f);}
       }
       Limb(map["UpperLeg."+suffix],map["LowerLeg."+suffix],map["Foot."+suffix],goal,Vector3.forward);
       float roll=0;if(gait&&phase<stance){float contact=phase/stance;roll=contact<.2f?Mathf.Lerp(-5,0,contact/.2f):contact>.75f?Mathf.Lerp(0,18,(contact-.75f)/.25f):0;}else if(gait)roll=-Mathf.Sin((phase-stance)/(1-stance)*Mathf.PI)*(run?18:8);
       map["Foot."+suffix].rotation=footQ[side]*Quaternion.Euler(roll,0,0);
       Vector3 hand=hands[side]+new Vector3(0,breath*.003f,0);
       if(gait){float swing=-Mathf.Cos((u+side*.5f)*Mathf.PI*2);hand+=new Vector3(0,(run?.18f:.015f)+(1-swing*swing)*.015f,swing*(run?.23f:.16f));}
       if(zombie)hand=new Vector3(hands[side].x*(side==0?1.02f:.95f),(side==0?.93f:1.08f)+Mathf.Sin(u*Mathf.PI*2+side)*.025f,(side==0?.12f:.25f)+(gait?Mathf.Sin(u*Mathf.PI*2+side*2.1f)*.07f:0));
       if(!zombie&&key=="Slash"&&side==0)hand=Path(u,new[]{0f,.20f,.36f,.58f,.86f,1f},new[]{hands[0],new Vector3(hands[0].x,1.53f,-.08f),new Vector3(-.08f,1.24f,.52f),new Vector3(.08f,.97f,.38f),hands[0],hands[0]});
       if(!zombie&&key=="Stab"&&side==0)hand=Path(u,new[]{0f,.2f,.4f,.55f,1f},new[]{hands[0],new Vector3(hands[0].x,1.17f,.02f),new Vector3(-.08f,1.23f,.58f),new Vector3(-.1f,1.21f,.55f),hands[0]});
       if(key=="Punch"){
        if(zombie){var rest=hand;var windup=rest+new Vector3(0,.19f,-.16f);var contact=new Vector3(side==0?-.20f:.20f,1.30f,side==0?.48f:.64f);hand=Path(u,new[]{0f,.28f,.47f,.60f,1f},new[]{rest,windup,contact,contact+Vector3.down*.08f,rest});}
        else{float weight=u<.47f?Smooth(0,1,u/.47f):Smooth(1,0,(u-.47f)/.53f);hand=Vector3.Lerp(hand,new Vector3(side==0?-.20f:.20f,1.28f,.57f),weight);}
       }
       if(key=="HitReact"){float w=Mathf.Sin(u*Mathf.PI);hand+=Vector3.forward*.12f*w;}
       Limb(map["UpperArm."+suffix],map["LowerArm."+suffix],map["Hand."+suffix],hand,new Vector3(side==0?-.25f:.25f,-.3f,-1));
       if(gait&&!zombie)map["Hand."+suffix].rotation=Quaternion.Euler(run?10:0,wave*2,0)*handQ[side];
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
        tool.rotation=Quaternion.FromToRotation(axis,direction.normalized)*(toolName=="Axe"?Quaternion.AngleAxis(180,Vector3.up):Quaternion.identity);
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
