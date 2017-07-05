using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace AssemblyCSharpEditor
{
    public class ProjectBuilder
    {
        static string[] SCENES = FindEnabledEditorScenes();
        static string APP_NAME = "Last Gladiator";
        static string TARGET_DIR = "build";

        [MenuItem("Custom/CI/Build AssetBundles")]
        static void BuildAssetBundles()
        {
            string[] files = Directory.GetFiles("Assets/Tables/Resources/", "*.xml");
            UnityEngine.Object[] objects = new UnityEngine.Object[files.Length];

            int i = 0;

            foreach (string fname in files)
            {
                objects[i] = AssetDatabase.LoadMainAssetAtPath(fname);
                Debug.Log(fname + " : " + objects[i].name);
                i++;
            }

            BuildPipeline.BuildAssetBundle(objects[0], objects, "./tables.unity3d", BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.Android);
        }

        [MenuItem("Custom/Set defines/OCTO 7")]
        static void DefineO7()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "KAKAO_O7;DEBUG_MODE");
        }

        [MenuItem("Custom/Set defines/LOCAL_TEST")]
        static void DefineLocalTest()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "LOCAL_TEST");
        }
        [MenuItem("Custom/Set defines/USE_ASSET_BUNDLE")]
        static public void DefineUseAssetBundle()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "USE_ASSET_BUNDLE");
        }
        [MenuItem("Custom/Set defines/USE_ASSET_BUNDLE LOCAL_TEST")]
        static void DefineLocalTestUseAssetBundle()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "LOCAL_TEST;USE_ASSET_BUNDLE");
        }

        [MenuItem("Custom/Set defines/LOCAL_TEST DEBUG_MODE")]
        static void DefineLocalTestDebugMode()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "LOCAL_TEST;DEBUG_MODE");
        }
        [MenuItem("Custom/Set defines/USE_ASSET_BUNDLE DEBUG_MODE")]
        static void DefineUseAssetBundleDebugMode()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "USE_ASSET_BUNDLE;DEBUG_MODE");
        }
        [MenuItem("Custom/Set defines/USE_ASSET_BUNDLE LOCAL_TEST DEBUG_MODE")]
        static void DefineLocalTestUseAssetBundleDebugMode()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "LOCAL_TEST;USE_ASSET_BUNDLE;DEBUG_MODE");
        }



        [MenuItem("Custom/CI/Build Android")]
        static void PerformAndroidBuild()
        {
            //PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "DEBUG_MODE");
            //string target_filename = APP_NAME + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_woAB.apk";
            //GenericBuild(SCENES, target_filename, BuildTarget.Android, BuildOptions.None);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "DEBUG_MODE;USE_ASSET_BUNDLE");
            string target_filename_ab = APP_NAME + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".apk";
            GenericBuild(SCENES, target_filename_ab, BuildTarget.Android, BuildOptions.None);
        }

        [MenuItem("Custom/CI/Build iOS Debug")]
        static void PerformiOSDebugBuild()
        {
            BuildOptions opt = BuildOptions.SymlinkLibraries |
                               BuildOptions.Development |
                               BuildOptions.ConnectWithProfiler |
                               BuildOptions.AllowDebugging |
                               BuildOptions.Development |
                               BuildOptions.AcceptExternalModificationsToPlayer;

            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersion = iOSTargetOSVersion.iOS_4_3;
            PlayerSettings.statusBarHidden = true;

            char sep = Path.DirectorySeparatorChar;
            string buildDirectory = Path.GetFullPath(".") + sep + TARGET_DIR;
            Directory.CreateDirectory(buildDirectory);

            string BUILD_TARGET_PATH = buildDirectory + "/ios";
            Directory.CreateDirectory(BUILD_TARGET_PATH);

            GenericBuild(SCENES, BUILD_TARGET_PATH, BuildTarget.iPhone, opt);
        }

        private static string[] FindEnabledEditorScenes()
        {
            List<string> EditorScenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                EditorScenes.Add(scene.path);
            }
            return EditorScenes.ToArray();
        }

        static void GenericBuild(string[] scenes, string target_filename, BuildTarget build_target, BuildOptions build_options)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(build_target);
            string res = BuildPipeline.BuildPlayer(scenes, target_filename, build_target, build_options);
            if (res.Length > 0)
            {
                throw new Exception("BuildPlayer failure: " + res);
            }
        }
    }
}