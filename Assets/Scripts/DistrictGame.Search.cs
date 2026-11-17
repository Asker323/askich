using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  bool NextSearchPoint(Person z){
   // Search only remembered ground. Never use the hidden survivor's position here.
   int step=z.searchStep++;
   for(int attempt=0;attempt<6;attempt++){
    float angle=z.id*.83f+step*2.399963f+attempt*Mathf.PI/3;
    float radius=1.5f+(attempt%3)*.6f;
    Vector2 candidate=(Vector2)Vector2Int.RoundToInt((z.searchOrigin+new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*radius)*2)*.5f;
    if(Blocked(candidate)||Vector2.Distance(candidate,z.p)<.85f)continue;
    // A small local search must not launch several full-world path queries.
    var path=FindPath(z.p,candidate,128);
    if(path.Count==0||path.Count>14||path.Exists(p=>Vector2.Distance(p,z.searchOrigin)>4))continue;
    z.goal=candidate;z.path=path;z.unreachable=0;return true;
   }
   z.path.Clear();return false;
  }
  void TickSearchMovement(Person z,float dt){
   if(z.searchPause>0){z.searchPause=Mathf.Max(0,z.searchPause-dt);return;}
   if(z.path.Count==0){
    if(z.searchStep>=3||!NextSearchPoint(z)){z.searchPause=.65f;return;}
   }
   if(Vector2.Distance(z.p,z.path[0])<.2f){
    z.path.RemoveAt(0);
    if(z.path.Count==0){z.searchPause=.55f;z.unreachable=0;}
    return;
   }
   Vector2 before=z.p,offset=z.path[0]-z.p;
   z.p=MoveAmongActors(z,Vector2.ClampMagnitude(offset,dt*z.speed*.65f),true);
   z.unreachable=(z.p-before).sqrMagnitude<.000001f?z.unreachable+dt:0;
   if(z.unreachable>.75f){z.path.Clear();z.searchPause=.55f;z.unreachable=0;}
  }
 }
}
