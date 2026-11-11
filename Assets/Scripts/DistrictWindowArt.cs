using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Askich {
 // Metre-scale window dressing. Broken glass never deletes the joinery.
 public sealed class DistrictWindowArt:MonoBehaviour {
  public Transform Leaf {get;private set;}
  public GameObject Glass {get;private set;}
  public GameObject Fragments {get;private set;}
  public int BurstCount {get;private set;}
  public bool GlassShaderReady=>glassMaterial&&glassMaterial.shader.isSupported;
  readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
  readonly List<Transform> curtains=new List<Transform>(),chips=new List<Transform>();
  Material glassMaterial;int lastState;float clock,burst=-1;
  Material Material(string shader,Color color){var m=new Material(Resources.Load<Shader>("Survival/"+shader)){color=color};materials.Add(m);return m;}
  GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;Destroy(g.GetComponent<Collider>());g.transform.SetParent(parent?parent:transform,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
  GameObject MeshObject(string name,Vector3[] verts,int[] triangles,Material mat,Transform parent=null,Vector2[] uv=null){var mesh=new Mesh{name=name};mesh.vertices=verts;mesh.triangles=triangles;if(uv!=null)mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent?parent:transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;}
  GameObject Pane(string name,float bottom,float top,float left,float right,Transform parent){return MeshObject(name,new[]{new Vector3(.02f,bottom,left),new Vector3(.02f,top,left),new Vector3(.02f,top,right),new Vector3(.02f,bottom,right)},new[]{0,1,2,0,2,3},glassMaterial,parent,new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});}
  public void Build(bool workshop){
   var wood=Material("WorldSurface",workshop?new Color(.36f,.40f,.35f):new Color(.64f,.63f,.52f));wood.SetFloat("_Surface",1);var dark=Material("WorldSurface",new Color(.21f,.24f,.20f));dark.SetFloat("_Surface",1);
   glassMaterial=Material("WindowGlass",new Color(.40f,.57f,.59f,.25f));var cloth=Material("WindowFabric",workshop?new Color(.57f,.51f,.40f):new Color(.66f,.59f,.47f));
   foreach(float z in new[]{-.06f,2.06f})Box("Window casing",new Vector3(-.01f,1.49f,z),new Vector3(.37f,1.49f,.13f),wood);
   Box("Window casing lintel",new Vector3(0,2.19f,1),new Vector3(.37f,.12f,2.25f),wood);Box("Worn projecting window sill",new Vector3(-.10f,.815f,1),new Vector3(.54f,.07f,2.30f),wood);
   Box("Window center mullion",new Vector3(0,1.49f,1),new Vector3(.15f,1.34f,.065f),wood);
   foreach(float z in new[]{.035f,.96f})Box("Fixed sash stile",new Vector3(0,1.48f,z),new Vector3(.11f,1.30f,.055f),wood);
   foreach(float y in new[]{.87f,2.10f,1.80f})Box("Fixed sash rail",new Vector3(0,y,.50f),new Vector3(.11f,.055f,.95f),wood);
   Pane("Old fixed glass",.90f,2.07f,.07f,.93f,transform);
   Leaf=new GameObject("Right window sash").transform;Leaf.SetParent(transform,false);Leaf.localPosition=new Vector3(0,0,2);
   foreach(float z in new[]{-.97f,-.035f})Box("Opening sash stile",new Vector3(0,1.48f,z),new Vector3(.11f,1.30f,.055f),wood,Leaf);
   foreach(float y in new[]{.87f,2.10f})Box("Opening sash rail",new Vector3(0,y,-.50f),new Vector3(.11f,.055f,.95f),wood,Leaf);
   Glass=Pane("Breakable thin window glass",.90f,2.07f,-.93f,-.07f,Leaf);
   Box("Small window latch",new Vector3(-.075f,1.42f,-.92f),new Vector3(.05f,.13f,.035f),dark,Leaf);
   Fragments=new GameObject("Glass remaining in sash");Fragments.transform.SetParent(Leaf,false);
   for(int i=0;i<8;i++){float z=-.91f+i*.115f;MeshObject("Jagged top glass",new[]{new Vector3(.02f,2.07f,z),new Vector3(.02f,2.07f,z+.11f),new Vector3(.02f,1.94f+(i%3)*.025f,z+.035f)},new[]{0,1,2},glassMaterial,Fragments.transform,new[]{Vector2.zero,Vector2.right,Vector2.one});}
   foreach(float z in new[]{-.93f,-.07f})for(int i=0;i<3;i++){float y=1.02f+i*.27f;MeshObject("Jagged edge glass",new[]{new Vector3(.02f,y,z),new Vector3(.02f,y+.19f,z),new Vector3(.02f,y+.09f,z+(z<-.5f?.09f:-.09f))},new[]{0,1,2},glassMaterial,Fragments.transform,new[]{Vector2.zero,Vector2.up,Vector2.one});}
   Fragments.SetActive(false);
   Box("Curtain rod",new Vector3(-.30f,2.33f,1),new Vector3(.055f,.055f,2.65f),dark);
   for(int side=0;side<2;side++){
    var verts=new List<Vector3>();var tris=new List<int>();const int cols=48,rows=24;float sign=side==0?1:-1;
    for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){float u=x/(float)cols,v=y/(float)rows;float width=Mathf.Lerp(.43f,.27f,Mathf.Sin(v*Mathf.PI));float z=(side==0?-.20f:2.20f)+sign*u*width;verts.Add(new Vector3(-.29f-Mathf.Sin(u*Mathf.PI*8)*(.035f+.025f*v),2.29f-1.61f*v+Mathf.Sin(u*25)*.012f*v,z));}
    for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){int a=y*(cols+1)+x;tris.AddRange(new[]{a,a+cols+1,a+1,a+1,a+cols+1,a+cols+2});}
    var curtain=MeshObject("Gathered window curtain",verts.ToArray(),tris.ToArray(),cloth);curtains.Add(curtain.transform);
    Box("Curtain tie",new Vector3(-.30f,1.30f,side==0?-.04f:2.04f),new Vector3(.10f,.045f,.27f),dark);
   }
   for(int i=0;i<18;i++){var chip=MeshObject("Fallen glass shard",new[]{Vector3.zero,new Vector3(.06f+i%3*.025f,0,.02f),new Vector3(.025f,0,.08f)},new[]{0,1,2},glassMaterial);chips.Add(chip.transform);chip.SetActive(false);}
  }
  public void SetState(int state,float angle,bool animate){
   Leaf.localRotation=Quaternion.Euler(0,-angle,0);Glass.SetActive(state!=2);Fragments.SetActive(state==2);
   if(state==2&&lastState!=2){if(animate){burst=0;BurstCount++;}else burst=-1;}
   if(state!=2)burst=-1;lastState=state;for(int i=0;i<chips.Count;i++){chips[i].gameObject.SetActive(state==2);PlaceChip(i);} // Also places restored shards deterministically.
  }
  void PlaceChip(int i){float endX=(i%2==0?-1:1)*(.30f+i%5*.16f),endZ=1.18f+(i*.317f)%1.05f;float t=burst<0?1:Mathf.Clamp01(burst/.65f);chips[i].localPosition=Vector3.Lerp(new Vector3(.02f,1.45f,1.55f),new Vector3(endX,endX<0?.172f:.045f,endZ),t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.22f;chips[i].localRotation=Quaternion.Euler((1-t)*i*43,i*61,(1-t)*i*29);}
  void Update(){if(AudioListener.pause)return;float dt=Mathf.Min(Time.deltaTime,.25f);clock+=dt;for(int i=0;i<curtains.Count;i++)curtains[i].localPosition=Vector3.right*(Mathf.Sin(clock*1.4f+i*2)* (lastState==0?.002f:.014f));if(burst>=0){burst+=dt;if(burst>.65f)burst=-1;for(int i=0;i<chips.Count;i++)PlaceChip(i);}}
  void OnDestroy(){foreach(var mesh in meshes)Destroy(mesh);foreach(var mat in materials)Destroy(mat);}
 }
}
