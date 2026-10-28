using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Askich.Editor
{
    /// <summary>Creates the entry scene and builds a reproducible Windows player.</summary>
    public static class BuildGame
    {
        public const string ScenePath = "Assets/Scenes/Arena.unity";

        [MenuItem("Askich/Prepare Project")]
        public static void Prepare()
        {
            PrepareInternal();
        }

        static void ConfigureArt()
        {
            foreach(string name in new[]{"warriors", "props", "houses"})
            {
                string path = "Assets/Resources/Medieval/"+name+".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer == null) throw new BuildFailedException("Missing atlas: "+path);
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
        private static bool PrepareInternal()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            Configure();
            ConfigureArt();
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Askich").AddComponent<ArenaGame>();
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save the arena scene.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ASKICH_PREPARE_OK");
            return true;
        }

        [MenuItem("Askich/Build Windows")]
        public static void Windows()
        {
            if (!PrepareInternal()) return;
            VerifyRules();
            ValidateScene();
            ValidateShaders();
            bool development = Environment.GetEnvironmentVariable("ASKICH_DEVELOPMENT") == "1";
            EditorUserBuildSettings.development = development;

            string overridePath = Environment.GetEnvironmentVariable("ASKICH_BUILD_DIR");
            string directory = string.IsNullOrWhiteSpace(overridePath)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Askich-Windows"))
                : Path.GetFullPath(overridePath);
            Directory.CreateDirectory(directory);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(directory, "Askich.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None
            });

            string summary = $"Result: {report.summary.result}\n" +
                             $"Product: {PlayerSettings.productName}\n" +
                             $"Version: {PlayerSettings.bundleVersion}\n" +
                             $"Unity: {Application.unityVersion}\n" +
                             $"Platform: {report.summary.platform}\n" +
                             $"Development: {development}\n" +
                             $"Errors: {report.summary.totalErrors}\n" +
                             $"Warnings: {report.summary.totalWarnings}\n" +
                             $"Bytes: {report.summary.totalSize}\n";
            File.WriteAllText(Path.Combine(directory, "build-report.txt"), summary);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException(summary);

            File.WriteAllText(Path.Combine(directory, "READ-ME.txt"),
                "ASKICH 2.1.0 — Windows x64\n\n" +
                "Run Askich.exe. Keep the entire folder together.\n" +
                "WASD / arrows: move | Mouse: aim | Left mouse: attack | Right mouse: block\n" +
                "Q: heavy attack | Space: dodge | E: rescue | Escape / P: pause | M: mute\n" +
                "Enter: continue story | 1 / 2 / 3: faction or upgrade\n" +
                "1 / 2 / 3: choose upgrade\n\n" +
                "Unity 6000.3.4f1 | Project: Asker323\n");
            Debug.Log("ASKICH_BUILD_OK\n" + summary);
        }

        [MenuItem("Askich/Build Android APK")]
        public static void Android()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support, Android SDK & NDK Tools and OpenJDK for Unity 6000.3.4f1 through Unity Hub.");
            if(!PrepareInternal())return;
            VerifyRules();ValidateScene();ValidateShaders();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.asker323.askich");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode=210;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            EditorUserBuildSettings.buildAppBundle=false;
            string output=Environment.GetEnvironmentVariable("ASKICH_BUILD_DIR");
            if(string.IsNullOrWhiteSpace(output))output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Askich-Android"));
            Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(output,"Askich.apk"),target=BuildTarget.Android,options=BuildOptions.None});
            File.WriteAllText(Path.Combine(output,"build-report.txt"),"Result: "+report.summary.result+"\nPlatform: Android ARM64\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Android build failed. See build report.");
        }
        [MenuItem("Askich/Validate Entry Scene")]
        public static void ValidateScene()
        {
            if (!File.Exists(ScenePath))
                throw new BuildFailedException("Entry scene is missing. Run Askich > Prepare Project.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int gameCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                gameCount += root.GetComponentsInChildren<ArenaGame>(true).Length;
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                        throw new BuildFailedException("Missing script on " + child.name);
            }
            if (gameCount != 1)
                throw new BuildFailedException("The entry scene must contain exactly one ArenaGame.");

            Debug.Log("ASKICH_SCENE_VALID");
        }

        [MenuItem("Askich/Verify Combat Rules")]
        public static void VerifyRules()
        {
            int assertions = 0;
            void Check(bool condition, string name)
            {
                if (!condition) throw new BuildFailedException("Combat rule failed: " + name);
                assertions++;
            }

            Check(ArenaRules.WaveSize(1) == 10, "first wave budget");
            Check(ArenaRules.WaveSize(ArenaRules.FinalWave) == 18, "final wave budget");
            Check(ArenaRules.WaveSize(-10) == ArenaRules.WaveSize(1), "minimum wave clamp");
            Check(ArenaRules.WaveSize(100) == ArenaRules.WaveSize(ArenaRules.FinalWave), "maximum wave clamp");
            Check(ArenaRules.Clamp(Vector2.zero) == Vector2.zero, "interior position remains unchanged");
            Vector2 clamped = ArenaRules.Clamp(new Vector2(100, -100), .5f);
            Check(Mathf.Approximately(clamped.x, ArenaRules.HalfWidth - .5f) &&
                  Mathf.Approximately(clamped.y, -ArenaRules.HalfHeight + .5f), "body stays inside arena");
            Check(ArenaRules.SegmentHits(new Vector2(-10, 0), new Vector2(10, 0), Vector2.zero, .25f),
                "fast projectile cannot tunnel through target");
            Check(!ArenaRules.SegmentHits(new Vector2(-10, 1), new Vector2(10, 1), Vector2.zero, .25f),
                "parallel projectile misses");
            Check(ArenaRules.SegmentHits(new Vector2(-1, .5f), new Vector2(1, .5f), Vector2.zero, .5f),
                "tangent contact is a hit");
            Check(!ArenaRules.SegmentHits(Vector2.zero, Vector2.right, new Vector2(2, 0), .25f),
                "target beyond end of frame is not hit early");
            Check(ArenaRules.SegmentHits(Vector2.zero, Vector2.zero, Vector2.zero, .5f),
                "stationary projectile inside target");
            Check(!ArenaRules.SegmentHits(Vector2.zero, Vector2.zero, Vector2.one, .5f),
                "stationary projectile outside target");
            Debug.Log("ASKICH_RULES_VALID: " + assertions + " assertions passed");
        }

        private static void ValidateShaders()
        {
            UnityEngine.Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settings.Length == 0) throw new BuildFailedException("GraphicsSettings asset is missing.");
            var graphics = new SerializedObject(settings[0]);
            SerializedProperty included = graphics.FindProperty("m_AlwaysIncludedShaders");
            if (included == null) throw new BuildFailedException("Always-included shaders setting is missing.");
            for (int i = 0; i < included.arraySize; i++)
            {
                Shader shader = included.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader != null && shader.name == "Hidden/Internal-Colored")
                    throw new BuildFailedException("Transient Internal-Colored shader must not be included in player builds.");
            }
            Debug.Log("ASKICH_SHADERS_VALID");
        }

        private static void Configure()
        {
            PlayerSettings.companyName = "Asker323";
            PlayerSettings.productName = "Askich";
            PlayerSettings.bundleVersion = "2.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.asker323.askich");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;

            // Runtime-created materials still need their shaders in a stripped player.
            UnityEngine.Object[] graphicsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsAssets.Length == 0)
                throw new BuildFailedException("GraphicsSettings asset is missing.");
            var graphics = new SerializedObject(graphicsAssets[0]);
            SerializedProperty included = graphics.FindProperty("m_AlwaysIncludedShaders");
            if (included != null)
            {
                // Internal-Colored belongs to Unity's transient default resources. Including it
                // causes DontSave serialization errors and can crash Unity 6000.3.4f1 builds.
                for (int i = included.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty entry = included.GetArrayElementAtIndex(i);
                    Shader existing = entry.objectReferenceValue as Shader;
                    if (existing == null || existing.name != "Hidden/Internal-Colored") continue;
                    entry.objectReferenceValue = null;
                    included.DeleteArrayElementAtIndex(i);
                }

                foreach (string shaderName in new[] { "Sprites/Default", "UI/Default" })
                {
                    Shader shader = Shader.Find(shaderName);
                    if (shader == null) continue;
                    bool present = false;
                    for (int i = 0; i < included.arraySize; i++)
                        present |= included.GetArrayElementAtIndex(i).objectReferenceValue == shader;
                    if (present) continue;
                    int index = included.arraySize;
                    included.InsertArrayElementAtIndex(index);
                    included.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                }
                graphics.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
