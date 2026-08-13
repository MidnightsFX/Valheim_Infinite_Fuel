using BepInEx.Configuration;
using System.Collections.Generic;

namespace ValheimInfiniteFire.common
{
    internal class ValConfig
    {
        public static ConfigFile cfg;

        public static Dictionary<string, ConfigEntry<bool>> NoFuelConfigs = new Dictionary<string, ConfigEntry<bool>>();
        public static Dictionary<string, ConfigEntry<bool>> SmokeConfigs = new Dictionary<string, ConfigEntry<bool>>();
        public static ConfigEntry<bool> EnableDebugMode;
        public static ConfigEntry<bool> SmokeDamage;
        public static ConfigEntry<bool> SmokeSuffocation;

        public ValConfig(ConfigFile cf) {
            cfg = cf;
            cfg.SaveOnConfigSet = true;
            CreateConfigValues(cf);
        }

        private void CreateConfigValues(ConfigFile Config) {
            EnableDebugMode = Config.Bind("Client config", "EnableDebugMode", false,
                new ConfigDescription("Enables Debug logging.",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true }));
            EnableDebugMode.SettingChanged += Logger.enableDebugLogging;
            Logger.CheckEnableDebugLogging();

            SmokeDamage = BindServerConfig("Smoke gameplay", "SmokeDamage", true,
                "Smoke applies the Smoked status effect (2 damage per second) to anyone standing in it. " +
                "Set false to make every character - players, tames and monsters - ignore smoke completely, " +
                "which also skips the smoke proximity check each of them runs every 2 seconds.");
            SmokeDamage.SettingChanged += (sender, args) => SmokeControl.ApplySmokeDamage();

            SmokeSuffocation = BindServerConfig("Smoke gameplay", "SmokeSuffocation", true,
                "Smoke can choke fires. Set false so fireplaces are never reported as blocked by their own smoke, " +
                "smelters, kilns and blast furnaces never stall on smoke, and spreading fires are never put out by it. " +
                "Spreading fires still expire after 30 seconds and still die in the rain.");
            SmokeSuffocation.SettingChanged += (sender, args) => SmokeControl.ApplyFireSuffocation();
        }

        /// <summary>
        ///  Helper to bind configs for bool types
        /// </summary>
        /// <param name="config_file"></param>
        /// <param name="catagory"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <param name="acceptableValues"></param>>
        /// <param name="advanced"></param>
        /// <returns></returns>
        public static ConfigEntry<bool> BindServerConfig(string catagory, string key, bool value, string description, AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return cfg.Bind(catagory, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
                );
        }
    }
}
