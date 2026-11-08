using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  GameObject heldProvision;int shownProvision;
  int SurvivalPose()=>medicalAction==MedicalAction.Water||medicalAction==MedicalAction.Drink?1:medicalAction==MedicalAction.Eat?2:medicalAction==MedicalAction.Rest?3:medicalAction==MedicalAction.Refill?4:0;
  void UpdateHeldProvision(){
   int mode=medicalAction==MedicalAction.Read?5:SurvivalPose();
   if(mode==shownProvision)return;
   if(heldProvision){heldProvision.SetActive(false);Destroy(heldProvision);}shownProvision=mode;
   if(mode!=1&&mode!=2&&mode!=4&&mode!=5)return;
   heldProvision=new GameObject("Held provision");heldProvision.transform.SetParent(hero.root,false);
   if(mode==5)MakeProgressionIcon(heldProvision.transform,22+readingBook);
   else if(mode==2&&selectedFood>=14)MakeProgressionIcon(heldProvision.transform,selectedFood);
   else if(mode==1||mode==4)MakeWaterModel(heldProvision.transform);
   else{var tin=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(tin.GetComponent<Collider>());tin.transform.SetParent(heldProvision.transform,false);tin.transform.localScale=new Vector3(.24f,.13f,.24f);tin.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.53f,.45f,.29f));}
   if(medicalAction==MedicalAction.Drink)foreach(var renderer in heldProvision.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=Mat(new Color(.4f,.28f,.14f));
   foreach(var t in heldProvision.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
   var follower=heldProvision.AddComponent<DistrictProvisionFollower>();follower.hand=hero.visual.Hand;follower.owner=hero.root;follower.drinking=mode==1;follower.reading=mode==5;
  }
 }
 [DefaultExecutionOrder(100)]
 public sealed class DistrictProvisionFollower:MonoBehaviour {
  public Transform hand,owner;public bool drinking,reading;
  void LateUpdate(){
   if(!hand||!owner)return;
   transform.position=hand.position+owner.forward*.03f+Vector3.up*.02f;
   transform.rotation=owner.rotation*Quaternion.Euler(drinking?-40:0,0,0);
   if(drinking)transform.position-=transform.up*.16f;
   if(reading){transform.position+=owner.right*.2f+Vector3.up*.035f;transform.rotation=owner.rotation*Quaternion.Euler(15,0,0);}
   transform.localScale=Vector3.one*(reading?.9f:.65f);
  }
 }
}
