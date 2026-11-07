using UnityEngine;
using UnityEngine.InputSystem;
namespace Askich {
 public sealed partial class DistrictGame {
  int hoveredStorage=-1,openedStorage=-1;
  bool storageClickConsumed;
  LineRenderer storageCue;MaterialPropertyBlock storageTint;
  bool StorageReachable(Home home,int slot)=>home!=null&&home==current&&slot>=0&&slot<3&&Vector2.Distance(hero.p,home.p+home.containers[slot])<1.6f&&Sight(hero.p,home.p+home.containers[slot]);
  int FocusedStorageSlot()=>searchSlot>=0&&searchHome==current?searchSlot:StorageReachable(current,hoveredStorage)?hoveredStorage:current==null?-1:NearbyContainer(current);
  int StorageUnderPointer(){
   if(current==null||!PcWorldPointer())return -1;
   var ray=cam.ScreenPointToRay(Mouse.current.position.ReadValue());float nearest=float.PositiveInfinity;int found=-1;
   for(int i=0;i<3;i++)if(StorageReachable(current,i))foreach(var mesh in current.storageArt[i]){
    if(mesh.bounds.IntersectRay(ray,out float distance)&&distance<nearest){nearest=distance;found=i;}
   }
   return found;
  }
  void InteractStorage(Home home,int slot){
   if(!StorageReachable(home,slot)||medicalAction!=MedicalAction.None)return;
   if(searchSlot>=0){CancelSearch();return;}
   if(home.taken[slot]){openedStorage=homes.IndexOf(home)*3+slot;suppliesOpen=true;inventorySelection=-1;}
   else BeginSearch(home,slot);
  }
  void TickStoragePointer(){
   storageClickConsumed=false;hoveredStorage=StorageUnderPointer();
   if(!suppliesOpen)openedStorage=-1;
   // Holding aim always reserves the left button for combat.
   if(hoveredStorage<0||!PcWorldPointer()||Mouse.current.rightButton.isPressed||!Mouse.current.leftButton.wasPressedThisFrame)return;
   storageClickConsumed=true;
   if(attack<=0&&heroStagger<=0)InteractStorage(current,hoveredStorage);
  }
  void ClearStorageFocus(){hoveredStorage=openedStorage=-1;storageClickConsumed=false;if(storageCue)storageCue.enabled=false;}
  void UpdateStorageCue(){
   int slot=FocusedStorageSlot();bool show=playing&&!paused&&!dead&&!won&&!suppliesOpen&&!pcAiming&&current!=null&&StorageReachable(current,slot);
   if(!show){if(storageCue)storageCue.enabled=false;return;}
   if(!storageCue){
    if(!attackCueMaterial)attackCueMaterial=new Material(Resources.Load<Shader>("Survival/AttackCue"));
    storageCue=new GameObject("Available storage outline").AddComponent<LineRenderer>();storageCue.sharedMaterial=attackCueMaterial;storageCue.useWorldSpace=true;storageCue.loop=true;storageCue.positionCount=4;storageCue.widthMultiplier=.035f;storageCue.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;storageCue.receiveShadows=false;storageTint=new MaterialPropertyBlock();
   }
   var bounds=current.storageArt[slot][0].bounds;float y=bounds.max.y+.12f;
   storageCue.SetPosition(0,new Vector3(bounds.min.x-.06f,y,bounds.min.z-.06f));storageCue.SetPosition(1,new Vector3(bounds.max.x+.06f,y,bounds.min.z-.06f));storageCue.SetPosition(2,new Vector3(bounds.max.x+.06f,y,bounds.max.z+.06f));storageCue.SetPosition(3,new Vector3(bounds.min.x-.06f,y,bounds.max.z+.06f));
   storageTint.SetColor("_Color",searchSlot>=0?new Color(.88f,.68f,.32f,.95f):hoveredStorage==slot?new Color(.91f,.91f,.74f,.95f):new Color(.51f,.72f,.65f,.8f));storageCue.SetPropertyBlock(storageTint);storageCue.enabled=true;
  }
 }
}
