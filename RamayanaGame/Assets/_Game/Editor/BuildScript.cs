using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ramayana.EditorTools
{
    public static class BuildScript
    {
        const string OutputPath = "Builds/Android/Ramayana-dev.apk";

        [MenuItem("Ramayana/Build/Android APK (Development)")]
        public static void BuildAndroidDev() => BuildAndroid(development: true);

        // Command line: Unity.exe -batchmode -projectPath <path> -executeMethod Ramayana.EditorTools.BuildScript.BuildAndroidCli
        public static void BuildAndroidCli() => EditorApplication.Exit(BuildAndroid(development: true) ? 0 : 1);

        static bool BuildAndroid(bool development)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[Ramayana] Android Build Support is not installed. Unity Hub > Installs > (gear) > Add modules > Android Build Support.");
                return false;
            }

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Ramayana] No scenes in Build Settings. Run Ramayana > Build Test Level first.");
                return false;
            }

            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
            });

            var s = report.summary;
            Debug.Log($"[Ramayana] Build {s.result}: {s.outputPath} ({s.totalSize / 1048576f:F1} MB, {s.totalTime:mm\\:ss}, {s.totalErrors} errors)");
            return s.result == BuildResult.Succeeded;
        }
    }
}
