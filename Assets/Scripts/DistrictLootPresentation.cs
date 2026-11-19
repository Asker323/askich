using UnityEngine;
namespace Askich {
 // Presentation only: neither inventory nor searched state is changed here.
 [DefaultExecutionOrder(100)]
 public sealed class DistrictLootPresentation:MonoBehaviour {
  Transform hand,body;
  public void Bind(Transform palm,Transform actor){hand=palm;body=actor;Follow();}
  void Follow(){if(!hand||!body)return;transform.position=hand.position+body.forward*.055f+Vector3.up*.025f;transform.rotation=body.rotation*Quaternion.Euler(0,0,-12);}
  void LateUpdate(){Follow();}
 }
}
