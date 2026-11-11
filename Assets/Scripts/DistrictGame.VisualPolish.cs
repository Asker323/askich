using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  readonly Dictionary<string,Material> surfaceMaterials=new Dictionary<string,Material>();
  Shader surfaceShader;
  Material SurfaceFor(string name,Color color){
   int kind=(name.EndsWith("wall")||name.Contains(" wall "))?0:name=="House floor"||name.Contains("fence board")||name=="Fence cross rail"?1:name=="Roof slope"?2:name=="District terrain"||name=="Garden path"||name=="Doorstep"?3:-1;
   if(kind<0)return Mat(color);
   string key=kind+":"+ColorUtility.ToHtmlStringRGB(color);
   if(surfaceMaterials.TryGetValue(key,out var material))return material;
   if(!surfaceShader)surfaceShader=Resources.Load<Shader>("Survival/WorldSurface");
   if(!surfaceShader||!surfaceShader.isSupported)return Mat(color);
   material=new Material(surfaceShader){color=color};material.SetFloat("_Surface",kind);surfaceMaterials.Add(key,material);return material;
  }
 }
}
