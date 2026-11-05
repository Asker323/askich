using UnityEngine;
using UnityEngine.InputSystem;
using Touch=UnityEngine.InputSystem.EnhancedTouch.Touch;
namespace Askich {
 // The district has its own layout; do not reuse medieval shield/dodge hit areas.
 public sealed class DistrictTouchControls {
  public static readonly Rect Pause=new Rect(1130,28,110,42),Torch=new Rect(1030,395,200,65),Use=new Rect(1030,475,200,70),Hit=new Rect(1030,555,200,65),Heal=new Rect(810,555,200,65);
  public Vector2 move;
  public bool pause,torch,use,hit,heal,sprint;
  public void Read(bool active){
   move=Vector2.zero;pause=torch=use=hit=heal=sprint=false;
   if(!active||!TouchControls.Enabled)return;
   foreach(var t in Touch.activeTouches)if(t.phase!=UnityEngine.InputSystem.TouchPhase.Ended&&t.phase!=UnityEngine.InputSystem.TouchPhase.Canceled)Sample(ScreenLayout.ToUI(t.screenPosition),t.phase==UnityEngine.InputSystem.TouchPhase.Began);
   if(!Application.isMobilePlatform&&Touch.activeTouches.Count==0&&Mouse.current!=null&&Mouse.current.leftButton.isPressed)Sample(ScreenLayout.ToUI(Mouse.current.position.ReadValue()),Mouse.current.leftButton.wasPressedThisFrame);
  }
  public void Sample(Vector2 p,bool began){
   var delta=p-new Vector2(115,535);float distance=delta.magnitude;
   if(distance<110&&distance>84)sprint=true;
   if(distance<110)move=new Vector2(delta.x,-delta.y).normalized*Mathf.InverseLerp(8,68,distance);
   if(Hit.Contains(p))hit=true;
   if(!began)return;
   pause|=Pause.Contains(p);torch|=Torch.Contains(p);use|=Use.Contains(p);heal|=Heal.Contains(p);
  }
 }
}
