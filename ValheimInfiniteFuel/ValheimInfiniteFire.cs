using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Managers;
using System.Collections.Generic;
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
        public const string PluginVersion = "1.1.0";

        public ValConfig cfg;
        public static ManualLogSource Log;
        public static Dictionary<string, ItemDrop> smelterOriginalFuel = new Dictionary<string, ItemDrop>() { };

        public void Awake() {
            
            Log = this.Logger;
            cfg = new ValConfig(Config);

            PrefabManager.OnPrefabsRegistered += FindAllFireTypes;
            PrefabManager.OnPrefabsRegistered += FindAllSmelters;
            common.Logger.LogDebug("Lets Light it up");
        }

        public static void FindAllFireTypes() {
            foreach(Fireplace fire in Resources.FindObjectsOfTypeAll<Fireplace>()) {
                string prefabname = Utils.GetPrefabName(fire.gameObject.name);
                string firename = $"{prefabname}-{Localization.instance.Localize(fire.m_name)}";
                ConfigEntry<bool> enableFire = ValConfig.BindServerConfig("InfiniteFire", $"{firename}", true, "Enable infinite fuel for this fire.");
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

        public static void FindAllSmelters()
        {
            foreach(Smelter smelter in Resources.FindObjectsOfTypeAll<Smelter>())
            {
                if (smelterOriginalFuel.Keys.Contains(smelter.m_name)) { continue; }
                if (smelter.m_fuelItem == null) { continue; }
                smelterOriginalFuel.Add(smelter.m_name, smelter.m_fuelItem);
                string prefabname = Utils.GetPrefabName(smelter.gameObject.name);
                string firename = $"{prefabname}-{Localization.instance.Localize(smelter.m_name)}";
                ConfigEntry<bool> enableFuel = ValConfig.BindServerConfig("InfiniteFuel", $"{firename}", false, "Enable infinite fuel for this smelter.");
                common.Logger.LogDebug($"Registering {firename} with InfiniteFuel {enableFuel.Value}");
                if (enableFuel.Value) {
                    smelter.m_fuelItem = null;
                } else {
                    smelter.m_fuelItem = smelterOriginalFuel[smelter.m_name];
                }

                enableFuel.SettingChanged += (sender, args) => {
                    foreach (Smelter smelt in Resources.FindObjectsOfTypeAll<Smelter>().Where(sm => sm.gameObject.name.StartsWith(prefabname))) {
                        common.Logger.LogDebug($"Updating {smelt.name} to InfiniteFuel:{enableFuel.Value}");
                        if (enableFuel.Value) {
                            smelter.m_fuelItem = null;
                        } else {
                            smelter.m_fuelItem = smelterOriginalFuel[smelter.m_name];
                        }
                    }
                };
            }
        }
    }
}