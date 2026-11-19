using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  Home approachHome;bool approachTurning;int approachSlot;float approachClock;Vector2 approachCorner;
  Vector2 StorageStand(Home h,int slot)=>h.p+h.containers[slot]+Vector2.down*.95f;
  void ApproachStorage(Home h,int slot){
   if(approachHome!=null){CancelSearch();return;}
   Vector2 goal=StorageStand(h,slot);
   if(Blocked(goal)||Vector2.Distance(hero.p,goal)>2){Say("Подойди к передней стороне ящика");return;}
   approachCorner=goal;
   if(Vector2.Distance(Move(hero.p,goal-hero.p),goal)>.03f){
    approachCorner=new Vector2(hero.p.x,goal.y);
    if(Vector2.Distance(Move(hero.p,approachCorner-hero.p),approachCorner)>.03f||Vector2.Distance(Move(approachCorner,goal-approachCorner),goal)>.03f){Say("Подойди к передней стороне ящика");return;}
   }
   approachHome=h;approachTurning=false;approachSlot=slot;approachClock=0;
  }
  void TickStorageApproach(float dt,Vector2 input,ref Vector2 actualMove){
   if(approachHome==null)return;
   if(input.sqrMagnitude>0||pcAiming||suppliesOpen||journalOpen||attack>0||heroStagger>0||medicalAction!=MedicalAction.None||Climbing){CancelSearch();return;}
   var goal=StorageStand(approachHome,approachSlot);var before=hero.p;
   if(Vector2.Distance(hero.p,approachCorner)<.025f)approachCorner=goal;
   hero.p=MoveAmongActors(hero,Vector2.ClampMagnitude(approachCorner-hero.p,dt*1.3f),false);var delta=hero.p-before;actualMove+=delta;
   if(delta.sqrMagnitude>0){face=delta.normalized;stepTime+=delta.magnitude;if(stepTime>=1.05f){stepTime-=1.05f;foley.Step(SurfaceAt(hero.p),false,false);Alert(hero.p,4);}}
   approachClock+=dt;
   if(Vector2.Distance(hero.p,goal)<.025f){
    approachTurning=true;face=Vector2.up;actualMove=Vector2.zero;
    if(Vector3.Angle(hero.root.forward,Vector3.forward)<3){var h=approachHome;approachHome=null;approachTurning=false;BeginSearch(h,approachSlot,true);}
   }
   else if(approachClock>4){CancelSearch();Say("Проход к ящику занят");}
  }
  void SetStorageContact(){
   if(searchSlot<0){hero.visual.SetStorageContact(false,Vector3.zero);return;}
   var h=searchHome;int i=searchSlot;
   // Contact stays on the accessible front edge rather than at the box centre.
   var point=h.drawers[i]?h.lids[i].position+new Vector3(0,.08f,-.2f):V(h.p+h.containers[i]+Vector2.down*.18f,h.storageRest[i].y+.015f);
   hero.visual.SetStorageContact(true,point);
  }
 }
}
