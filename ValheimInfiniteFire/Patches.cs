using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ValheimInfiniteFire.common;

namespace ValheimInfiniteFire {
    internal static class Patches {

        /// <summary>
        ///  Infinite fuel stations report a full tank and never write a burn back, so the fuel actually stored in
        ///  the ZDO stays exactly what players put in. This is raised around the two places that must see that
        ///  real value: adding fuel, and refunding it when the station is destroyed.
        /// </summary>
        private static bool UseStoredFuel;

        private static bool HasInfiniteFuel(Component station) {
            if (UseStoredFuel) { return false; }
            ValConfig.NoFuelConfigs.TryGetValue(Utils.GetPrefabName(station.gameObject.name), out ConfigEntry<bool> status);
            return status != null && status.Value;
        }

        /// <summary>
        ///  Report a full tank, not just "some" fuel. A token 1 can be less than m_fuelPerProduct, so production
        ///  mods that make several products a tick and check for a whole product's worth first would stall, and
        ///  auto fuel mods would keep feeding it. m_fuelPerProduct can be configured above m_maxFuel, so cover
        ///  that too.
        /// </summary>
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.GetFuel))]
        internal static class SmelterGetFuel {
            [HarmonyPostfix]
            internal static void Postfix(Smelter __instance, ref float __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = Mathf.Max(__instance.m_maxFuel, __instance.m_fuelPerProduct);
            }
        }

        /// <summary>
        ///  UpdateSmelter writes GetFuel() minus the burn back every tick. Against the full tank reported above that
        ///  would store fuel nobody added, and DropAllItems refunds whatever the ZDO holds, so every destroyed
        ///  smelter would pay out a tank of coal. Skipping the write leaves the stored fuel untouched.
        /// </summary>
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.SetFuel))]
        internal static class SmelterSetFuel {
            [HarmonyPrefix]
            internal static bool Prefix(Smelter __instance) {
                return !HasInfiniteFuel(__instance);
            }
        }

        /// <summary>
        ///  Vanilla won't offer fuel to a full station, but auto fuel mods that read the ZDO directly still send it.
        ///  Store it for real so it is refunded on destroy instead of vanishing into a blocked SetFuel.
        /// </summary>
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.RPC_AddFuel))]
        internal static class SmelterAddStoredFuel {
            [HarmonyPriority(Priority.First)]
            [HarmonyPrefix]
            internal static void Prefix() { UseStoredFuel = true; }

            [HarmonyFinalizer]
            internal static void Finalizer() { UseStoredFuel = false; }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetFuel))]
        internal static class CookerGetFuel {
            [HarmonyPostfix]
            internal static void Postfix(CookingStation __instance, ref float __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = __instance.m_maxFuel;
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetFuel))]
        internal static class CookerSetFuel {
            [HarmonyPrefix]
            internal static bool Prefix(CookingStation __instance) {
                return !HasInfiniteFuel(__instance);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_AddFuel))]
        internal static class CookerAddStoredFuel {
            [HarmonyPriority(Priority.First)]
            [HarmonyPrefix]
            internal static void Prefix() { UseStoredFuel = true; }

            [HarmonyFinalizer]
            internal static void Finalizer() { UseStoredFuel = false; }
        }

        /// <summary>
        ///  Unlike the smelter, the cooking station refunds fuel through GetFuel(), so without this every destroyed
        ///  oven drops a full tank of wood it never held.
        /// </summary>
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.DropAllItems))]
        internal static class CookerRefundStoredFuel {
            [HarmonyPriority(Priority.First)]
            [HarmonyPrefix]
            internal static void Prefix() { UseStoredFuel = true; }

            [HarmonyFinalizer]
            internal static void Finalizer() { UseStoredFuel = false; }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnHoverFuelSwitch))]
        internal static class OnHoverDisplayNoFuelRequirementCookingStation {
            [HarmonyAfter("shudnal.MyLittleUI")]
            [HarmonyPostfix]
            internal static void Postfix(CookingStation __instance, ref string __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = "No Fuel Needed.";
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddFuel))]
        internal static class OnHoverDisplayNoFuelRequirementSmelter {
            [HarmonyAfter("shudnal.MyLittleUI")]
            [HarmonyPostfix]
            internal static void Postfix(Smelter __instance, ref string __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = "No Fuel Needed.";
            }
        }

        /// <summary>
        ///  Decouples smoke from the gameplay it chokes. IsBlocked feeds Fireplace.CheckUnderTerrain and
        ///  Smelter.UpdateSmoke, which run at 0.25Hz and 1Hz per loaded piece, so this costs nothing per frame.
        /// </summary>
        [HarmonyPatch(typeof(SmokeSpawner), nameof(SmokeSpawner.IsBlocked))]
        internal static class SmokeSpawnerNeverBlocked {
            [HarmonyPostfix]
            internal static void Postfix(ref bool __result) {
                if (ValConfig.SmokeSuffocation.Value) { return; }
                __result = false;
            }
        }

        /// <summary>
        ///  Reads the ZDO off a fireplace, or null for a prefab asset or a placement ghost, neither of which has
        ///  one. Resources.FindObjectsOfTypeAll hands out both, and a ghost has its ZNetView destroyed outright.
        /// </summary>
        private static ZDO GetFireZDO(Fireplace fireplace) {
            ZNetView nview = fireplace.m_nview;
            if (nview == null || !nview.IsValid()) { return null; }
            return nview.GetZDO();
        }

        /// <summary>
        ///  The schedule. IsBurning is the single gate every consumer of "is this fire lit" goes through, so
        ///  forcing it false darkens the piece through UpdateState, stops its fuel draining at UpdateFireplace and
        ///  stops it igniting or throwing cinders, all in vanilla code. Nothing is written to the world, so there
        ///  is no ZDO ownership to wait for, no sweep to run at dawn, and nothing left behind if the mod is
        ///  removed.
        ///
        ///  Postfix, not prefix, so vanilla decides first and a scheduled fire is only ever forced dark.
        ///
        ///  This runs roughly once a second per loaded fireplace, off UpdateFireplace. With nothing scheduled that
        ///  is a bool test and a Count compare. With a schedule set it is an int field read and a HashSet lookup,
        ///  which is why the piece is identified by ZDO.GetPrefab and not Utils.GetPrefabName, which allocates a
        ///  substring every call. The clock itself is only re-read once per ScheduleCheckInterval.
        /// </summary>
        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.IsBurning))]
        internal static class FireplaceScheduledOff {
            [HarmonyPostfix]
            internal static void Postfix(Fireplace __instance, ref bool __result) {
                if (!__result || FireSchedule.Count == 0) { return; }
                ZDO zdo = GetFireZDO(__instance);
                if (zdo == null || !FireSchedule.IsOffNow(zdo)) { return; }
                __result = false;
            }
        }

        /// <summary>
        ///  Lets a player light a scheduled fire by hand, which then holds until the next boundary.
        ///
        ///  The guard mirrors the vanilla toggle test in Fireplace.Interact exactly, so refuelling and the hold
        ///  and alt paths are untouched. Without this the first press would toggle the stored state to off while
        ///  the piece was already dark, so it would take two presses before anything visibly happened.
        /// </summary>
        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
        internal static class FireplaceScheduleOverride {
            [HarmonyPrefix]
            internal static bool Prefix(Fireplace __instance, bool hold, bool alt, ref bool __result) {
                if (FireSchedule.Count == 0 || hold || alt || !__instance.m_canTurnOff) { return true; }
                ZDO zdo = GetFireZDO(__instance);
                if (zdo == null || zdo.GetFloat(ZDOVars.s_fuel) <= 0f) { return true; }
                // Already false once this fire has been lit by hand, so a second press falls through to the
                // vanilla toggle and puts it out again.
                if (!FireSchedule.IsOffNow(zdo)) { return true; }

                FireSchedule.LitByHand(__instance.m_nview);
                // As far as the world is concerned this fire is already on and only the schedule was holding it
                // dark, so let it light where it stands. If a player had switched it off by hand, fall through and
                // let vanilla toggle it back on.
                if (zdo.GetInt(ZDOVars.s_state, 1) != 1) { return true; }
                // Vanilla repaints from RPC_ToggleOn, and we just skipped it. Without this the flame waits for
                // the next UpdateFireplace tick, up to 2 seconds after the player pressed the key.
                __instance.UpdateState();
                __result = true;
                return false;
            }
        }

        /// <summary>
        ///  With infinite fuel on, GetHoverText returns an empty string, so a scheduled fire would give the player
        ///  nothing at all to explain why it is dark. Only runs while someone is looking at the piece.
        /// </summary>
        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
        internal static class FireplaceScheduleHover {
            [HarmonyAfter("shudnal.MyLittleUI")]
            [HarmonyPostfix]
            internal static void Postfix(Fireplace __instance, ref string __result) {
                if (FireSchedule.Count == 0) { return; }
                ZDO zdo = GetFireZDO(__instance);
                if (zdo == null || !FireSchedule.IsOffNow(zdo)) { return; }
                float until = FireSchedule.OffUntil(zdo);
                if (until < 0f) { return; }

                if (string.IsNullOrEmpty(__result)) {
                    __result = Localization.instance.Localize(__instance.m_name);
                }
                __result += $"\nScheduled off until {FireSchedule.FormatHour(until)}";
            }
        }
    }
}
