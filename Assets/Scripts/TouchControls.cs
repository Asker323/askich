using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Askich {
public sealed class TouchControls {
public static bool ForceEnabled;
static readonly bool requested=Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-touch")>=0;
public static bool Enabled => Application.isMobilePlatform||ForceEnabled||requested;
public Vector2 move;
public bool attack,block,heavy,dodge,interact,pause;
bool priorHeavy,priorDodge,priorInteract,priorPause;
readonly List<Vector2> points=new List<Vector2>(10);
public static readonly Vector2 Stick=new Vector2(115,535),Attack=new Vector2(1130,505),Shield=new Vector2(1010,535),Heavy=new Vector2(1125,610),Dodge=new Vector2(1220,585),Interact=new Vector2(920,605),Pause=new Vector2(1220,125);
public void Read() {
points.Clear();var screen=Touchscreen.current;
if(screen!=null)foreach(var touch in screen.touches)if(touch.press.isPressed)points.Add(ScreenLayout.ToUI(touch.position.ReadValue()));
Evaluate(points);
}
public void Evaluate(IReadOnlyList<Vector2> touches) {
move=Vector2.zero;attack=block=false;bool h=false,d=false,e=false,p=false;
foreach(var point in touches) {
if(Vector2.Distance(point,Stick)<110){Vector2 delta=(point-Stick)/68;move=Vector2.ClampMagnitude(new Vector2(delta.x,-delta.y),1);}
if(Vector2.Distance(point,Attack)<52)attack=true;
if(Vector2.Distance(point,Shield)<42)block=true;
if(Vector2.Distance(point,Heavy)<36)h=true;
if(Vector2.Distance(point,Dodge)<37)d=true;
if(Vector2.Distance(point,Interact)<34)e=true;
if(Vector2.Distance(point,Pause)<28)p=true;
}
heavy=h&&!priorHeavy;dodge=d&&!priorDodge;interact=e&&!priorInteract;pause=p&&!priorPause;
priorHeavy=h;priorDodge=d;priorInteract=e;priorPause=p;
}
}
public sealed partial class ArenaGame {
readonly TouchControls touchControls=new TouchControls();
void TouchHud() {
void Disc(Vector2 center,float radius,string text,bool pressed) {
Color old=GUI.color;GUI.color=pressed?new Color(.85f,.68f,.34f,.8f):new Color(.08f,.07f,.04f,.7f);
GUI.DrawTexture(new Rect(center.x-radius,center.y-radius,radius*2,radius*2),NeonVisual.Get("circle").texture);
GUI.color=old;Text(new Rect(center.x-radius,center.y-15,radius*2,radius*2),text,new GUIStyle(smallStyle){alignment=TextAnchor.UpperCenter,fontStyle=FontStyle.Bold,fontSize=14});
}
Disc(TouchControls.Stick,78,"",false);Disc(TouchControls.Stick+new Vector2(touchControls.move.x,-touchControls.move.y)*45,28,"",true);
Disc(TouchControls.Attack,52,"УДАР",touchControls.attack);Disc(TouchControls.Shield,42,"ЩИТ",touchControls.block);
Disc(TouchControls.Heavy,36,"СИЛА",touchControls.heavy);Disc(TouchControls.Dodge,37,"ПЕРЕКАТ",touchControls.dodge);
Disc(TouchControls.Interact,34,"ВЗЯТЬ",touchControls.interact);Disc(TouchControls.Pause,28,"II",false);
}
}
}
