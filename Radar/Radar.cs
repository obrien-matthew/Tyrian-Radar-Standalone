using EFT;
using System.Collections.Generic;
using Comfort.Common;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Radar.Patches;
using UnityEngine;

namespace Radar
{
    [BepInPlugin("Tyrian.Radar", "Radar", "1.1.5")]
    public class Radar : BaseUnityPlugin
    {
        internal static Radar? Instance { get; private set; }

        public static Dictionary<GameObject, HashSet<Material>> objectsMaterials = new Dictionary<GameObject, HashSet<Material>>();

        const string baseSettings = "Base Settings";
        const string advancedSettings = "Advanced Settings";
        const string colorSettings = "Color Settings";
        const string radarSettings = "Radar Settings";

        public static ConfigEntry<string> radarLanguage = null!;
        public static ConfigEntry<bool> radarEnableConfig = null!;
        public static ConfigEntry<bool> radarEnablePulseConfig = null!;
        public static ConfigEntry<bool> radarEnableCorpseConfig = null!;
        public static ConfigEntry<bool> radarEnableLootConfig = null!;
        public static ConfigEntry<KeyboardShortcut> radarEnableShortCutConfig = null!;
        public static ConfigEntry<KeyboardShortcut> radarEnableCorpseShortCutConfig = null!;
        public static ConfigEntry<KeyboardShortcut> radarEnableLootShortCutConfig = null!;

        public static ConfigEntry<float> radarSizeConfig = null!;
        public static ConfigEntry<float> radarBlipSizeConfig = null!;
        public static ConfigEntry<float> radarDistanceScaleConfig = null!;
        public static ConfigEntry<float> radarYHeightThreshold = null!;
        public static ConfigEntry<float> radarOffsetYConfig = null!;
        public static ConfigEntry<float> radarOffsetXConfig = null!;
        public static ConfigEntry<float> radarRangeConfig = null!;
        public static ConfigEntry<float> radarScanInterval = null!;
        public static ConfigEntry<float> radarLootThreshold = null!;

        public static ConfigEntry<Color> bossBlipColor = null!;
        public static ConfigEntry<Color> usecBlipColor = null!;
        public static ConfigEntry<Color> bearBlipColor = null!;
        public static ConfigEntry<Color> scavBlipColor = null!;
        public static ConfigEntry<Color> bestLootBlipColor = null!;
        public static ConfigEntry<Color> betterLootBlipColor = null!;
        public static ConfigEntry<Color> goodLootBlipColor = null!;
        public static ConfigEntry<Color> corpseBlipColor = null!;
        public static ConfigEntry<Color> lootBlipColor = null!;
        public static ConfigEntry<Color> backgroundColor = null!;


        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Radar Plugin Enabled.");
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Add a custom configuration option for the Apply button
            radarLanguage = Config.Bind<string>(baseSettings, "Language", "EN",
                new ConfigDescription("Preferred language, if not available will tried English",
                new AcceptableValueList<string>("EN", "ZH", "RU")));

            radarEnableConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_enable"), true);
            radarEnableShortCutConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_enable_shortcut"), new KeyboardShortcut(KeyCode.F10));
            radarEnablePulseConfig = Config.Bind(baseSettings, Locales.GetTranslatedString("radar_pulse_enable"), true, Locales.GetTranslatedString("radar_pulse_enable_info"));

            radarEnableCorpseConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_corpse_enable"), false);
            radarEnableCorpseShortCutConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_corpse_shortcut"), new KeyboardShortcut(KeyCode.F11));
            radarEnableLootConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_loot_enable"), false);
            radarEnableLootShortCutConfig = Config.Bind(advancedSettings, Locales.GetTranslatedString("radar_loot_shortcut"), new KeyboardShortcut(KeyCode.F9));

            radarSizeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_hud_size"), 0.8f,
                new ConfigDescription(Locales.GetTranslatedString("radar_hud_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            radarBlipSizeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_blip_size"), 0.7f,
                new ConfigDescription(Locales.GetTranslatedString("radar_blip_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            radarDistanceScaleConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_distance_scale"), 0.7f,
                new ConfigDescription(Locales.GetTranslatedString("radar_distance_scale_info"), new AcceptableValueRange<float>(0.1f, 2f)));
            radarYHeightThreshold = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_y_height_threshold"), 1f,
                new ConfigDescription(Locales.GetTranslatedString("radar_y_height_threshold_info"), new AcceptableValueRange<float>(1f, 4f)));
            radarOffsetXConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_x_position"), 0f,
                new ConfigDescription(Locales.GetTranslatedString("radar_x_position_info"), new AcceptableValueRange<float>(-4000f, 4000f)));
            radarOffsetYConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_y_position"), 0f,
                new ConfigDescription(Locales.GetTranslatedString("radar_y_position_info"), new AcceptableValueRange<float>(-4000f, 4000f)));
            radarRangeConfig = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_range"), 128f,
                new ConfigDescription(Locales.GetTranslatedString("radar_range_info"), new AcceptableValueRange<float>(32f, 512f)));
            radarScanInterval = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_scan_interval"), 3f,
                new ConfigDescription(Locales.GetTranslatedString("radar_scan_interval_info"), new AcceptableValueRange<float>(0.1f, 30f)));
            radarLootThreshold = Config.Bind<float>(radarSettings, Locales.GetTranslatedString("radar_loot_threshold"), 30000f,
                new ConfigDescription(Locales.GetTranslatedString("radar_loot_threshold_info"), new AcceptableValueRange<float>(1000f, 100000f)));

            bossBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_boss_blip_color"), new Color(1f, 0f, 1f));
            scavBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_scav_blip_color"), new Color(1f, 1f, 0f));
            usecBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_usec_blip_color"), new Color(1f, 0f, 0f));
            bearBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_bear_blip_color"), new Color(1f, 0.25f, 0f));
            corpseBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_corpse_blip_color"), new Color(0.5f, 0.5f, 0.5f, 0.5f));
            bestLootBlipColor = Config.Bind<Color>(colorSettings, "Loot (Best) Blip Color", new Color(0f, 1f, 0f, 0.9f));
            betterLootBlipColor = Config.Bind<Color>(colorSettings, "Loot (Better) Blip Color", new Color(0.33f, 1f, 0.33f, 0.75f));
            goodLootBlipColor = Config.Bind<Color>(colorSettings, "Loot (Good) Blip Color", new Color(0.66f, 1f, 0.66f, 0.5f));
            lootBlipColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_loot_blip_color"), new Color(1f, 1f, 1f, 0.25f));
            backgroundColor = Config.Bind<Color>(colorSettings, Locales.GetTranslatedString("radar_background_blip_color"), new Color(0f, 0.7f, 0.85f));

            AssetBundleManager.LoadAssetBundle();

            new GameStartPatch().Enable();
        }
    }
}