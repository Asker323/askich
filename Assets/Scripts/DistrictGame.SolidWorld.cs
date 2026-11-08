using System;
using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  sealed class SolidProp {public Rect rect;public string name;}
  readonly Dictionary<Vector2Int,List<SolidProp>> solidCells=new Dictionary<Vector2Int,List<SolidProp>>();
  readonly List<SolidProp> solidProps=new List<SolidProp>();
  void BuildSolidWorld(){
   solidCells.Clear();solidProps.Clear();
   foreach(var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None)){
    var bounds=renderer.bounds;string name=renderer.name.ToLowerInvariant();if(name=="tapered trunk and branches")bounds=new Bounds(renderer.transform.position+Vector3.up*.65f,new Vector3(.42f,1.3f,.42f));var size=bounds.size;
    bool furniture=name.Contains("table")||name.Contains("desk")||name.Contains("bed")||name.Contains("couch")||name.Contains("mattress")||name.Contains("counter")||name.Contains("workbench");
    if(bounds.min.y>(furniture?1.2f:.85f)||bounds.max.y<.30f||bounds.center.y>2.4f||name.Contains("wall")||name.Contains("door")||name.Contains("roof")||name.Contains("window")||name.Contains("floor")||name.Contains("rug")||name.Contains("terrain")||name.Contains("canopy")||name.Contains("grass"))continue;
    bool post=name.Contains("trunk")||name.Contains("post")||name.Contains("column");
    if(!(furniture&&size.x>.4f&&size.z>.4f||post&&size.y>.7f&&size.x>.08f&&size.z>.08f||size.y>.34f&&size.x>.34f&&size.z>.34f))continue;
    var solid=new SolidProp{name=renderer.name,rect=new Rect(bounds.min.x-.28f,bounds.min.z-.28f,size.x+.56f,size.z+.56f)};solidProps.Add(solid);
    for(int x=Mathf.FloorToInt(solid.rect.xMin);x<=Mathf.FloorToInt(solid.rect.xMax);x++)for(int z=Mathf.FloorToInt(solid.rect.yMin);z<=Mathf.FloorToInt(solid.rect.yMax);z++){var key=new Vector2Int(x,z);if(!solidCells.TryGetValue(key,out var bucket))solidCells[key]=bucket=new List<SolidProp>();bucket.Add(solid);}
   }
  }
  bool SolidBlocked(Vector2 p){if(!solidCells.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y)),out var bucket))return false;foreach(var prop in bucket)if(prop.rect.Contains(p))return true;return false;}
  bool SightBlocked(Vector2 p){if(!InsideWorld(p))return true;foreach(var b in barriers)if(b.active&&b.blocksSight&&new Rect(b.rect.x-.28f,b.rect.y-.28f,b.rect.width+.56f,b.rect.height+.56f).Contains(p))return true;return false;}
  readonly struct PathNode:IComparable<PathNode> {
   public readonly Vector2Int cell;public readonly int cost,priority,order;
   public PathNode(Vector2Int cell,int cost,int priority,int order){this.cell=cell;this.cost=cost;this.priority=priority;this.order=order;}
   public int CompareTo(PathNode other){int result=priority.CompareTo(other.priority);return result!=0?result:order.CompareTo(other.order);}
  }
  List<Vector2> FindPath(Vector2 start,Vector2 end){
   const float cellSize=.5f;var source=Vector2Int.RoundToInt(start/cellSize);var target=Vector2Int.RoundToInt(end/cellSize);
   var empty=new List<Vector2>();if(Blocked((Vector2)target*cellSize))return empty;
   var open=new SortedSet<PathNode>();var cost=new Dictionary<Vector2Int,int>();var previous=new Dictionary<Vector2Int,Vector2Int>();int order=0;
   int Distance(Vector2Int cell)=>Mathf.Abs(cell.x-target.x)+Mathf.Abs(cell.y-target.y);
   open.Add(new PathNode(source,0,Distance(source),order++));cost[source]=0;
   Vector2Int[] directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
   for(int visited=0;open.Count>0&&visited<12000;visited++){
    var node=open.Min;open.Remove(node);if(cost[node.cell]!=node.cost)continue;
    if(node.cell==target){var result=new List<Vector2>();var cell=target;while(cell!=source){result.Add((Vector2)cell*cellSize);cell=previous[cell];}result.Reverse();return result;}
    foreach(var direction in directions){var next=node.cell+direction;int steps=node.cost+1;if(cost.TryGetValue(next,out var known)&&known<=steps||Blocked((Vector2)next*cellSize)||Blocked(((Vector2)node.cell+(Vector2)next)*(.5f*cellSize)))continue;cost[next]=steps;previous[next]=node.cell;open.Add(new PathNode(next,steps,steps+Distance(next),order++));}
   }
   return empty;
  }
  bool ContainerReachable(Home home,int slot){
   Vector2 target=home.p+home.containers[slot];
   for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++){var approach=(Vector2)Vector2Int.RoundToInt(target*2)*.5f+new Vector2(x,z)*.5f;if(Vector2.Distance(approach,target)>=1.6f||Blocked(approach)||!Sight(approach,target))continue;if(FindPath(home.p+Vector2.down*5,approach).Count>0)return true;}
   return false;
  }
  Home RoomAt(Vector2 p)=>homes.Find(h=>Mathf.Abs(p.x-h.p.x)<3.8f&&Mathf.Abs(p.y-h.p.y)<3.8f);
  Vector2 ResolveSolidPosition(Vector2 position){
   position=ResolveStreetPosition(position);if(!Blocked(position))return position;var room=RoomAt(position);
   for(float radius=.2f;radius<=4;radius+=.2f)for(int i=0;i<32;i++){float angle=i*Mathf.PI/16;var candidate=position+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;if(RoomAt(candidate)==room&&!Blocked(candidate))return candidate;}
   return new Vector2(-12,-20);
  }
 }
}
