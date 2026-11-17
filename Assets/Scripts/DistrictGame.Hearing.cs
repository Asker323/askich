using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  struct SoundCrossing {public float enter,exit,loss;}
  readonly List<SoundCrossing> soundCrossings=new List<SoundCrossing>();
  static float FootstepNoise(bool run,bool quiet)=>quiet?1.5f:run?14:4;
  static bool SoundAxis(float start,float delta,float min,float max,ref float enter,ref float exit){
   if(Mathf.Abs(delta)<.00001f)return start>=min&&start<=max;
   float a=(min-start)/delta,b=(max-start)/delta;
   enter=Mathf.Max(enter,Mathf.Min(a,b));exit=Mathf.Min(exit,Mathf.Max(a,b));return enter<exit;
  }
  float NoiseDistance(Vector2 source,Vector2 listenerPosition){
   Vector2 delta=listenerPosition-source;soundCrossings.Clear();
   foreach(var barrier in barriers){
    if(!barrier.active||barrier.soundLoss<=0)continue;
    float enter=0,exit=1;Rect rect=barrier.rect;
    if(SoundAxis(source.x,delta.x,rect.xMin,rect.xMax,ref enter,ref exit)&&SoundAxis(source.y,delta.y,rect.yMin,rect.yMax,ref enter,ref exit)&&exit-enter>.0001f)
     soundCrossings.Add(new SoundCrossing{enter=enter,exit=exit,loss=barrier.soundLoss});
   }
   soundCrossings.Sort((a,b)=>a.enter.CompareTo(b.enter));
   // Adjacent/overlapping wall pieces form one acoustic obstacle, not stacked penalties.
   float distance=delta.magnitude,end=-1,loss=0;
   foreach(var hit in soundCrossings){
    if(hit.enter>end+.0001f){distance+=loss;loss=hit.loss;end=hit.exit;}
    else{loss=Mathf.Max(loss,hit.loss);end=Mathf.Max(end,hit.exit);}
   }
   return distance+loss;
  }
 }
}
