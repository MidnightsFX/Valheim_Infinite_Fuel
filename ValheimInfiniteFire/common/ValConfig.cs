using BepInEx.Configuration;
using System.Collections.Generic;

namespace ValheimInfiniteFire.common
{
    internal class ValConfig
    {
        public static ConfigFile cfg;

        public static Dictionary<string, ConfigEntry<bool>> NoFuelConfigs = new Dictionary<string, ConfigEntry<bool>>();
        public static Dictionary<string, ConfigEntry<bool>> SmokeConfigs = new Dictionary<string, ConfigEntry<bool>>();
        public static Dictionary<string, ConfigEntry<string>> ScheduleConfigs = new Dictionary<string, ConfigEntry<string>>();
        public static ConfigEntry<bool> EnableDebugMode;
        public static ConfigEntry<float> ScheduleCheckInterval;
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

            ScheduleCheckInterval = BindServerConfig("Schedule", "ScheduleCheckInterval", 10f,
                "How many in game seconds pass before a scheduled fire rechecks the clock. A Valheim day is 1200 " +
                "seconds and the clock runs faster at night, an hour after dark being 30 seconds against 70 in " +
                "daylight, so 10 puts a boundary up to about 20 in game minutes late at night and 9 by day. Lower " +
                "is near enough free, the check is a handful of comparisons and only runs while a fire is loaded.",
                new AcceptableValueRange<float>(1f, 120f), true);
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

        /// <summary>
        ///  Helper to bind configs for float types
        /// </summary>
        /// <param name="catagory"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <param name="acceptableValues"></param>
        /// <param name="advanced"></param>
        /// <returns></returns>
        public static ConfigEntry<float> BindServerConfig(string catagory, string key, float value, string description, AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return cfg.Bind(catagory, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
                );
        }

        /// <summary>
        ///  Helper to bind configs for string types
        /// </summary>
        /// <param name="catagory"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <param name="acceptableValues"></param>
        /// <param name="advanced"></param>
        /// <returns></returns>
        public static ConfigEntry<string> BindServerConfig(string catagory, string key, string value, string description, AcceptableValueList<string> acceptableValues = null, bool advanced = false) {
            return cfg.Bind(catagory, key, value,
                new ConfigDescription(description,
                    acceptableValues,
                new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
                );
        }
    }
}
