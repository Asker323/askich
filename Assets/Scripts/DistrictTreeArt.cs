using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 // Two meshes per tree: tapered branching wood and a canopy of small leaf clusters.
 public static class DistrictTreeArt {
  static Material bark,foliage;
  public static void Build(Vector3 position,int seed){
   if(!bark){bark=new Material(Shader.Find("Standard"));bark.color=new Color(.23f,.20f,.16f);bark.SetFloat("_Glossiness",0);foliage=new Material(Resources.Load<Shader>("Survival/Canopy"));}
   var root=new GameObject("Branching wind tree");root.transform.position=position;
   var random=new System.Random(seed*7919+37);float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
   var owned=new List<Mesh>();
   var wood=new List<Vector3>();var woodTriangles=new List<int>();
   var leaves=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
   void Branch(Vector3 a,Vector3 b,float ra,float rb){
    var axis=(b-a).normalized;var u=Vector3.Cross(axis,Vector3.forward).normalized;var v=Vector3.Cross(axis,u);int start=wood.Count;
    for(int end=0;end<2;end++)for(int j=0;j<7;j++){float t=j*Mathf.PI*2/7;wood.Add((end==0?a:b)+(u*Mathf.Cos(t)+v*Mathf.Sin(t))*(end==0?ra:rb));}
    for(int j=0;j<7;j++){int k=(j+1)%7;woodTriangles.AddRange(new[]{start+j,start+k,start+7+j,start+k,start+7+k,start+7+j});}
   }
   void Leaf(Vector3 p,Vector3 direction,float length,Color color){
    var d=direction.normalized*length;var side=Vector3.Cross(direction,new Vector3(.3f,1,.2f)).normalized*length*.38f;int n=leaves.Count;
    leaves.AddRange(new[]{p-d*.5f,p-side,p+Vector3.up*length*.13f,p+side,p+d*.5f});
    for(int j=0;j<5;j++)colors.Add(color*(j==2?1.12f:1));
    triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+1,n+4,n+2,n+2,n+4,n+3});
   }
   float height=R(4.2f,5.7f);Branch(Vector3.zero,new Vector3(.12f,height*.85f,.08f),.19f,.045f);
   for(int b=0;b<10;b++){
    float angle=b*2.39996f+R(-.25f,.25f),reach=R(1.1f,2.05f);var start=new Vector3(.06f,height*(.35f+b*.035f),.04f);
    var tip=new Vector3(Mathf.Cos(angle)*reach,height*(.62f+b*.028f),Mathf.Sin(angle)*reach);
    Branch(start,tip,.065f,.012f);
    for(int twig=0;twig<3;twig++){
     var end=tip+new Vector3(R(-.5f,.5f),R(.1f,.6f),R(-.5f,.5f));Branch(Vector3.Lerp(start,tip,.65f),end,.022f,.005f);
     for(int k=0;k<(Application.isMobilePlatform?12:23);k++){
      var offset=new Vector3(R(-.65f,.65f),R(-.4f,.55f),R(-.65f,.65f));
      var color=Color.Lerp(new Color(.20f,.27f,.15f),new Color(.38f,.40f,.21f),R(0,1));
      Leaf(end+offset,new Vector3(R(-1,1),R(-.4f,.6f),R(-1,1)),R(.25f,.46f),color);
     }
    }
   }
   void Show(string name,List<Vector3> vertices,List<int> indices,Material material,List<Color> tint=null){
    var mesh=new Mesh{name=name};owned.Add(mesh);mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);if(tint!=null)mesh.SetColors(tint);mesh.RecalculateNormals();mesh.RecalculateBounds();
    var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root.transform,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
   }
   Show("Tapered trunk and branches",wood,woodTriangles,bark);Show("Individual wind leaves",leaves,triangles,foliage,colors);
   root.AddComponent<DistrictTreeSway>().Phase=seed*.7f;root.AddComponent<DistrictTreeMeshes>().owned=owned.ToArray();
  }
 }
 public sealed class DistrictTreeMeshes:MonoBehaviour {
  public Mesh[] owned;
  void OnDestroy(){if(owned!=null)foreach(var mesh in owned)if(mesh)Destroy(mesh);}
 }
}
