using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace Askich.Editor {
 public static class AdultCharacterAuthor {
  const string Folder="Assets/Resources/Survival/Human";
  static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
  static Dictionary<string,Transform> Map(GameObject root,bool old){
   var map=new Dictionary<string,Transform>();
   void Add(string human,string bone){map[human]=Find(root,bone);}
   Add("Hips","Hips");Add("Spine",old?"Abdomen":"Spine");Add("Chest",old?"Torso":"Chest");Add("Neck","Neck");Add("Head","Head");
   foreach(string side in new[]{"L","R"}){
    string h=side=="L"?"Left":"Right";
    Add(h+"Shoulder","Shoulder."+side);Add(h+"UpperArm","UpperArm."+side);Add(h+"LowerArm","LowerArm."+side);Add(h+"Hand",(old?"Index1.":"Hand.")+side);
    Add(h+"UpperLeg","UpperLeg."+side);Add(h+"LowerLeg","LowerLeg."+side);Add(h+"Foot","Foot."+side);
    if(!old)Add(h+"Toes","Toes."+side);
   }

   return map;
  }
  static GameObject Shadow(Dictionary<string,Transform> source,out Dictionary<string,Transform> map){
   var root=new GameObject("HumanSource");map=new Dictionary<string,Transform>();
   foreach(var pair in source){
    string key=pair.Key,parent=key=="Hips"?null:key=="Spine"?"Hips":key=="Chest"?"Spine":key=="Neck"?"Chest":key=="Head"?"Neck":key.EndsWith("Shoulder")?"Chest":key.EndsWith("UpperArm")?key.Replace("UpperArm","Shoulder"):key.EndsWith("LowerArm")?key.Replace("LowerArm","UpperArm"):key.EndsWith("Hand")?key.Replace("Hand","LowerArm"):key.EndsWith("UpperLeg")?"Hips":key.EndsWith("LowerLeg")?key.Replace("LowerLeg","UpperLeg"):key.Replace("Foot","LowerLeg");
    var bone=new GameObject("Human"+key).transform;bone.SetParent(parent==null?root.transform:map[parent],false);bone.SetPositionAndRotation(pair.Value.position,pair.Value.rotation);map[key]=bone;
   }
   map["Hips"].position=source["Hips"].position;return root;
  }
  static void CopyPose(Dictionary<string,Transform> source,Dictionary<string,Transform> shadow){foreach(var p in source)shadow[p.Key].SetPositionAndRotation(p.Value.position,p.Value.rotation);}
  static void MatchCrawlerHands(Dictionary<string,Transform> source,Dictionary<string,Transform> target){
   for(int iteration=0;iteration<3;iteration++){
    Vector3 shift=Vector3.zero;
    foreach(string side in new[]{"Left","Right"}){
     var upper=target[side+"UpperArm"];var lower=target[side+"LowerArm"];var hand=target[side+"Hand"];Vector3 wanted=source[side+"Hand"].position;wanted.y=Mathf.Max(.075f,wanted.y);
     float reach=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position)-.012f;var delta=wanted-upper.position;
     if(delta.magnitude>reach)shift+=delta.normalized*(delta.magnitude-reach);
    }
    shift.x=0;target["Hips"].position+=shift*.65f;
   }
   foreach(string side in new[]{"Left","Right"}){
    var upper=target[side+"UpperArm"];var lower=target[side+"LowerArm"];var hand=target[side+"Hand"];Vector3 wanted=source[side+"Hand"].position;wanted.y=Mathf.Max(.075f,wanted.y);
    float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);Vector3 delta=wanted-upper.position;float distance=Mathf.Clamp(delta.magnitude,.02f,a+b-.005f);Vector3 direction=delta.normalized;
    float x=(a*a-b*b+distance*distance)/(2*distance),h=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));Vector3 outward=Vector3.right*Mathf.Sign(upper.position.x-target["Chest"].position.x);Vector3 bend=Vector3.ProjectOnPlane(outward,direction).normalized;
    Vector3 elbow=upper.position+direction*x+bend*h;upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
    lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,wanted-lower.position)*lower.rotation;
   }
  }
  static void PlaceHand(Dictionary<string,Transform> bones,string side,Vector3 wanted,float weight){
   var upper=bones[side+"UpperArm"];var lower=bones[side+"LowerArm"];var hand=bones[side+"Hand"];
   float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);var delta=wanted-upper.position;float distance=Mathf.Clamp(delta.magnitude,.02f,a+b-.005f);var direction=delta.normalized;
   float x=(a*a-b*b+distance*distance)/(2*distance),h=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));var outward=Vector3.right*Mathf.Sign(upper.position.x-bones["Chest"].position.x);var bend=Vector3.ProjectOnPlane(outward,direction).normalized;
   var elbow=upper.position+direction*x+bend*h;var original=upper.rotation;upper.rotation=Quaternion.Slerp(original,Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*original,weight);
   original=lower.rotation;lower.rotation=Quaternion.Slerp(original,Quaternion.FromToRotation(hand.position-lower.position,wanted-lower.position)*original,weight);
  }
  static Avatar Avatar(GameObject root,Dictionary<string,Transform> map){
   foreach(string side in new[]{"Left","Right"}){
    Vector3 d=Vector3.right*Mathf.Sign(map[side+"UpperArm"].position.x-map["Chest"].position.x);
    void Aim(string parent,string child,Vector3 dir){var a=map[side+parent];var b=map[side+child];a.rotation=Quaternion.FromToRotation(b.position-a.position,dir)*a.rotation;}
    Aim("UpperArm","LowerArm",d);Aim("LowerArm","Hand",d);Aim("UpperLeg","LowerLeg",Vector3.down);Aim("LowerLeg","Foot",Vector3.down);
   }
   var human=map.Select(p=>new HumanBone{humanName=p.Key,boneName=p.Value.name,limit=new HumanLimit{useDefaultValues=true}}).ToArray();
   var skeleton=root.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
   var description=new HumanDescription{human=human,skeleton=skeleton,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false};
   var avatar=AvatarBuilder.BuildHumanAvatar(root,description);if(!avatar||!avatar.isValid||!avatar.isHuman)throw new Exception("Invalid human avatar: "+root.name);return avatar;
  }
  static void SaveAsset(UnityEngine.Object asset,string path){var existing=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);if(existing){EditorUtility.CopySerialized(asset,existing);UnityEngine.Object.DestroyImmediate(asset);}else AssetDatabase.CreateAsset(asset,path);}
  public static void Bake(){
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
   string path="Assets/CharacterSources/Adult.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=false;importer.importBlendShapes=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;importer.SaveAndReimport();
   foreach(string type in new[]{"Hero","Lean","Heavy","Crawler"})BakeType(type,path);
   AssetDatabase.SaveAssets();
  }
  public static void BakeAndBuild(){Bake();BuildDistrict.Pair();}
  static Material SkinMaterial(string name,Color tint,bool skin){
   string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(skin?Shader.Find("Berezki/Weathered"):Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}mat.color=tint;mat.SetFloat("_Glossiness",.08f);if(skin)mat.SetFloat("_Saturation",.82f);EditorUtility.SetDirty(mat);return mat;
  }
  static void BakeType(string type,string sourcePath){
   bool hero=type=="Hero";string oldName=hero?"Characters_Matt":type=="Heavy"?"Zombie_Chubby":"Zombie_Basic";
   var source=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Survival/Art/"+oldName));source.name="Source";
   var target=new GameObject("Adult");var geometry=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));geometry.name="Geometry";geometry.transform.SetParent(target.transform,false);
   Debug.Log("Target left shoulder: "+Find(target,"UpperArm.L").position);
   Avatar sa=null,ta=null;GameObject shadow=null;
   try{
    var animator=target.GetComponentInChildren<Animator>();if(animator)UnityEngine.Object.DestroyImmediate(animator);
    var sourcePlayer=source.GetComponentInChildren<Animation>();var sourceClips=new Dictionary<string,AnimationClip>();
    foreach(AnimationState state in sourcePlayer){string key=state.name.Substring(state.name.LastIndexOf('|')+1);if(!sourceClips.ContainsKey(key))sourceClips[key]=state.clip;}
    if(hero){foreach(string key in new[]{"Search","DrinkProvision","EatProvision","RestProvision"})sourceClips[key]=Resources.Load<AnimationClip>("Survival/"+key);}
    else if(type=="Crawler")sourceClips["CrawlerLunge"]=Resources.Load<AnimationClip>("Survival/CrawlerLunge");
    sourceClips["Idle"].SampleAnimation(sourcePlayer.gameObject,0);
    var sm=Map(source,true);var tm=Map(target,false);
    var native=new List<Transform>();
    if(hero)foreach(string weapon in new[]{"Axe","Spear"}){
     var original=source.GetComponentsInChildren<Renderer>(true).First(r=>r.name==weapon);var mesh=new Mesh();if(original is SkinnedMeshRenderer skinned)skinned.BakeMesh(mesh);else mesh=UnityEngine.Object.Instantiate(original.GetComponent<MeshFilter>().sharedMesh);var v=mesh.vertices;Transform hand=sm["LeftHand"];
     float toolScale=weapon=="Axe"?.55f:.75f;
     for(int i=0;i<v.Length;i++)v[i]=toolScale*(Quaternion.Inverse(hand.rotation)*(original.transform.TransformPoint(v[i])-hand.position));
     mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();SaveAsset(mesh,Folder+"/"+weapon+".asset");
     var tool=new GameObject(weapon,typeof(MeshFilter),typeof(MeshRenderer));tool.transform.SetParent(tm["LeftHand"],false);tool.transform.localScale=Vector3.one/Mathf.Abs(tm["LeftHand"].lossyScale.x);tool.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+weapon+".asset");tool.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Survival/Art/Atlas");native.Add(tool.transform);
    }
    foreach(var skin in target.GetComponentsInChildren<SkinnedMeshRenderer>()){
     string name=skin.name;bool body=name=="Body";Color c=body?new Color(.68f,.49f,.38f):name=="Shorts"?new Color(.13f,.17f,.20f):name=="Shirt"?new Color(.27f,.32f,.26f):name=="Trousers"?new Color(.2f,.24f,.28f):name=="Hair"?new Color(.055f,.045f,.035f):name.StartsWith("Iris")?new Color(.06f,.065f,.05f):name.StartsWith("Eye")?new Color(.58f,.59f,.53f):new Color(.15f,.14f,.12f);
     skin.sharedMaterials=Enumerable.Repeat(SkinMaterial(name,c,body),skin.sharedMaterials.Length).ToArray();skin.updateWhenOffscreen=true;
    }
    var rest=target.GetComponentsInChildren<Transform>().ToArray();var restPositions=rest.Select(t=>t.localPosition).ToArray();var restRotations=rest.Select(t=>t.localRotation).ToArray();var restScales=rest.Select(t=>t.localScale).ToArray();
    float ankleGround=Mathf.Min(tm["LeftFoot"].position.y,tm["RightFoot"].position.y);
    shadow=Shadow(sm,out var shadowMap);sa=Avatar(shadow,shadowMap);ta=Avatar(target,tm);
    using(var sh=new HumanPoseHandler(sa,shadow.transform))using(var th=new HumanPoseHandler(ta,target.transform)){
     var bones=target.GetComponentsInChildren<Transform>(true).Where(t=>t!=target.transform).ToArray();
     var storedClips=new List<AnimationClip>();
     var player=target.AddComponent<Animation>();player.playAutomatically=false;player.cullingType=AnimationCullingType.AlwaysAnimate;
     string[] keys=hero?new[]{"Idle","Walk","Run","Punch","Slash","Stab","HitReact","Death","Duck","Search","DrinkProvision","EatProvision","RestProvision"}:type=="Crawler"?new[]{"Idle","Walk","Punch","HitReact","Death","Crawl","CrawlerLunge"}:new[]{"Idle","Walk","Punch","HitReact","Death"};
     foreach(string key in keys){
      if(!sourceClips.TryGetValue(key,out var clip)){if(key=="HitReact")continue;throw new Exception("Missing source motion: "+type+" "+key);}
      var output=new AnimationClip{name=key,legacy=true,frameRate=24};var curves=new Dictionary<EditorCurveBinding,AnimationCurve>();int count=Mathf.CeilToInt(clip.length*24);
      var pose=new HumanPose{muscles=new float[HumanTrait.MuscleCount]};
      for(int frame=0;frame<=count;frame++){
       float time=clip.length*frame/count;if(type=="Crawler"&&(key=="Death"||key=="HitReact"))sourceClips["Crawl"].SampleAnimation(sourcePlayer.gameObject,sourceClips["Crawl"].length*.12f);else clip.SampleAnimation(sourcePlayer.gameObject,time);CopyPose(sm,shadowMap);sh.GetHumanPose(ref pose);th.SetHumanPose(ref pose);
       if(hero&&(key=="Idle"||key=="Search"||key=="DrinkProvision"||key=="EatProvision")){
        for(int i=0;i<rest.Length;i++){rest[i].localPosition=restPositions[i];rest[i].localRotation=restRotations[i];rest[i].localScale=restScales[i];}
        foreach(string side in new[]{"Left","Right"}){
         Vector3 direction=new Vector3(Mathf.Sign(tm[side+"UpperArm"].position.x-tm["Chest"].position.x)*.1f,-1,.025f).normalized;
         var upper=tm[side+"UpperArm"];var lower=tm[side+"LowerArm"];var hand=tm[side+"Hand"];
         upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,direction)*upper.rotation;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,direction)*lower.rotation;
        }
        tm["Chest"].localRotation*=Quaternion.Euler(Mathf.Sin(time*Mathf.PI)*.5f,0,0);
        if(key=="Search"){
         tm["Chest"].rotation=Quaternion.AngleAxis(8,Vector3.right)*tm["Chest"].rotation;
         foreach(string side in new[]{"Left","Right"}){float sign=Mathf.Sign(tm[side+"UpperArm"].position.x-tm["Chest"].position.x);PlaceHand(tm,side,new Vector3(sign*.22f,1.10f+Mathf.Sin(time*Mathf.PI*2/1.5f+(side=="Left"?0:Mathf.PI))*.035f,.46f),1);}
        }
        if(key=="DrinkProvision"||key=="EatProvision"){
         float u=time/clip.length;float w=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.23f))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,u)));Vector3 mouth=tm["Head"].position+Vector3.up*.085f+Vector3.forward*.12f;
         if(key=="DrinkProvision")PlaceHand(tm,"Left",mouth,w);
         else{PlaceHand(tm,"Left",new Vector3(-.18f,1.1f,.34f),w);PlaceHand(tm,"Right",mouth+Vector3.up*(Mathf.Sin(time*14)*.012f),w);}
        }
       }
       if(key!="Death"&&key!="Crawl"&&key!="CrawlerLunge"&&key!="RestProvision")tm["Hips"].position+=Vector3.up*(ankleGround-Mathf.Min(tm["LeftFoot"].position.y,tm["RightFoot"].position.y));
       if(type=="Crawler"&&(key=="Crawl"||key=="CrawlerLunge"||key=="HitReact"||key=="Death"))MatchCrawlerHands(sm,tm);
       if(type=="Crawler"&&key=="Death"){float settle=Mathf.SmoothStep(0,1,time/clip.length);tm["Hips"].position-=Vector3.up*.1f*settle;tm["Chest"].rotation=Quaternion.AngleAxis(12*settle,Vector3.forward)*tm["Chest"].rotation;}
       if(type=="Crawler"&&key=="HitReact")tm["Chest"].rotation=Quaternion.AngleAxis(-8*Mathf.Sin(time/clip.length*Mathf.PI),Vector3.right)*tm["Chest"].rotation;
       if(type=="Crawler"&&key=="CrawlerLunge"&&(frame==8||frame==18))Debug.Log($"Crawler retarget t={time:F3}: source hand {sm["RightHand"].position}; target hand {tm["RightHand"].position}; source hips {sm["Hips"].position}; target hips {tm["Hips"].position}");
       foreach(var tool in native)tool.localRotation=Quaternion.Inverse(tm["LeftHand"].rotation)*sm["LeftHand"].rotation;
       foreach(var bone in bones){
        string path=AnimationUtility.CalculateTransformPath(bone,target.transform);
        void Track(string property,float value){var binding=EditorCurveBinding.FloatCurve(path,typeof(Transform),property);if(!curves.TryGetValue(binding,out var curve))curves[binding]=curve=new AnimationCurve();curve.AddKey(time,value);}
        Vector3 p=bone.localPosition;Quaternion q=bone.localRotation;Track("localPosition.x",p.x);Track("localPosition.y",p.y);Track("localPosition.z",p.z);Track("localRotation.x",q.x);Track("localRotation.y",q.y);Track("localRotation.z",q.z);Track("localRotation.w",q.w);Vector3 scale=bone.localScale;Track("localScale.x",scale.x);Track("localScale.y",scale.y);Track("localScale.z",scale.z);
       }
      }
      foreach(var pair in curves)AnimationUtility.SetEditorCurve(output,pair.Key,pair.Value);output.EnsureQuaternionContinuity();string clipPath=Folder+"/"+type+"_"+key+".anim";SaveAsset(output,clipPath);var stored=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);stored.name=key;EditorUtility.SetDirty(stored);storedClips.Add(stored);player.AddClip(stored,key);
     }
     AssetDatabase.SaveAssets();AnimationUtility.SetAnimationClips(player,storedClips.ToArray());
     var idle=storedClips.First(c=>c.name==(type=="Crawler"?"Crawl":"Idle"));idle.SampleAnimation(target,0);
    }
    PrefabUtility.SaveAsPrefabAsset(target,Folder+"/"+type+".prefab");Debug.Log("Retargeted adult "+type);
   }finally{if(sa)UnityEngine.Object.DestroyImmediate(sa);if(ta)UnityEngine.Object.DestroyImmediate(ta);if(shadow)UnityEngine.Object.DestroyImmediate(shadow);UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);}
  }
 }
}
