using System.Linq;
using Ramayana.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ramayana.EditorTools
{
    static class ProjectSetup
    {
        const string SettingsDir = "Assets/_Game/Settings";
        const string UrpPackage = "Packages/com.unity.render-pipelines.universal";

        static readonly string[] Packages =
        {
            "com.unity.2d.sprite",
            "com.unity.2d.animation",
            "com.unity.2d.psdimporter",
            "com.unity.2d.tilemap",
            "com.unity.cinemachine",
        };

        static AddAndRemoveRequest packageRequest;

        [MenuItem("Ramayana/Setup/Run All")]
        public static void RunAll()
        {
            Configure2DRenderer();
            ConfigurePlayerSettings();
            InstallPackages(); // last: triggers a domain reload
        }

        [MenuItem("Ramayana/Setup/1 Configure 2D Renderer")]
        public static void Configure2DRenderer()
        {
            EnsureFolder(SettingsDir);

            string rendererPath = SettingsDir + "/Renderer2D.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(rendererPath);
            if (renderer == null)
            {
                // Mirrors URP's own "Create > Rendering > URP 2D Renderer" menu.
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
                ResourceReloader.ReloadAllNullIn(renderer, UrpPackage);
                var so = new SerializedObject(renderer);
                var pp = so.FindProperty("m_PostProcessData");
                if (pp != null)
                {
                    pp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(UrpPackage + "/Runtime/Data/PostProcessData.asset");
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            string urpPath = SettingsDir + "/URP_2D.asset";
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(urp, urpPath);
            }

            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);

            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            AssetDatabase.SaveAssets();
            Debug.Log("[Ramayana] 2D renderer configured: " + urpPath);
        }

        [MenuItem("Ramayana/Setup/2 Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.companyName = "MayankJoshi";
            PlayerSettings.productName = "Ramayana";
            PlayerSettings.SetApplicationIdentifier(android, "com.mayankjoshi.ramayana");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[Ramayana] Player settings configured for Android.");
        }

        [MenuItem("Ramayana/Setup/3 Install 2D Packages")]
        public static void InstallPackages()
        {
            packageRequest = Client.AddAndRemove(Packages, null);
            EditorApplication.update += WaitForPackages;
            Debug.Log("[Ramayana] Installing packages: " + string.Join(", ", Packages));
        }

        static void WaitForPackages()
        {
            if (packageRequest == null || !packageRequest.IsCompleted) return;
            EditorApplication.update -= WaitForPackages;

            if (packageRequest.Status == StatusCode.Success)
            {
                var added = packageRequest.Result.Where(p => Packages.Contains(p.name)).Select(p => $"{p.name}@{p.version}");
                Debug.Log("[Ramayana] Packages installed: " + string.Join(", ", added));
            }
            else
            {
                Debug.LogError("[Ramayana] Package install failed: " + packageRequest.Error?.message);
            }
        }

        [MenuItem("Ramayana/Reset Save")]
        public static void ResetSave()
        {
            SaveSystem.Delete();
            PlayerPrefs.DeleteAll();
            Debug.Log("[Ramayana] Save and preferences cleared.");
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
