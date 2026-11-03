using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
namespace Askich.Editor {
 public static class BuildSurvival {
  public static void Pair(){string release=Environment.GetEnvironmentVariable("GOIDA_SURVIVAL_RELEASE");if(string.IsNullOrEmpty(release))throw new Exception("Set GOIDA_SURVIVAL_RELEASE");Environment.SetEnvironmentVariable("ASKICH_DEVELOPMENT","1");Windows();Environment.SetEnvironmentVariable("ASKICH_DEVELOPMENT","0");Environment.SetEnvironmentVariable("ASKICH_BUILD_DIR",release);Windows();}

  public static void Windows(){BuildGame.Prepare();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);new GameObject("Survival").AddComponent<SurvivalGame>();string path="Assets/Scenes/Survival.unity";EditorSceneManager.SaveScene(scene,path);bool dev=Environment.GetEnvironmentVariable("ASKICH_DEVELOPMENT")=="1";string output=Environment.GetEnvironmentVariable("ASKICH_BUILD_DIR");if(string.IsNullOrEmpty(output))throw new Exception("Set ASKICH_BUILD_DIR");Directory.CreateDirectory(output);PlayerSettings.productName="GOIDA Survival Prototype";var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},target=BuildTarget.StandaloneWindows64,locationPathName=Path.Combine(output,"GOIDA-Survival.exe"),options=dev?BuildOptions.Development:BuildOptions.None});File.WriteAllText(Path.Combine(output,"build-report.txt"),$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nDevelopment: {dev}\n");PlayerSettings.productName="GOIDA";AssetDatabase.SaveAssets();if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Survival build failed");}
 }
}
