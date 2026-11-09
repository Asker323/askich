using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  const float ClimbDuration=2.8f,ClimbOffset=.72f,ClimbCost=12;
  HouseWindow climbWindow;
  Vector2 climbStart,climbFrom,climbTo;
  Quaternion climbFacing;
  float climbTime,climbApproach,climbHealth;
  bool Climbing=>climbWindow!=null;
  float ClimbPhase=>Mathf.Clamp01((climbTime-climbApproach)/ClimbDuration);
  Vector2 ClimbSavePosition=>!Climbing?hero.p:climbTime<climbApproach?hero.p:ClimbPhase<.5f?climbFrom:climbTo;
  bool ClimbSpace(Vector2 p)=>!Blocked(p)&&!infected.Exists(z=>z.hp>0&&Vector2.Distance(z.p,p)<BodySpacing+.08f);
  bool ClimbApproachClear(Vector2 a,Vector2 b){int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.08f));for(int i=0;i<=steps;i++)if(!ClimbSpace(Vector2.Lerp(a,b,(float)i/steps)))return false;return true;}
  string ClimbReason(HouseWindow w,out Vector2 from,out Vector2 to){
   from=to=hero.p;if(w==null)return "Подойди к окну";
   if(w.state==0||w.state==1&&w.angle<90)return "Сначала полностью открой окно";
   if(Climbing||FortWorking||breakingWindow!=null||attack>0||heroStagger>0||medicalAction!=MedicalAction.None||cacheSearching||searchSlot>=0)return "Сначала закончи действие";
   if(stamina<ClimbCost)return "Недостаточно сил";
   var crossing=w.p+Vector2.up*.55f;float side=hero.p.x>w.p.x?1:-1;from=crossing+Vector2.right*side*ClimbOffset;to=crossing-Vector2.right*side*ClimbOffset;
   if(Vector2.Distance(hero.p,from)>1.1f)return "Подойди к свободной стороне подоконника";
   if(!ClimbSpace(to))return "С другой стороны нет места";
   if(!ClimbApproachClear(hero.p,from))return "Подступ к окну загорожен";
   // Only this sill and its pane may be crossed. Other furniture/walls remain solid.
   for(int i=0;i<=24;i++){var p=Vector2.Lerp(from,to,i/24f);if(SolidBlocked(p))return "Проём загорожен";foreach(var b in barriers)if(b!=w.sill&&b!=w.pane&&b.active&&new Rect(b.rect.x-.28f,b.rect.y-.28f,b.rect.width+.56f,b.rect.height+.56f).Contains(p))return "Проём загорожен";}
   return null;
  }
  bool StartWindowClimb(){
   if(!playing||paused||dead||suppliesOpen||journalOpen)return false;var w=NearbyWindow();var reason=ClimbReason(w,out var from,out var to);if(reason!=null){if(w!=null)Say(reason);return false;}
   climbWindow=w;climbStart=hero.p;climbFrom=from;climbTo=to;climbFacing=hero.root.rotation;climbTime=0;climbApproach=Mathf.Max(.28f,Vector2.Distance(climbStart,from)/1.8f);climbHealth=health;
   stamina-=ClimbCost;heroVelocity=Vector2.zero;ClearPcCombat();CancelSearch();CancelMedical();hero.visual.SetSearching(false);hero.visual.SetSurvivalPose(0,0);Alert(from,4);return true;
  }
  void ClearWindowClimb(bool settle){if(!Climbing)return;if(settle){hero.p=ClimbSavePosition;hero.root.position=V(hero.p);}climbWindow=null;heroVelocity=Vector2.zero;hero.visual.SetWindowClimb(-1);}
  void TickWindowClimb(float dt){
   if(!Climbing)return;
   if(climbTime<climbApproach&&(health<climbHealth||heroStagger>0||!ClimbApproachClear(hero.p,climbFrom))){ClearWindowClimb(false);return;}
   climbTime+=dt;
   if(climbTime<climbApproach){hero.p=Vector2.Lerp(climbStart,climbFrom,climbTime/climbApproach);return;}
   hero.p=Vector2.Lerp(climbFrom,climbTo,WindowClimbMotion.Travel(ClimbPhase));face=(climbTo-climbFrom).normalized;
   if(ClimbPhase>=1){ClearWindowClimb(false);foley.Step(SurfaceAt(hero.p),false,false);Alert(hero.p,6);if(health<climbHealth)heroStagger=Mathf.Max(heroStagger,.35f);Save();}
  }
  bool AnimateWindowClimb(){
   if(!Climbing)return false;hero.root.position=V(hero.p);
   if(climbTime<climbApproach){var direction=climbFrom-climbStart;hero.root.rotation=Quaternion.Slerp(climbFacing,Quaternion.LookRotation(V(climbTo-climbFrom)),Mathf.SmoothStep(0,1,climbTime/climbApproach));hero.visual.Tick(direction.magnitude/climbApproach,FrameDelta,0,true);}
   else{hero.root.rotation=Quaternion.LookRotation(V(climbTo-climbFrom));hero.visual.SetWindowClimb(ClimbPhase);}return true;
  }
 }
 // Shared root travel keeps the authored hand and foot contacts tied to the sill.
 public static class WindowClimbMotion {
  public static float Ramp(float u,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,u));
  public static float Travel(float u)=>Ramp(u,.28f,.80f);
 }
}
