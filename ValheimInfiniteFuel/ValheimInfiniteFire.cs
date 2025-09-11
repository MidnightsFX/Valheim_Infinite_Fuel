using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;
using ValheimInfiniteFire.common;

namespace ValheimInfiniteFire
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ValheimInfiniteFire : BaseUnityPlugin
    {
        public const string PluginGUID = "MidnightsFX.InfiniteFire";
        public const string PluginName = "InfiniteFire";
        public const string PluginVersion = "1.0.0";

        public ValConfig cfg;
        public static ManualLogSource Log;

        public void Awake() {
            
            Log = this.Logger;
            cfg = new ValConfig(Config);

            PrefabManager.OnVanillaPrefabsAvailable += FindAllFireTypes;
            common.Logger.LogDebug("Lets Light it up");
        }

        public static void FindAllFireTypes() {
            foreach(Fireplace fire in Resources.FindObjectsOfTypeAll<Fireplace>()) {
                string prefabname = Utils.GetPrefabName(fire.gameObject.name);
                string firename = $"{prefabname}-{Localization.instance.Localize(fire.m_name)}";
                ConfigEntry<bool> enableFire = ValConfig.BindServerConfig("InfiniteFire", $"{firename}", true, "Enable infinite fuel for this.");
                common.Logger.LogDebug($"Registering {firename} with infinitefire {enableFire.Value}");
                fire.m_infiniteFuel = enableFire.Value;
                enableFire.SettingChanged += (sender, args) => {
                    foreach(Fireplace fp in Resources.FindObjectsOfTypeAll<Fireplace>().Where(fp => fp.gameObject.name.StartsWith(prefabname))) {
                        common.Logger.LogDebug($"Updating {fp.name} to InfiniteFire:{enableFire.Value}");
                        fp.m_infiniteFuel = enableFire.Value;
                    }
                };
            }
        }
    }
}