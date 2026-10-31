using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Squares.Editor
{
    public static class BuildProject
    {
        public static void BuildAll()
        {
            try
            {
                Configure();
                SimulationChecks.Run();
                Directory.CreateDirectory("Builds/Windows");
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/Arena.unity" },
                    locationPathName = "Builds/Windows/Squares.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception("Windows build failed: " + report.summary.result);
                Debug.Log("SQUARES_BUILD_PASS bytes=" + report.summary.totalSize);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); Debug.LogError("SQUARES_BUILD_FAIL"); EditorApplication.Exit(1); }
        }

        [MenuItem("Squares/Prepare Arena Scene")]
        public static void Configure()
        {
            PlayerSettings.companyName = "Squares";
            PlayerSettings.productName = "SQUARES OVERLOAD";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "games.squares.overload");
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            Shader sprite = Shader.Find("Sprites/Default");
            bool found = false;
            for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == sprite) found = true;
            if (!found && sprite != null)
            {
                shaders.InsertArrayElementAtIndex(shaders.arraySize);
                shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = sprite;
                graphics.ApplyModifiedPropertiesWithoutUndo();
            }
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("SQUARES // OVERLOAD").AddComponent<GameApp>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Arena.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Arena.unity", true) };
            AssetDatabase.SaveAssets();
        }
    }
}
