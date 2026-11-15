using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  readonly Dictionary<string,Material> surfaceMaterials=new Dictionary<string,Material>();
  Shader surfaceShader;
  Material SurfaceFor(string name,Color color){
   if(name=="Road marking"){if(surfaceMaterials.TryGetValue("paint",out var paint))return paint;paint=new Material(Resources.Load<Shader>("Survival/RoadPaint")){color=color};surfaceMaterials.Add("paint",paint);return paint;}
   string lower=name.ToLowerInvariant();
   string scan=name=="Street"?"asphalt_01":lower.Contains("wall")?"plaster_stone_wall_02":lower.Contains("doorstep")||lower.Contains("kerb")?"plastered_wall_02":name=="District terrain"||lower.Contains("garden path")||lower.Contains("yard worn path")?"brown_mud_leaves_01":name=="House floor"||lower.Contains("fence")||lower.Contains("table")||lower.Contains("workbench")||lower.Contains("bed frame")||lower.Contains("door leaf")||lower.Contains("timber")||lower.Contains("board")?"wood_floor_deck":null;
   if(scan!=null){
    string scanKey=scan+":"+ColorUtility.ToHtmlStringRGB(color);if(surfaceMaterials.TryGetValue(scanKey,out var cached))return cached;
    var shader=Resources.Load<Shader>("Survival/ScannedSurface");var texture=Resources.Load<Texture2D>("Survival/Surfaces/"+scan+"_diff_1k");
    if(shader&&texture){var scanned=new Material(shader);scanned.mainTexture=texture;scanned.SetTexture("_BumpMap",Resources.Load<Texture2D>("Survival/Surfaces/"+scan+"_nor_gl_1k"));scanned.SetTexture("_Roughness",Resources.Load<Texture2D>("Survival/Surfaces/"+scan+"_rough_1k"));scanned.SetFloat("_Tiling",scan=="asphalt_01"?.5f:scan=="wood_floor_deck"?.55f:.65f);scanned.SetFloat("_Damp",lower.Contains("wall")?.45f:0);scanned.SetFloat("_PlasterCover",scan=="plaster_stone_wall_02"?1:0);scanned.SetFloat("_Saturation",scan=="wood_floor_deck"?.38f:.85f);scanned.color=scan=="asphalt_01"?Color.white*.75f:scan=="plaster_stone_wall_02"?Color.Lerp(Color.white*.85f,color*1.4f,.35f):scan=="plastered_wall_02"?Color.white*.65f:scan=="wood_floor_deck"?Color.Lerp(new Color(.62f,.66f,.67f),color,.22f):Color.white*.85f;surfaceMaterials.Add(scanKey,scanned);return scanned;}
   }
   int kind=(name.EndsWith("wall")||name.Contains(" wall "))?0:name=="House floor"||name.Contains("fence board")||name=="Fence cross rail"?1:lower.Contains("roof")?2:name=="District terrain"||name=="Garden path"||name=="Doorstep"?3:-1;
   if(kind<0)return Mat(color);
   string key=kind+":"+ColorUtility.ToHtmlStringRGB(color);
   if(surfaceMaterials.TryGetValue(key,out var material))return material;
   if(!surfaceShader)surfaceShader=Resources.Load<Shader>("Survival/WorldSurface");
   if(!surfaceShader||!surfaceShader.isSupported)return Mat(color);
   material=new Material(surfaceShader){color=color};material.SetFloat("_Surface",kind);surfaceMaterials.Add(key,material);return material;
  }
 }
}
