using UnityEngine;
namespace Askich {
public static class ScreenLayout {
public static Rect Safe => Application.isMobilePlatform?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
public static float Scale => Mathf.Min(Safe.width/1280f,Safe.height/720f);
public static Vector2 Offset => new Vector2(Safe.x+(Safe.width-1280*Scale)/2,Screen.height-Safe.yMax+(Safe.height-720*Scale)/2);
public static Vector2 ToUI(Vector2 screen) => (new Vector2(screen.x,Screen.height-screen.y)-Offset)/Scale;
public static void Fit(Camera camera) {float s=Scale;Vector2 p=Offset;camera.rect=new Rect(p.x/Screen.width,(Screen.height-p.y-720*s)/Screen.height,1280*s/Screen.width,720*s/Screen.height);}
}}
