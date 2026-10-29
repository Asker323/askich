using UnityEngine;
namespace Askich
{
    public static class ScreenLayout
    {
        public static Rect Safe => Application.isMobilePlatform ? Screen.safeArea : new Rect(0,0,Screen.width,Screen.height);
        public static Rect Viewport(float width,float height,Rect safe)
        {
            width=Mathf.Max(1,width);height=Mathf.Max(1,height);
            float left=Mathf.Clamp(safe.xMin,0,width),bottom=Mathf.Clamp(safe.yMin,0,height);
            float right=Mathf.Clamp(safe.xMax,left,width),top=Mathf.Clamp(safe.yMax,bottom,height);
            if(right-left<1||top-bottom<1){left=bottom=0;right=width;top=height;}
            float scale=Mathf.Min((right-left)/1280f,(top-bottom)/720f);
            return new Rect(left+(right-left-1280*scale)/2,bottom+(top-bottom-720*scale)/2,1280*scale,720*scale);
        }
        static Rect Current => Viewport(Screen.width,Screen.height,Safe);
        public static float Scale => Current.width/1280f;
        public static Vector2 Offset {get {var r=Current;return new Vector2(r.x,Screen.height-r.yMax);}}
        public static Vector2 ToUI(Vector2 screen) => (new Vector2(screen.x,Screen.height-screen.y)-Offset)/Scale;
        public static void Fit(Camera camera)
        {
            var r=Current;float w=Mathf.Max(1,Screen.width),h=Mathf.Max(1,Screen.height);
            camera.rect=new Rect(r.x/w,r.y/h,r.width/w,r.height/h);
        }
    }
}
