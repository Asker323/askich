using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class CharacterToolAuthor {
  const string Folder="Assets/Resources/Survival/Human/";
  static Material Material(string name,Color color,float metal){string path=Folder+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.SetFloat("_Metallic",metal);mat.SetFloat("_Glossiness",metal>0?.28f:.08f);EditorUtility.SetDirty(mat);return mat;}
  public static void Bake(){
   var wood=Material("ToolWood",new Color(.28f,.16f,.075f),0);var steel=Material("ToolSteel",new Color(.34f,.37f,.36f),.65f);
   foreach(string name in new[]{"Axe","Spear"}){
    bool axe=name=="Axe";var points=new List<Vector3>();var handle=new List<int>();var blade=new List<int>();
    void Quad(int a,int b,int c,int d,List<int> indices){indices.AddRange(new[]{a,b,c,a,c,d});}
    for(int end=0;end<2;end++)for(int side=0;side<8;side++){float a=side*Mathf.PI/4;points.Add(new Vector3(Mathf.Cos(a)*(axe?.021f:.018f),end==0?(axe?-.16f:-.52f):(axe?.48f:.89f),Mathf.Sin(a)*(axe?.016f:.018f)));}
    for(int side=0;side<8;side++)Quad(side,(side+1)%8,(side+1)%8+8,side+8,handle);
    for(int i=1;i<7;i++){handle.AddRange(new[]{0,i+1,i,8,8+i,8+i+1});}
    Vector2[] polygon=axe?new[]{new Vector2(-.035f,.36f),new Vector2(.09f,.33f),new Vector2(.25f,.28f),new Vector2(.27f,.53f),new Vector2(.10f,.49f),new Vector2(-.035f,.47f)}:new[]{new Vector2(-.035f,.86f),new Vector2(-.065f,.97f),new Vector2(0,1.18f),new Vector2(.065f,.97f),new Vector2(.035f,.86f)};
    int start=points.Count,n=polygon.Length;for(int face=0;face<2;face++)foreach(var p in polygon)points.Add(new Vector3(p.x,p.y,(face==0?-1:1)*(axe?.022f:.009f)));
    for(int i=1;i<n-1;i++)blade.AddRange(new[]{start,start+i+1,start+i,start+n,start+n+i,start+n+i+1});for(int i=0;i<n;i++)Quad(start+i,start+(i+1)%n,start+(i+1)%n+n,start+i+n,blade);
    var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.subMeshCount=2;mesh.SetTriangles(handle,0);mesh.SetTriangles(blade,1);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Folder+name+".asset";EditorUtility.CopySerialized(mesh,AssetDatabase.LoadAssetAtPath<Mesh>(path));Object.DestroyImmediate(mesh);
   }
   var root=PrefabUtility.LoadPrefabContents(Folder+"Hero.prefab");try{foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))if(renderer.name=="Axe"||renderer.name=="Spear")renderer.sharedMaterials=new[]{wood,steel};PrefabUtility.SaveAsPrefabAsset(root,Folder+"Hero.prefab");}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
 }
}
