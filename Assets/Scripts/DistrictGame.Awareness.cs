using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  enum Awareness {Roam,Investigate,Chase,Search}
  static float LegacyEnemyHealth(int id)=>id%3==1?90:id%3==2?40:65;
  static float EnemyDamage(Person z)=>z.id%3==1?34:z.id%3==2?20:26;
  bool CanSeeHero(Person z){
   Vector2 offset=hero.p-z.p;float distance=offset.magnitude;
   if(distance>(crouching?4:11)||!Sight(z.p,hero.p))return false;
   Vector3 forward=z.root.forward;return distance<1.8f||Vector2.Dot(offset.normalized,new Vector2(forward.x,forward.z))>.15f;
  }
  void HearNoise(Vector2 source,float radius){
   foreach(var z in infected){
    if(z.hp<=0||Vector2.Distance(z.p,source)>=radius)continue;
    // A visible target takes priority over distractions; hidden targets are never tracked.
    if(z.awareness==Awareness.Chase&&CanSeeHero(z))continue;
    z.awareness=Awareness.Investigate;z.goal=source;z.searchOrigin=source;z.alert=14;z.repath=0;z.unreachable=0;
   }
  }
  void StartSearching(Person z){z.awareness=Awareness.Search;z.searchClock=6;z.path.Clear();z.alert=6;}
  void ObserveEnemy(Person z,float dt){
   bool sees=CanSeeHero(z);
   if(sees){z.awareness=Awareness.Chase;z.goal=hero.p;z.searchOrigin=hero.p;z.alert=8;z.unreachable=0;return;}
   if(z.awareness==Awareness.Chase){z.awareness=Awareness.Investigate;z.goal=z.searchOrigin;z.alert=10;z.repath=0;}
   z.alert=Mathf.Max(0,z.alert-dt);
   if(z.awareness==Awareness.Search){
    z.searchClock-=dt;z.root.Rotate(0,65*dt,0);
    if(z.searchClock<=0){z.awareness=Awareness.Roam;z.alert=0;z.path.Clear();}return;
   }
   if(z.awareness==Awareness.Investigate&&(Vector2.Distance(z.p,z.goal)<.9f||z.alert<=0||z.unreachable>2.5f))StartSearching(z);
  }
  void TickInfected(float dt){
   foreach(var z in infected){
    if(z.hp<=0)continue;
    if(Vector2.Distance(z.p,hero.p)>40&&z.awareness==Awareness.Roam)continue;
    z.rest-=dt;z.repath-=dt;ObserveEnemy(z,dt);
    if(TickEnemyAttack(z,dt))continue;
    float distance=Vector2.Distance(z.p,hero.p);
    if(distance<(IsCrawler(z)?CrawlerRange:1.35f)&&z.rest<=0&&CanSeeHero(z)){BeginEnemyAttack(z);continue;}
    Vector2 old=z.p;
    if(z.awareness==Awareness.Search||(z.awareness==Awareness.Chase&&distance<.9f)){Animate(z,Vector2.zero,true,0);continue;}
    if(z.awareness==Awareness.Chase||z.awareness==Awareness.Investigate){
     if(z.repath<=0){z.path=FindPath(z.p,z.goal);z.repath=.65f+(z.id%5)*.08f;}
     if(z.path.Count>0){Vector2 target=z.path[0];if(Vector2.Distance(z.p,target)<.25f)z.path.RemoveAt(0);else z.p=MoveAmongActors(z,(target-z.p).normalized*dt*z.speed,true);}
     z.unreachable=(z.p-old).sqrMagnitude<.000001f?z.unreachable+dt:0;
    }else{
     Vector2 roam=new Vector2(Mathf.Sin(Time.time*.14f+z.phase),Mathf.Cos(Time.time*.1f+z.phase));z.p=MoveAmongActors(z,roam*dt*.35f,true);
    }
    Animate(z,z.p-old,true,0);
   }
  }
 }
}
