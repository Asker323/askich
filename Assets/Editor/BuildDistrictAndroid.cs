using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Askich.Editor {
 public static class BuildDistrictAndroid {
  public static void Build(){
   if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new Exception("Android Build Support is missing");
   string tools=Environment.GetEnvironmentVariable("GOIDA_ANDROID_TOOLS");
   if(!string.IsNullOrEmpty(tools)){
    var settings=Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions",true);
    foreach(var pair in new[]{new[]{"jdkRootPath","OpenJDK"},new[]{"sdkRootPath","SDK"},new[]{"ndkRootPath","NDK"}})settings.GetProperty(pair[0]).SetValue(null,Path.Combine(tools,pair[1]));
   }
   BuildGame.Prepare();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);new GameObject("Survival").AddComponent<DistrictGame>();
   string path="Assets/Scenes/District.unity";EditorSceneManager.SaveScene(scene,path);
   PlayerSettings.productName="Berezki Last Frequency";
   PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.asker323.askich");
   PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
   PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel35;
   PlayerSettings.Android.bundleVersionCode=241;PlayerSettings.Android.useCustomKeystore=false;PlayerSettings.Android.forceInternetPermission=false;
   PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
   PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
   EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.development=false;
   string output=Environment.GetEnvironmentVariable("ASKICH_BUILD_DIR");if(string.IsNullOrEmpty(output))throw new Exception("Set ASKICH_BUILD_DIR");Directory.CreateDirectory(output);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{path},target=BuildTarget.Android,locationPathName=Path.Combine(output,"Berezki.apk"),options=BuildOptions.None});
   File.WriteAllText(Path.Combine(output,"build-report.txt"),$"Result: {report.summary.result}\nPlatform: Android ARM64\nScene: {path}\nMin SDK: 26\nTarget SDK: 35\nGraphics: OpenGLES3\nSigning: local debug key (test APK)\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n");
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Berezki Android build failed");
  }
 }
}
