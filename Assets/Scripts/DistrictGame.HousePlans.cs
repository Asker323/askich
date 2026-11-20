using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  sealed class BackExit {public GameObject[] boardArt;public int boards;public Home home;public int bit;public bool open;public float angle;public Barrier gate;public Transform leaf;public Vector2 p;}
  readonly List<BackExit> backExits=new List<BackExit>();
  static bool HasHousePlan(int id)=>id==5||id==6;
  void BuildNorthWall(Home h,int id,Color tint){
   if(!HasHousePlan(id)){Wall("North wall",h.p+Vector2.up*4,new Vector2(8,.3f),2.8f,tint);return;}
   for(int side=-1;side<=1;side+=2)Wall("Rear wall section",h.p+new Vector2(side*2.65f,4),new Vector2(2.7f,.3f),2.8f,tint);
   Cube("Rear door lintel",V(h.p+Vector2.up*4,2.66f),new Vector3(2.6f,.28f,.3f),tint);
   var exit=new BackExit{home=h,bit=id==5?1:2,p=h.p+Vector2.up*4};exit.gate=new Barrier{soundLoss=3,rect=new Rect(exit.p-new Vector2(1.3f,.2f),new Vector2(2.6f,.4f))};barriers.Add(exit.gate);
   exit.leaf=new GameObject("Back door hinge").transform;exit.leaf.position=V(exit.p+Vector2.right*1.3f);
   Color wood=new Color(.28f,.31f,.25f);Cube("Back door leaf",new Vector3(-1.3f,1.2f,0),new Vector3(2.6f,2.4f,.13f),wood,exit.leaf);
   for(int n=0;n<6;n++)Cube("Back door plank seam",new Vector3(-2.4f+n*.43f,1.2f,-.075f),new Vector3(.025f,2.25f,.015f),wood*.7f,exit.leaf);
   Cube("Back door latch",new Vector3(-2.25f,1.2f,-.12f),new Vector3(.23f,.06f,.11f),new Color(.46f,.45f,.35f),exit.leaf);
   for(int side=-1;side<=1;side+=2)Cube("Back door frame",V(exit.p+Vector2.right*side*1.36f,1.25f),new Vector3(.12f,2.5f,.25f),wood*.8f);
   Cube("Back doorstep",V(exit.p+Vector2.up*.55f,.08f),new Vector3(2.8f,.16f,1.1f),new Color(.43f,.43f,.36f));Cube("Back yard worn path",V(exit.p+Vector2.up*2,.012f),new Vector3(1.4f,.025f,2.5f),new Color(.34f,.31f,.23f));backExits.Add(exit);BuildDoorBoards(exit);
  }
  void Partition(Home h,string name,Vector2 local,Vector2 size,Color tint){
   var p=h.p+local;barriers.Add(new Barrier{soundLoss=4,rect=new Rect(p-size/2,size)});
   Cube(name+" lower wall",V(p,.47f),new Vector3(size.x,.94f,size.y),tint);
   h.cut.Add(Cube(name+" upper wall",V(p,1.75f),new Vector3(size.x,1.62f,size.y),tint));
   Cube(name+" skirting",V(p,.19f),new Vector3(size.x+.035f,.13f,size.y+.035f),tint*.65f);
  }
  bool BuildPlannedInterior(Home h,int id){
   if(!HasHousePlan(id))return false;var cloth=new Color(.34f,.39f,.33f);var wood=new Color(.32f,.25f,.18f);
   void Part(string name,Vector2 local,float y,Vector3 size,Color c){Cube(name,V(h.p+local,y),size,c);}
   if(id==4){
    Partition(h,"Family bedroom partition",new Vector2(.8f,-2.5f),new Vector2(.15f,3),new Color(.45f,.44f,.35f));
    Partition(h,"Family pantry partition",new Vector2(.8f,2.1f),new Vector2(.15f,1.6f),new Color(.45f,.44f,.35f));
    Part("Family bed frame",new Vector2(2.5f,-2.4f),.34f,new Vector3(1.35f,.45f,2.2f),wood);Part("Rumpled family blanket",new Vector2(2.5f,-2.4f),.6f,new Vector3(1.30f,.14f,2),cloth);Part("Family bed pillow",new Vector2(2.5f,-1.65f),.72f,new Vector3(.9f,.12f,.4f),new Color(.61f,.60f,.51f));
    Part("Small child cot",new Vector2(3.05f,.25f),.45f,new Vector3(.8f,.6f,1.4f),wood);Part("Child cot blanket",new Vector2(3.05f,.25f),.78f,new Vector3(.75f,.1f,1.3f),new Color(.44f,.46f,.40f));
    for(int n=0;n<4;n++)Part("Abandoned child blocks",new Vector2(2.1f+n*.17f,1.1f),.22f,new Vector3(.12f,.12f,.12f),HouseColours[n]);
    Part("Family dining rug",new Vector2(-1.8f,-1.8f),.18f,new Vector3(2.5f,.02f,1.8f),new Color(.26f,.28f,.23f));
   }else{
    Partition(h,"Workshop sleeping nook",new Vector2(-2.6f,-.9f),new Vector2(2.8f,.15f),new Color(.38f,.40f,.35f));
    Partition(h,"Workshop entry screen",new Vector2(2.6f,-.9f),new Vector2(2.8f,.15f),new Color(.38f,.40f,.35f));
    Part("Carpenter workbench",new Vector2(-2.4f,-2.65f),.65f,new Vector3(2.5f,.95f,1),wood);for(int n=0;n<5;n++)Part("Workbench timber offcuts",new Vector2(-3.2f+n*.33f,-2.65f),1.17f,new Vector3(.18f,.08f,.65f),HouseColours[n]);
    Part("Workshop sleeping bed",new Vector2(2.5f,-2.4f),.35f,new Vector3(1.35f,.4f,2),cloth);Part("Workshop folded blanket",new Vector2(2.5f,-2.7f),.6f,new Vector3(1.3f,.10f,1.1f),new Color(.28f,.29f,.25f));
    Part("Worn workshop mat",new Vector2(.1f,.1f),.18f,new Vector3(1.5f,.02f,2.4f),new Color(.26f,.25f,.19f));
   }
   return true;
  }
  bool BackOccupied(BackExit e){var r=e.gate.rect;r.xMin-=.28f;r.xMax+=.28f;r.yMin-=.28f;r.yMax+=.28f;return r.Contains(hero.p)||infected.Exists(z=>z.hp>0&&r.Contains(z.p));}
  int CaptureBackExits(){int flags=0;foreach(var e in backExits)if(e.boards==0&&(e.open||BackOccupied(e)))flags|=e.bit;return flags;}
  void RestoreBackExits(int flags){foreach(var e in backExits){e.open=(flags&e.bit)!=0;e.angle=e.open?100:0;e.gate.active=!e.open;e.leaf.rotation=Quaternion.Euler(0,e.angle,0);}}
  BackExit NearbyBackExit(){
   foreach(var e in backExits){float distance=Vector2.Distance(hero.p,e.p);if(distance>=1.7f)continue;int slot=NearbyContainer(e.home);if(slot>=0&&Vector2.Distance(hero.p,e.home.p+e.home.containers[slot])<distance)continue;return e;}return null;
  }
  bool UseBackExit(){var e=NearbyBackExit();if(e==null)return false;if(e.open&&BackOccupied(e)){Say("Проём занят");return true;}if(e.boards>0){if(RoomAt(hero.p)!=e.home)StartFort(e,2);else Say("V · снять доски изнутри");return true;}e.open=!e.open;Alert(e.p,7);foley.Door(V(e.p,1),e.open);Save();return true;}
  void TickBackExits(float dt){foreach(var e in backExits){if(e.boards>0){RefreshBoards(e);continue;}bool occupied=BackOccupied(e);e.angle=Mathf.MoveTowards(e.angle,e.open||occupied?100:0,dt*220);e.gate.active=!occupied&&e.angle<(e.open?70:25);e.leaf.rotation=Quaternion.Euler(0,e.angle,0);}}
  void DrawBackExit(){var e=NearbyBackExit();if(e==null)return;var r=new Rect(330,478,620,64);Panel(r);GUI.Label(new Rect(r.x+12,r.y+6,r.width-24,26),e.boards>0?"Баррикада · "+e.boards+"/3 · E взлом снаружи":"E · "+(e.open?"закрыть выход во двор":"открыть выход во двор"),small);if(RoomAt(hero.p)==e.home)GUI.Label(new Rect(r.x+12,r.y+32,r.width-24,26),e.boards>0?"V · снять доски · вернётся 1 доска":"V · укрепить · 2 доски + проволока · 6 с",small);}
 }
}
