using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  readonly List<Mesh> environmentArtMeshes=new List<Mesh>();
  void BuildEnvironmentArt(){
   var vertices=new List<Vector3>();var triangles=new List<int>();var colours=new List<Color>();
   void Kerb(float x,float z){
    int start=vertices.Count;var section=new[]{new Vector2(-.16f,.02f),new Vector2(-.16f,.13f),new Vector2(-.10f,.18f),new Vector2(.16f,.18f),new Vector2(.16f,.02f)};
    for(int end=0;end<2;end++)foreach(var s in section){vertices.Add(new Vector3(x+(end==0?-.46f:.46f),s.y,z+s.x));colours.Add(Color.white);}
    for(int i=0;i<5;i++){int j=(i+1)%5;triangles.AddRange(new[]{start+i,start+j,start+i+5,start+j,start+j+5,start+i+5});}
    for(int i=1;i<4;i++){triangles.AddRange(new[]{start,start+i+1,start+i,start+5,start+5+i,start+6+i});}
   }
   for(int x=-22;x<=22;x++)if(Mathf.Abs(x+12)>1.5f&&Mathf.Abs(x-12)>1.5f&&Mathf.Abs(x)>4){Kerb(x,-20.2f);Kerb(x,-27.8f);}
   environmentArtMeshes.Add(MakeStreetMesh("Weathered kerb stones",vertices,triangles,colours,SurfaceFor("Stone kerb",new Color(.55f,.55f,.50f))));
   foreach(int id in new[]{5,6}){
    var h=homes[id];DressRoofDrainage(h,id);var wood=new Color(.28f,.23f,.18f);
    Cube("Interior timber skirting north",V(h.p+Vector2.up*3.78f,.25f),new Vector3(7.6f,.18f,.07f),wood);
    Cube("Interior timber skirting east",V(h.p+Vector2.right*3.78f,.25f),new Vector3(.07f,.18f,7.6f),wood);
    for(int side=-1;side<=1;side+=2){Cube("Interior timber skirting front",V(h.p+new Vector2(side*2.55f,-3.78f),.25f),new Vector3(2.5f,.18f,.07f),wood);}
    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Cube("Table timber leg",V(h.p+new Vector2(-2+x*.64f,z*.49f),.40f),new Vector3(.11f,.48f,.11f),wood);
    Cube("Table timber apron",V(h.p+new Vector2(-2,0),.59f),new Vector3(1.4f,.12f,1.1f),wood);
   }
  }
  void BuildFrontFacade(Home h,int id,Color tint){
   Vector2 p=h.p;
   for(int side=-1;side<=1;side+=2){
    float x=side*2.5f;
    if(id==5||id==6){
     // Keep the full solid footprint: these are fixed windows, not extra exits.
     Wall("Front window sill wall",p+new Vector2(x,-4),new Vector2(3,.3f),.8f,tint,h,true);
     for(int edge=-1;edge<=1;edge+=2)Wall("Front window pier wall",p+new Vector2(x+edge*1.075f,-4),new Vector2(.85f,.3f),2.8f,tint,h,true);
     h.cut.Add(Cube("Front window lintel wall",V(p+new Vector2(x,-4),2.5f),new Vector3(1.3f,.6f,.3f),tint));
     var frame=new GameObject("Fixed glazed facade window");frame.transform.position=V(p+new Vector2(x-.65f,-4));frame.transform.rotation=Quaternion.Euler(0,90,0);frame.transform.localScale=new Vector3(1,1,.65f);frame.AddComponent<DistrictWindowArt>().Build(id==6);h.cut.Add(frame);
    }else{
     Wall("Front wall",p+new Vector2(x,-4),new Vector2(3,.3f),2.8f,tint,h,true);
     h.cut.Add(Cube("Window trim",V(p+new Vector2(x,-4.18f),1.65f),new Vector3(1.45f,1.2f,.12f),new Color(.8f,.78f,.65f)));
     h.cut.Add(Cube("Front window glass",V(p+new Vector2(x,-4.25f),1.65f),new Vector3(1.2f,.95f,.05f),new Color(.24f,.39f,.42f)));
     h.cut.Add(Cube("Window mullion",V(p+new Vector2(x,-4.29f),1.65f),new Vector3(.06f,1,.06f),new Color(.8f,.78f,.65f)));
    }
   }
  }

  void DressRoofDrainage(Home h,int id){
   // Attached to the roof/cutaway so pipes never hang across the interior view.
   var metal=new Color(.27f,.29f,.27f);
   float height=id==5?2.9f:3.35f;
   for(int side=-1;side<=1;side+=2){
    var gutter=Cube("Roof rain gutter",new Vector3(0,height,-side*4.30f),new Vector3(8.65f,.11f,.14f),metal,h.roof.transform);
    if(id==6)gutter.transform.localRotation=Quaternion.Euler(0,0,7);
   }
   float drainHeight=id==5?2.85f:2.82f;
   var pipe=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pipe.name="Exterior downpipe";Destroy(pipe.GetComponent<Collider>());pipe.transform.position=V(h.p+new Vector2(-3.75f,-4.28f),drainHeight*.5f);pipe.transform.localScale=new Vector3(.09f,drainHeight*.5f,.09f);pipe.GetComponent<Renderer>().sharedMaterial=SurfaceFor("Roof drain",metal);h.cut.Add(pipe);
   foreach(float y in new[]{.7f,2.1f})h.cut.Add(Cube("Downpipe wall strap",V(h.p+new Vector2(-3.75f,-4.25f),y),new Vector3(.16f,.04f,.16f),metal));
   var chimney=new Vector3(id%2==0?2:-2,4.80f,1+id%3*.45f);
   Cube("Roof chimney coping",chimney,new Vector3(.73f,.10f,.73f),new Color(.28f,.28f,.24f),h.roof.transform);
   Cube("Soot dark chimney opening",chimney+Vector3.up*.055f,new Vector3(.40f,.012f,.40f),new Color(.07f,.07f,.065f),h.roof.transform);
  }

 }
}
