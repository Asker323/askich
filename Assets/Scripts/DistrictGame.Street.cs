using System;
using System.Collections.Generic;
using UnityEngine;

namespace Askich {
 public sealed partial class DistrictGame {
  readonly List<Rect> streetCover=new List<Rect>();
  Material streetMaterial,grassMaterial;
  Mesh streetMesh,grassMesh;
  void BuildStreet(){
   streetMaterial=new Material(Resources.Load<Shader>("Survival/StreetSurface"));
   grassMaterial=new Material(Resources.Load<Shader>("Survival/VergeGrass"));
   var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
   var random=new System.Random(726);
   float Rand(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
   void Patch(Vector2 center,Vector2 size,Color color,float height,int sides=8){
    int index=vertices.Count;vertices.Add(V(center,height));colors.Add(color);
    for(int i=0;i<sides;i++){
     float a=i*Mathf.PI*2/sides;float radius=Rand(.8f,1.1f);
     vertices.Add(V(center+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y)*radius,height));
     colors.Add(color*.9f);
    }
    for(int i=0;i<sides;i++){triangles.Add(index);triangles.Add(index+1+(i+1)%sides);triangles.Add(index+1+i);}
   }
   // All ground decoration is authored in the clear stretch between the two houses.
   // Jagged dark strips follow the road instead of drawing a uniform grid.
   for(int i=0;i<22;i++){
    var p=new Vector2(Rand(-21,21),Rand(-26.7f,-21.3f));
    for(int n=0;n<3;n++){
     Patch(p,new Vector2(Rand(.15f,.45f),.025f),new Color(.14f,.15f,.145f),.028f,4);
     p+=new Vector2(.28f,Rand(-.13f,.13f));
    }
   }
   for(int i=0;i<18;i++)Patch(new Vector2(Rand(-20,20),Rand(-19,-17)),new Vector2(.1f,.08f),new Color(.43f,.41f,.33f),.09f,5);
   streetMesh=MakeStreetMesh("Road wear and gravel",vertices,triangles,colors,streetMaterial);

   vertices.Clear();colors.Clear();triangles.Clear();var uv=new List<Vector2>();
   for(int i=0;i<420;i++){
    float x=Rand(-21,21),z=i%2==0?Rand(-19.7f,-17):Rand(-30,-28);
    if(new Rect(-10.1f,-20.8f,6.2f,2.9f).Contains(new Vector2(x,z)))continue;
    if(Mathf.Abs(x+12)<1.4f||Mathf.Abs(x-12)<1.4f||Mathf.Abs(x)<4)continue;
    float height=Rand(.12f,.32f);Color tint=new Color(Rand(.8f,1.2f),Rand(.8f,1.1f),Rand(.7f,1));
    for(int blade=0;blade<3;blade++){
     int index=vertices.Count;float a=Rand(0,Mathf.PI*2);
     var side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.038f;
     vertices.Add(new Vector3(x,.08f,z)-side);vertices.Add(new Vector3(x,.08f,z)+side);vertices.Add(new Vector3(x+.07f,height,z+.03f));
     triangles.Add(index);triangles.Add(index+1);triangles.Add(index+2);
     colors.Add(tint*.65f);colors.Add(tint*.65f);colors.Add(tint);
     uv.Add(Vector2.zero);uv.Add(Vector2.zero);uv.Add(Vector2.up);
    }
   }
   grassMesh=MakeStreetMesh("Low verge grass",vertices,triangles,colors,grassMaterial);grassMesh.SetUVs(0,uv);
   grassMesh.bounds=new Bounds(new Vector3(0,.4f,-23.5f),new Vector3(46,2,17));
   Fence(new Vector2(-17,-18.5f),3.5f);
   Fence(new Vector2(17,-18.5f),3.5f);
   // Existing parked vehicles retain their established collision rectangles.
   for(int i=0;i<5;i++)Cube("Discarded roadside board",V(new Vector2(-19+i*.32f,-20.4f),.12f),new Vector3(.1f,.07f,.8f),new Color(.34f,.28f,.2f)).transform.rotation=Quaternion.Euler(0,15+i*7,0);
   DistrictAtmosphere.Point("Radio approach sky bounce",new Vector3(-12,2.8f,-20.5f),new Color(.59f,.7f,.83f),.7f,10);
   DistrictAtmosphere.Point("Clinic approach sky bounce",new Vector3(12,2.8f,-20.5f),new Color(.59f,.7f,.83f),.7f,10);
  }
  Mesh MakeStreetMesh(string name,List<Vector3> vertices,List<int> triangles,List<Color> colors,Material material){
   var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();
   var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
   return mesh;
  }
  void Fence(Vector2 center,float width){
   var rect=new Rect(center-new Vector2(width/2,.10f),new Vector2(width,.2f));streetCover.Add(rect);barriers.Add(new Barrier{rect=rect});
   for(int i=0;i<8;i++){
    float x=center.x-width/2+i*width/7;
    Cube("Splintered fence board",V(new Vector2(x,center.y),.5f),new Vector3(.26f,.9f-(i%3)*.08f,.12f),new Color(.32f,.28f,.21f));
   }
   for(int i=0;i<2;i++)Cube("Fence cross rail",V(center,.25f+i*.4f),new Vector3(width,.10f,.15f),new Color(.28f,.24f,.18f));
  }
  Vector2 ResolveStreetPosition(Vector2 position){
   foreach(var cover in streetCover){
    var expanded=new Rect(cover.x-.28f,cover.y-.28f,cover.width+.56f,cover.height+.56f);
    if(!expanded.Contains(position))continue;
    float side=position.y>=cover.center.y?1:-1;
    foreach(float offset in new[]{side*.85f,-side*.85f}){
     var candidate=position+Vector2.up*offset;
     if(!Blocked(candidate))return candidate;
    }
   }
   return position;
  }
  void ReleaseStreet(){
   foreach(var mesh in environmentArtMeshes)if(mesh)Destroy(mesh);
   foreach(var obj in new UnityEngine.Object[]{streetMesh,grassMesh,streetMaterial,grassMaterial})if(obj)Destroy(obj);
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  void CheckStreet(Action<bool,string> check){
   check(streetMaterial.shader.isSupported&&grassMaterial.shader.isSupported,"street and animated grass shaders included in Windows build");
   check(grassMesh.vertexCount<4000&&streetMesh.vertexCount<2000,"street decoration fits two compact meshes");
   check(streetCover.Count==2&&Blocked(streetCover[0].center)&&!Sight(streetCover[0].center+Vector2.up,streetCover[0].center-Vector2.up),"wooden cover blocks movement and enemy sight");
   check(FindPath(new Vector2(-12,-20),new Vector2(-14,-20)).Count>0&&FindPath(new Vector2(-12,-20),new Vector2(-12,-17)).Count>0,"starting stones and radio doorstep remain reachable");
   var overlap=streetCover[0].center;
   var resolved=ResolveStreetPosition(overlap);
   check(!Blocked(resolved)&&Vector2.Distance(resolved,overlap)<1,"legacy positions overlapping new fence move less than one metre to clear ground");
   string snapshot=JsonUtility.ToJson(CaptureSave());
   hero.p=overlap;var enemy=infected[0];enemy.hp=enemy.maxHp;enemy.p=overlap+Vector2.left*.4f;
   Save();hero.p=Vector2.zero;
   check(Load()&&!Blocked(hero.p)&&!Blocked(enemy.p)&&Vector2.Distance(hero.p,overlap)<1,"saved survivor and living enemy load beside newly added fence");
   System.IO.File.WriteAllText(SavePath,snapshot);
   check(Load(),"world snapshot reloads after street migration scenario");
   check(FindPath(new Vector2(-12,-24),new Vector2(12,-24)).Count>0&&FindPath(new Vector2(12,-24),new Vector2(12,-17)).Count>0,"street crossing and clinic approach remain reachable");
  }
#endif
 }
}
