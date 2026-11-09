using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  const float BodySpacing=.64f;
  bool ActorSpaceFree(Person actor,Vector2 from,Vector2 to){
   bool Clear(Vector2 other){float before=(from-other).sqrMagnitude,after=(to-other).sqrMagnitude;return after>=BodySpacing*BodySpacing||after>before+.000001f;}
   if(actor!=hero&&Climbing&&(!Clear(climbFrom)||!Clear(climbTo)))return false;
   if(actor!=hero&&!Clear(hero.p))return false;
   foreach(var other in infected)if(other!=actor&&other.hp>0&&!Clear(other.p))return false;
   return true;
  }
  Vector2 MoveAmongActors(Person actor,Vector2 delta,bool steer){
   var p=actor.p;int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.12f));var step=delta/steps;
   for(int i=0;i<steps;i++){
    var target=Move(p,step);
    if(ActorSpaceFree(actor,p,target)){p=target;continue;}
    if(!steer){var x=Move(p,new Vector2(step.x,0));if(ActorSpaceFree(actor,p,x))p=x;var y=Move(p,new Vector2(0,step.y));if(ActorSpaceFree(actor,p,y))p=y;continue;}
    // Try both shoulders rather than allowing every pursuer to occupy the same grid point.
    float sign=actor.id%2==0?1:-1;
    for(int turn=0;turn<4;turn++){
     float angle=(turn<2?60:90)*(turn%2==0?sign:-sign);
     float radians=angle*Mathf.Deg2Rad;var side=new Vector2(step.x*Mathf.Cos(radians)-step.y*Mathf.Sin(radians),step.x*Mathf.Sin(radians)+step.y*Mathf.Cos(radians));target=Move(p,side);
     if((target-p).sqrMagnitude>.000001f&&ActorSpaceFree(actor,p,target)){p=target;break;}
    }
   }
   return p;
  }
 }
}
