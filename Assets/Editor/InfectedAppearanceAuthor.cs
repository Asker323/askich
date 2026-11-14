using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class InfectedAppearanceAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  static float G(float p,float c,float w)=>Mathf.Exp(-Mathf.Pow((p-c)/w,2));
  public static void BakeAndBuild(){Bake();BuildDistrict.Pair();}
  public static void Bake(){
   var root=PrefabUtility.LoadPrefabContents(Folder+"Lean.prefab");
   try{
    var sources=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterSources/Adult.fbx").GetComponentsInChildren<SkinnedMeshRenderer>(true);
    foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
     string name=skin.name;if(name!="Body"&&name!="Hair"&&name!="Trousers")continue;
     var source=sources.First(r=>r.name==name).sharedMesh;var mesh=UnityEngine.Object.Instantiate(source);var basis=source.vertices;var vertices=new Vector3[basis.Length];var uv=new List<Vector3>();var matrix=skin.transform.localToWorldMatrix;
     Vector3 Edit(Vector3 v){var p=matrix.MultiplyPoint3x4(v);
      if(name=="Body"){
       float face=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.035f,.09f,p.z));
       p.z-=.006f*G(Mathf.Abs(p.x),.052f,.022f)*G(p.y,1.665f,.032f)*face;
       p.y-=.007f*G(p.x,0,.04f)*G(p.y,1.595f,.027f)*face;
      }else if(name=="Trousers"){
       float leg=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1.03f,p.y));float centre=Mathf.Sign(p.x)*.105f;
       p.x=centre+(p.x-centre)*(1+.13f*leg);p.z*=1+.12f*leg;
       float folds=G(p.y,.52f,.13f)*.004f*Mathf.Sin(p.y*108+p.x*32);p.z+=Mathf.Sign(p.z)*folds;
      }else{
       float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.70f,1.79f,p.y));
       p.y+=edge*(.005f*Mathf.Sin(p.x*173+p.z*91)+.003f*Mathf.Sin(p.z*211));
      }
      return matrix.inverse.MultiplyPoint3x4(p);
     }
     for(int i=0;i<vertices.Length;i++){vertices[i]=Edit(basis[i]);uv.Add(matrix.MultiplyPoint3x4(vertices[i]));}mesh.vertices=vertices;mesh.SetUVs(2,uv);
     mesh.ClearBlendShapes();for(int shape=0;shape<source.blendShapeCount;shape++)for(int f=0;f<source.GetBlendShapeFrameCount(shape);f++){
      var delta=new Vector3[basis.Length];var normal=new Vector3[basis.Length];var tangent=new Vector3[basis.Length];source.GetBlendShapeFrameVertices(shape,f,delta,normal,tangent);
      for(int i=0;i<delta.Length;i++)delta[i]=Edit(basis[i]+delta[i])-vertices[i];mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,f),delta,normal,tangent);
     }
     mesh.RecalculateBounds();string path=Folder+"Lean"+name+"Refined.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     path=Folder+"Lean"+name+"Refined.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat"));AssetDatabase.CreateAsset(material,path);}
     if(name=="Body"){material.shader=Resources.Load<Shader>("Survival/InfectedSkin");material.mainTexture=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Body.mat").mainTexture;material.color=new Color(.80f,.84f,.78f);}
     else if(name=="Trousers"){material.shader=Resources.Load<Shader>("Survival/InfectedCloth");material.mainTexture=Texture2D.whiteTexture;material.color=new Color(.22f,.26f,.29f);}
     else{material.color=new Color(.28f,.23f,.18f);material.SetFloat("_Glossiness",.04f);}
     EditorUtility.SetDirty(material);skin.sharedMaterial=material;
    }
    PrefabUtility.SaveAsPrefabAsset(root,Folder+"Lean.prefab");AssetDatabase.SaveAssets();
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
 }
}
