using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Askich.Editor {
 public static class DistrictArtImport {
  const string Source="Assets/Resources/Survival/Apocalypse";
  const string Output="Assets/Resources/Survival/Art";
  public static void PrepareAndBuild(){Prepare();BuildDistrict.Pair();}
  public static void Prepare(){
   Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var texture=(TextureImporter)AssetImporter.GetAtPath(Source+"/Zombie_Atlas.png");
   texture.filterMode=FilterMode.Bilinear;texture.mipmapEnabled=true;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.SaveAndReimport();
   string matPath=Output+"/Atlas.mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,matPath);}
   mat.shader=Shader.Find("Berezki/Weathered");
   mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"/Zombie_Atlas.png");mat.color=new Color(.9f,.9f,.85f);mat.SetFloat("_Glossiness",.16f);
   var report=new StringBuilder();
   foreach(string file in Directory.GetFiles(Source,"*.fbx")){
    string path=file.Replace('\\','/');var importer=(ModelImporter)AssetImporter.GetAtPath(path);
    importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;
    importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
    var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    string name=Path.GetFileNameWithoutExtension(path);model.name="Model";
    var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
    report.AppendLine(name+": "+string.Join(", ",clips.Select(c=>c.name+" ("+c.length.ToString("0.00")+"s)")));
    var idle=clips.FirstOrDefault(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);
    if(idle)idle.SampleAnimation(model,0);
    if(name.StartsWith("Characters"))foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.name!="Matt")r.gameObject.SetActive(false);
    var renderers=model.GetComponentsInChildren<Renderer>();bool first=true;Bounds bounds=default;
    foreach(var r in renderers){
     Bounds b=r.bounds;
     if(r is SkinnedMeshRenderer skin){var mesh=new Mesh();skin.BakeMesh(mesh,true);b=new Bounds(skin.transform.TransformPoint(mesh.vertices[0]),Vector3.zero);foreach(var v in mesh.vertices)b.Encapsulate(skin.transform.TransformPoint(v));UnityEngine.Object.DestroyImmediate(mesh);skin.updateWhenOffscreen=true;}
     if(first){bounds=b;first=false;}else bounds.Encapsulate(b);
     r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
    }
    report.AppendLine("  renderer bounds "+string.Join(";",renderers.Select(r=>r.name+" "+r.bounds+" scale "+r.transform.lossyScale)));
    float height=name.StartsWith("Characters")?1.8f:name=="Zombie_Chubby"?1.95f:name=="Zombie_Ribcage"?1.1f:name.StartsWith("Zombie")?1.82f:name=="StreetLights"?5:name=="WaterTower"?10:name=="Couch"?.85f:name.StartsWith("Vehicle")?1.65f:name=="Barrel"?1:name=="Pallet_Broken"?.16f:name=="TrashBag_1"?.55f:.65f;
    float scale=height/Mathf.Max(.01f,bounds.size.y);
    var wrapper=new GameObject(name);model.transform.SetParent(wrapper.transform,false);model.transform.localScale*=scale;
    model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
    var anim=model.GetComponent<Animation>();if(anim){anim.playAutomatically=false;anim.cullingType=AnimationCullingType.BasedOnRenderers;}
    report.AppendLine($"  bounds {bounds}; scale {scale}");
    PrefabUtility.SaveAsPrefabAsset(wrapper,Output+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(wrapper);
   }
   foreach(var file in Directory.GetFiles("Assets/Resources/Survival/ZombieAudio","*.wav")){
    var imp=(AudioImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));imp.forceToMono=true;var settings=imp.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.65f;imp.defaultSampleSettings=settings;imp.SaveAndReimport();
   }
   EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();File.WriteAllText("../../work/apocalypse-import.txt",report.ToString());
  }
 }
}
