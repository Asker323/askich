using UnityEngine;
using UnityEngine.InputSystem;
namespace Askich {
 public sealed partial class DistrictGame {
  const float PcBufferWindow=.16f;
  int queuedPcAction;float queuedPcTime;Vector2 queuedPcDirection;
  bool pcAiming;LineRenderer pcAimCue;
  void ClearPcCombat(){queuedPcAction=0;queuedPcTime=0;pcAiming=false;if(pcAimCue)pcAimCue.enabled=false;}
  bool PcWorldPointer(){var mouse=Mouse.current;if(Climbing||TouchControls.Enabled||mouse==null||!mouse.enabled||suppliesOpen||journalOpen)return false;var p=mouse.position.ReadValue();return p.x>=0&&p.y>=0&&p.x<Screen.width&&p.y<Screen.height&&!PointerOnHud(ScreenLayout.ToUI(p));}
  void ReadPcAim(){
   pcAiming=PcWorldPointer()&&Mouse.current.rightButton.isPressed;
   if(!PcWorldPointer())return;
   Ray ray=cam.ScreenPointToRay(Mouse.current.position.ReadValue());
   if(new Plane(Vector3.up,Vector3.up*.85f).Raycast(ray,out float distance)){
    var delta=ray.GetPoint(distance)-V(hero.p,.85f);if(delta.sqrMagnitude>.1f)face=new Vector2(delta.x,delta.z).normalized;
   }
   zoom=Mathf.Clamp(zoom-Mouse.current.scroll.ReadValue().y*.01f,7,22);
  }
  bool RequestPcStrike(Vector2 direction,bool shove){
   if(Climbing||!playing||paused||dead||won||suppliesOpen||heroStagger>0)return false;
   if(attack<=0)return shove?TryShove(direction):TryAttack(direction);
   if(attack>PcBufferWindow)return false;
   queuedPcAction=shove?2:1;queuedPcTime=PcBufferWindow;queuedPcDirection=direction;return true;
  }
  void TickPcCombat(float dt){
   if(Climbing||TouchControls.Enabled||suppliesOpen||journalOpen){ClearPcCombat();return;}
   queuedPcTime-=dt;if(queuedPcTime<0&&attack>0)queuedPcAction=0;
   bool shove=Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame;
   bool hit=!storageClickConsumed&&PcWorldPointer()&&Mouse.current.leftButton.wasPressedThisFrame;
   if(shove||hit){if(!RequestPcStrike(face,shove)&&stamina<(shove?24:WeaponStrike(weapon).cost))Say("Не хватает сил. Отступи и переведи дыхание.");}
   if(queuedPcAction!=0&&attack<=0){int action=queuedPcAction;queuedPcAction=0;if(action==2)TryShove(queuedPcDirection);else TryAttack(queuedPcDirection);}
  }
  void UpdatePcAimCue(){
   bool show=pcAiming&&playing&&!paused&&!dead&&!won&&!suppliesOpen&&!journalOpen;
   if(!show){if(pcAimCue)pcAimCue.enabled=false;return;}
   if(!pcAimCue){
    if(!attackCueMaterial)attackCueMaterial=new Material(Resources.Load<Shader>("Survival/AttackCue"));
    pcAimCue=new GameObject("PC weapon reach").AddComponent<LineRenderer>();pcAimCue.sharedMaterial=attackCueMaterial;pcAimCue.useWorldSpace=true;pcAimCue.loop=true;pcAimCue.positionCount=19;pcAimCue.widthMultiplier=.025f;pcAimCue.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;pcAimCue.receiveShadows=false;
    var tint=new MaterialPropertyBlock();tint.SetColor("_Color",new Color(.57f,.76f,.7f,.5f));pcAimCue.SetPropertyBlock(tint);
   }
   pcAimCue.enabled=true;var strike=WeaponStrike(weapon);float yaw=Mathf.Atan2(face.x,face.y),half=Mathf.Acos(strike.dot);
   pcAimCue.SetPosition(0,V(hero.p,.065f));
   for(int i=0;i<18;i++){float a=yaw+Mathf.Lerp(-half,half,i/17f);pcAimCue.SetPosition(i+1,V(hero.p+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*strike.range,.065f));}
  }
  void DrawPcControls(){}
 }
}
