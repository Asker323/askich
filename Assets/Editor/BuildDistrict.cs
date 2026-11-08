using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
namespace Askich.Editor {
 public static class BuildDistrict {
  public static void Pair(){string release=Environment.GetEnvironmentVariable("GOIDA_SURVIVAL_RELEASE");if(string.IsNullOrEmpty(release))throw new Exception("Set GOIDA_SURVIVAL_RELEASE");Environment.SetEnvironmentVariable("ASKICH_DEVELOPMENT","1");Windows();Environment.SetEnvironmentVariable("ASKICH_DEVELOPMENT","0");Environment.SetEnvironmentVariable("ASKICH_BUILD_DIR",release);Windows();}

  static void ValidateDistrictShaders(){
   foreach(string name in new[]{"Dusk","Weathered","WorldSurface","Canopy","StreetSurface","VergeGrass"}){
    var shader=Resources.Load<Shader>("Survival/"+name);if(!shader)throw new Exception("Missing district shader: "+name);
    foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity.ToString()=="Error")throw new Exception("District shader error: "+name+" "+message.message);
   }
  }
  public static void PreservePrimitiveTypes(){var references=new GameObject("Runtime primitive type references");references.SetActive(false);references.AddComponent<BoxCollider>();references.AddComponent<SphereCollider>();references.AddComponent<CapsuleCollider>();}
  public static void Windows(){BuildGame.Prepare();if(!File.Exists("Assets/Resources/Survival/World.mat")){AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/Survival/World.mat");AssetDatabase.SaveAssets();}var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);new GameObject("Survival").AddComponent<DistrictGame>();PreservePrimitiveTypes();string path="Assets/Scenes/District.unity";EditorSceneManager.SaveScene(scene,path);bool dev=Environment.GetEnvironmentVariable("ASKICH_DEVELOPMENT")=="1";string output=Environment.GetEnvironmentVariable("ASKICH_BUILD_DIR");if(string.IsNullOrEmpty(output))throw new Exception("Set ASKICH_BUILD_DIR");Directory.CreateDirectory(output);PlayerSettings.productName="Zonbee";var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},target=BuildTarget.StandaloneWindows64,locationPathName=Path.Combine(output,"Zonbee.exe"),options=dev?BuildOptions.Development:BuildOptions.None});File.WriteAllText(Path.Combine(output,"build-report.txt"),$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nDevelopment: {dev}\n");PlayerSettings.productName="Zonbee";AssetDatabase.SaveAssets();ValidateDistrictShaders();if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Survival build failed");}
 }
}
