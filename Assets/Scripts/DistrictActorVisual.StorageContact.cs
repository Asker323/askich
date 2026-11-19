using UnityEngine;
namespace Askich {
 public sealed partial class DistrictActorVisual {
  readonly Transform[] reachUpper=new Transform[2],reachLower=new Transform[2],reachHand=new Transform[2];
  Transform reachHips;bool contactWanted;Vector3 contactPoint;float contactWeight;
  void PrepareStorageContact(GameObject model){
   if(zombie)return;
   foreach(var bone in model.GetComponentsInChildren<Transform>(true)){
    if(bone.name=="Hips")reachHips=bone;
    for(int i=0;i<2;i++){string side=i==0?"L":"R";if(bone.name=="UpperArm."+side)reachUpper[i]=bone;if(bone.name=="LowerArm."+side)reachLower[i]=bone;if(bone.name=="Hand."+side)reachHand[i]=bone;}
   }
  }
  public void SetStorageContact(bool active,Vector3 point){contactWanted=active;contactPoint=point;}
  void ApplyStorageContact(float dt){
   if(zombie||!reachHips)return;
   bool active=contactWanted&&searching&&state=="Search"&&!dead;
   // The animation rewrites the base pose each frame; corrections never accumulate.
   contactWeight=Mathf.MoveTowards(contactWeight,active?1:0,dt*7);
   if(!active){contactWeight=0;return;}
   float weight=contactWeight;
   var left=transform.TransformPoint(restFoot[0])+Vector3.up*.18f;var right=transform.TransformPoint(restFoot[1])+Vector3.up*.18f;var lr=turnFoot[0].rotation;var rr=turnFoot[1].rotation;
   float crouch=Mathf.Clamp((1.05f-contactPoint.y)*.85f,0,.28f)*weight;
   reachHips.position-=Vector3.up*crouch;
   if(turnChest)turnChest.rotation=Quaternion.AngleAxis(12*weight,transform.right)*turnChest.rotation;
   SolveTurnLeg(turnUpper[0],turnLower[0],turnFoot[0],left);SolveTurnLeg(turnUpper[1],turnLower[1],turnFoot[1],right);turnFoot[0].rotation=lr;turnFoot[1].rotation=rr;
   for(int i=0;i<2;i++){
    var upper=reachUpper[i];var lower=reachLower[i];var hand=reachHand[i];if(!upper||!lower||!hand)continue;
    float phase=storageSearch*Mathf.PI*2/1.5f+i*Mathf.PI;
    var goal=contactPoint+transform.right*((i==0?-.14f:.14f)+Mathf.Sin(phase)*.025f)+Vector3.up*(.025f+.035f*(1+Mathf.Sin(phase)));
    goal=Vector3.Lerp(hand.position,goal,weight);
    float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
    Vector3 delta=goal-upper.position;float distance=Mathf.Clamp(delta.magnitude,.05f,a+b-.015f);Vector3 direction=delta.normalized;goal=upper.position+direction*distance;
    float along=(a*a-b*b+distance*distance)/(2*distance);
    Vector3 bend=Vector3.ProjectOnPlane(-transform.forward+transform.right*(i==0?-.65f:.65f)-Vector3.up*.3f,direction).normalized;
    Vector3 elbow=upper.position+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
    Quaternion wrist=hand.rotation;
    upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
    lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,goal-lower.position)*lower.rotation;
    hand.rotation=Quaternion.Slerp(hand.rotation,wrist,.8f);
   }
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public float ArtContactWeight=>contactWeight;
#endif
 }
}
