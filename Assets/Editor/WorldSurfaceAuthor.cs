using System.IO;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class WorldSurfaceAuthor {
  public static void Build(){
   foreach(string file in Directory.GetFiles("Assets/Resources/Survival/Surfaces","*.jpg")){
    var importer=(TextureImporter)AssetImporter.GetAtPath(file);bool normal=file.Contains("nor_gl"),colour=file.Contains("diff");
    importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=colour;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
   }
   BuildDistrict.Pair();
  }
 }
}
