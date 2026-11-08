using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace Askich.Editor {
 public static class HeroHandsAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  public static void BakeAndBuild(){Bake();BuildDistrict.Pair();}
  public static void Bake(){
   var root=PrefabUtility.LoadPrefabContents(Folder+"Hero.prefab");
   try {
    var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Body");
    var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CharacterSources/Adult.fbx").GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Body").sharedMesh;
    var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Hero hands";int index=Enumerable.Range(0,mesh.blendShapeCount).First(i=>mesh.GetBlendShapeName(i).EndsWith("GripL"));
    int count=mesh.vertexCount;var delta=new Vector3[count];var normals=new Vector3[count];var tangents=new Vector3[count];mesh.GetBlendShapeFrameVertices(index,0,delta,normals,tangents);var vertices=mesh.vertices;var moving=Enumerable.Range(0,count).Where(i=>delta[i].sqrMagnitude>1e-10f).ToArray();var right=new Vector3[count];var rn=new Vector3[count];var rt=new Vector3[count];int matched=0;
    Vector3 Mirror(Vector3 v)=>new Vector3(-v.x,v.y,v.z);
    for(int i=0;i<count;i++){Vector3 target=Mirror(vertices[i]);float best=.001f*.001f;int found=-1;foreach(int j in moving){float distance=(target-vertices[j]).sqrMagnitude;if(distance<best){best=distance;found=j;}}if(found<0)continue;right[i]=Mirror(delta[found]);rn[i]=Mirror(normals[found]);rt[i]=Mirror(tangents[found]);matched++;}
    if(matched<moving.Length*.8f)throw new Exception("Right hand mirror could not match source topology: "+matched+" / "+moving.Length);
    mesh.AddBlendShapeFrame("GripR",100,right,rn,rt);string path=Folder+"HeroHands.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);
    body.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);PrefabUtility.SaveAsPrefabAsset(root,Folder+"Hero.prefab");Debug.Log("Hand mirror matched "+matched+" vertices");
   }finally{PrefabUtility.UnloadPrefabContents(root);}
   AssetDatabase.SaveAssets();
  }
 }
}
