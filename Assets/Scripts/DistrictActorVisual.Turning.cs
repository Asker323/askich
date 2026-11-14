using UnityEngine;

namespace Askich {
 public sealed partial class DistrictActorVisual {
  readonly Transform[] turnUpper=new Transform[2],turnLower=new Transform[2],turnFoot=new Transform[2];
  readonly Vector3[] restFoot=new Vector3[2],plantedFoot=new Vector3[2];
  readonly Quaternion[] restFootRotation=new Quaternion[2],plantedRotation=new Quaternion[2];
  Transform turnChest;
  float facingSpeed,turnRate,turnLead,lastYawStep,pivotTime;
  int pivotLeg=-1,nextPivotLeg;
  bool pivotPlanted;
  Vector3 pivotFrom;
  Quaternion pivotFromRotation;
  const float PivotDuration=.28f;

  void PrepareTurning(GameObject model){
   if(zombie)return;
   Transform Find(string name){foreach(var bone in model.GetComponentsInChildren<Transform>(true))if(bone.name==name)return bone;return null;}
   turnChest=Find("Chest");
   for(int i=0;i<2;i++){
    string side=i==0?"L":"R";turnUpper[i]=Find("UpperLeg."+side);turnLower[i]=Find("LowerLeg."+side);turnFoot[i]=Find("Foot."+side);
    restFoot[i]=transform.InverseTransformPoint(turnFoot[i].position);restFootRotation[i]=Quaternion.Inverse(transform.rotation)*turnFoot[i].rotation;
   }
  }

  public void Face(Vector3 direction,float speed,float dt){
   if(zombie||paused||dt<=0)return;
   facingSpeed=speed;lastYawStep=0;
   float angle=direction.sqrMagnitude>.000001f?Vector3.SignedAngle(transform.forward,direction,Vector3.up):0;
   float step=Mathf.Clamp(angle*(1-Mathf.Exp(-14*dt)),-(speed>.15f?540:210)*dt,(speed>.15f?540:210)*dt);
   transform.rotation=Quaternion.AngleAxis(step,Vector3.up)*transform.rotation;
   lastYawStep=step;turnRate=Mathf.Lerp(turnRate,step/dt,1-Mathf.Exp(-16*dt));turnLead=Mathf.Lerp(turnLead,Mathf.Clamp(angle*.18f,-10,10),1-Mathf.Exp(-12*dt));
  }

  void ApplyTurning(float dt){
   if(zombie||!turnChest)return;
   if(dead||actionRemaining>0||searching||provisionMode>0||windowClimbPhase>=0||state!="Idle"&&state!="Walk"&&state!="Run"){
    pivotLeg=-1;pivotPlanted=false;turnLead=turnRate=0;return;
   }
   // The upper body leads a turn; tools receive the same transform as the hand.
   float bank=facingSpeed>.15f?Mathf.Clamp(turnRate*facingSpeed*.004f,-6,6):0;
   var offset=Quaternion.AngleAxis(turnLead,Vector3.up)*Quaternion.AngleAxis(-bank,transform.forward);
   Vector3 chestPosition=turnChest.position;turnChest.rotation=offset*turnChest.rotation;
   void TurnTool(GameObject tool){if(!tool)return;var t=tool.transform;t.position=chestPosition+offset*(t.position-chestPosition);t.rotation=offset*t.rotation;}
   TurnTool(nativeAxe);TurnTool(nativeSpear);

   if(facingSpeed>.15f){pivotLeg=-1;pivotPlanted=false;return;}
   if(!pivotPlanted){
    if(Mathf.Abs(turnRate)<12)return;
    var undoYaw=Quaternion.AngleAxis(-lastYawStep,Vector3.up);
    for(int i=0;i<2;i++){plantedFoot[i]=transform.position+undoYaw*(turnFoot[i].position-transform.position);plantedRotation[i]=undoYaw*turnFoot[i].rotation;}
    pivotPlanted=true;nextPivotLeg=turnRate>0?1:0;
   }
   // A load/teleport must never drag an old foot anchor across the map.
   if(Vector3.Distance(plantedFoot[0],transform.position)>.65f||Vector3.Distance(plantedFoot[1],transform.position)>.65f){pivotPlanted=false;pivotLeg=-1;return;}
   if(pivotLeg<0){
    bool NeedsStep(int i)=>Vector3.Distance(plantedFoot[i],transform.TransformPoint(restFoot[i]))>.025f||Quaternion.Angle(plantedRotation[i],transform.rotation*restFootRotation[i])>9;
    int candidate=NeedsStep(nextPivotLeg)?nextPivotLeg:NeedsStep(1-nextPivotLeg)?1-nextPivotLeg:-1;
    if(candidate>=0){pivotLeg=candidate;pivotTime=0;pivotFrom=plantedFoot[candidate];pivotFromRotation=plantedRotation[candidate];}
    else if(Mathf.Abs(turnRate)<3){pivotPlanted=false;return;}
   }
   if(pivotLeg>=0){
    pivotTime=Mathf.Min(PivotDuration,pivotTime+dt);float phase=pivotTime/PivotDuration;
    var target=transform.TransformPoint(restFoot[pivotLeg]);
    plantedFoot[pivotLeg]=Vector3.Lerp(pivotFrom,target,Mathf.SmoothStep(0,1,phase))+Vector3.up*(.065f*Mathf.Sin(phase*Mathf.PI));
    plantedRotation[pivotLeg]=Quaternion.Slerp(pivotFromRotation,transform.rotation*restFootRotation[pivotLeg],Mathf.SmoothStep(0,1,phase));
   }
   for(int i=0;i<2;i++){
    SolveTurnLeg(turnUpper[i],turnLower[i],turnFoot[i],plantedFoot[i]);turnFoot[i].rotation=plantedRotation[i];
   }
   if(pivotLeg>=0&&pivotTime>=PivotDuration){nextPivotLeg=1-pivotLeg;pivotLeg=-1;}
  }

  void SolveTurnLeg(Transform upper,Transform lower,Transform foot,Vector3 goal){
   float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,foot.position);var delta=goal-upper.position;
   float distance=Mathf.Clamp(delta.magnitude,.02f,a+b-.002f);var direction=delta.normalized;float along=(a*a-b*b+distance*distance)/(2*distance);
   var knee=upper.position+direction*along+Vector3.ProjectOnPlane(transform.forward,direction).normalized*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
   upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,knee-upper.position)*upper.rotation;
   lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,goal-lower.position)*lower.rotation;
  }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public int ArtPivotLeg=>pivotLeg;
  public bool ArtPivotPlanted=>pivotPlanted;
  public float ArtTurnLead=>turnLead;
#endif
 }
}
