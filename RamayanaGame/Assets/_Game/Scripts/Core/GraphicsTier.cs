using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ramayana.Core
{
    public enum Tier { Low, Mid, High }

    // Picks Low/Mid/High from device specs on launch; player can override in settings.
    public static class GraphicsTier
    {
        const string OverrideKey = "gfx_tier_override";

        public static Tier Current { get; private set; }
        public static event Action<Tier> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Apply(PlayerPrefs.HasKey(OverrideKey) ? (Tier)PlayerPrefs.GetInt(OverrideKey) : Detect());
        }

        public static Tier Detect()
        {
            int ramMb = SystemInfo.systemMemorySize;
            int cores = SystemInfo.processorCount;
            if (ramMb < 4000 || cores < 6) return Tier.Low;
            if (ramMb < 8000) return Tier.Mid;
            return Tier.High;
        }

        public static void SetOverride(Tier tier)
        {
            PlayerPrefs.SetInt(OverrideKey, (int)tier);
            Apply(tier);
        }

        public static void ClearOverride()
        {
            PlayerPrefs.DeleteKey(OverrideKey);
            Apply(Detect());
        }

        static void Apply(Tier tier)
        {
            Current = tier;
            QualitySettings.vSyncCount = 0;
            float refresh = (float)Screen.currentResolution.refreshRateRatio.value;
            Application.targetFrameRate = tier == Tier.High ? Mathf.Clamp(Mathf.RoundToInt(refresh), 60, 120) : 60;

            // Editing the URP asset in the Editor would persist to disk, so only do it on device.
            if (!Application.isEditor && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                urp.renderScale = tier == Tier.Low ? 0.75f : 1f;

            Debug.Log($"[GraphicsTier] {tier} (RAM {SystemInfo.systemMemorySize} MB, {SystemInfo.processorCount} cores, GPU {SystemInfo.graphicsDeviceName})");
            Changed?.Invoke(tier);
        }
    }
}
