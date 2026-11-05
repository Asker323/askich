using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  Texture2D stickRing;
  void DrawTouchStick(){
   if(!stickRing){stickRing=new Texture2D(96,96,TextureFormat.RGBA32,false);var pixels=new Color[96*96];for(int y=0;y<96;y++)for(int x=0;x<96;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(48,48))/48;float a=Mathf.Clamp01((1-d)*48)*Mathf.Clamp01((d-.87f)*48);pixels[y*96+x]=new Color(1,1,1,a);}stickRing.SetPixels(pixels);stickRing.Apply();}
   var center=new Vector2(115,535);GUI.color=touch.sprint?new Color(.83f,.72f,.4f,.75f):new Color(.62f,.73f,.66f,.5f);GUI.DrawTexture(new Rect(center.x-82,center.y-82,164,164),stickRing);GUI.color=new Color(.62f,.73f,.66f,.3f);GUI.DrawTexture(new Rect(center.x-56,center.y-56,112,112),stickRing);var knob=center+new Vector2(touch.move.x,-touch.move.y)*52;GUI.color=MedicalAccent;GUI.DrawTexture(new Rect(knob.x-20,knob.y-20,40,40),stickRing);GUI.color=Color.white;
  }
 }
}
