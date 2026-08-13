using BepInEx.Configuration;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ValheimInfiniteFire.common;

namespace ValheimInfiniteFire
{
    /// <summary>
    ///  Per piece smoke toggles, plus the two switches for smoke's gameplay effects.
    ///
    ///  Smoke is turned off by deactivating the spawner's GameObject, not by disabling or destroying the
    ///  component. That drops it out of SmokeSpawner.Instances, the list MonoUpdaters ticks every frame, so a
    ///  piece with smoke off costs nothing at runtime. It also makes SmokeSpawner.IsBlocked() take its
    ///  !activeInHierarchy branch, a physics test that goes false once the last puff clears, so fireplaces stay
    ///  lit and smelters keep producing. Disabling the component instead would leave activeInHierarchy true and
    ///  IsBlocked() would read Time.time - m_lastSpawnTime > 4, reporting blocked forever.
    /// </summary>
    internal static class SmokeControl
    {
        /// <summary>"prefab|relative/path" to the spawner GameObject's activeSelf as it shipped. forge, blackforge
        /// and BatteringRam ship with theirs already off, so re-enabling restores this rather than SetActive(true).</summary>
        private static readonly Dictionary<string, bool> OriginalSpawnerActive = new Dictionary<string, bool>();
        /// <summary>Character prefab name to its shipped m_tolerateSmoke.</summary>
        private static readonly Dictionary<string, bool> OriginalTolerateSmoke = new Dictionary<string, bool>();
        /// <summary>Fire prefab name to its shipped m_smokeDieChance, which is 1f, not the 0.5f field default.</summary>
        private static readonly Dictionary<string, float> OriginalFireDieChance = new Dictionary<string, float>();
        /// <summary>OnPrefabsRegistered fires on every world entry, so only ever subscribe once per prefab.</summary>
        private static readonly HashSet<string> SubscribedSmoke = new HashSet<string>();

        public static void OnPrefabsRegistered() {
            DiscoverSmokeSpawners();
            ApplySmokeDamage();
            ApplyFireSuffocation();
        }

        private static void DiscoverSmokeSpawners() {
            foreach (SmokeSpawner spawner in Resources.FindObjectsOfTypeAll<SmokeSpawner>()) {
                if (spawner == null) { continue; }
                Transform root = spawner.transform.root;
                // Buildable pieces only. This also keeps us off smokebomb_explosion, the one prefab that puts its
                // SmokeSpawner on the root next to a ZNetView, where deactivating would break the whole bomb.
                if (root.GetComponent<Piece>() == null) { continue; }

                string prefabname = Utils.GetPrefabName(root.gameObject.name);
                if (!ValConfig.SmokeConfigs.TryGetValue(prefabname, out ConfigEntry<bool> smoke)) {
                    smoke = ValConfig.BindServerConfig("Smoke", prefabname, true,
                        "Enable smoke for this piece. Disabling switches its smoke spawner off entirely, so it costs nothing at runtime.");
                    ValConfig.SmokeConfigs[prefabname] = smoke;
                    common.Logger.LogDebug($"Registering {prefabname} with Smoke {smoke.Value}");
                }
                if (SubscribedSmoke.Add(prefabname)) {
                    smoke.SettingChanged += (sender, args) => ApplySmoke(prefabname);
                }
                ApplySpawnerState(spawner, root, prefabname, smoke.Value);
            }
        }

        private static void ApplySmoke(string prefabname) {
            if (!ValConfig.SmokeConfigs.TryGetValue(prefabname, out ConfigEntry<bool> smoke)) { return; }
            common.Logger.LogDebug($"Updating {prefabname} to Smoke:{smoke.Value}");

            // One sweep reaches the prefab asset and every placed clone, they all live in the same object set.
            foreach (SmokeSpawner spawner in Resources.FindObjectsOfTypeAll<SmokeSpawner>()) {
                if (spawner == null) { continue; }
                Transform root = spawner.transform.root;
                if (root.GetComponent<Piece>() == null) { continue; }
                // Exact match, not StartsWith: fire_pit must not drag fire_pit_iron along with it.
                if (Utils.GetPrefabName(root.gameObject.name) != prefabname) { continue; }
                ApplySpawnerState(spawner, root, prefabname, smoke.Value);
            }
        }

        private static void ApplySpawnerState(SmokeSpawner spawner, Transform root, string prefabname, bool enabled) {
            string key = prefabname + "|" + RelativePath(spawner.transform, root);
            if (!OriginalSpawnerActive.TryGetValue(key, out bool original)) {
                original = spawner.gameObject.activeSelf;
                OriginalSpawnerActive[key] = original;
            }
            bool wanted = enabled && original;
            if (spawner.gameObject.activeSelf != wanted) { spawner.gameObject.SetActive(wanted); }
        }

        /// <summary>
        ///  Path from root (exclusive) down to child, so a clone keys the same as the prefab it came from.
        ///  Never run this through Utils.GetPrefabName, which truncates at the first space, and BatteringRam
        ///  has a child named "kiln engine".
        /// </summary>
        private static string RelativePath(Transform child, Transform root) {
            StringBuilder path = new StringBuilder(child.name);
            for (Transform parent = child.parent; parent != null && parent != root; parent = parent.parent) {
                path.Insert(0, '/').Insert(0, parent.name);
            }
            return path.ToString();
        }

        /// <summary>
        ///  m_tolerateSmoke is read in exactly two places, Character.UpdateSmoke and SE_Smoke.CanAdd, so setting it
        ///  covers both the damage and the CanAdd gate with no patch, and skips the 2 second proximity check too.
        /// </summary>
        public static void ApplySmokeDamage() {
            bool vanilla = ValConfig.SmokeDamage.Value;
            common.Logger.LogDebug($"Applying SmokeDamage:{vanilla}");

            // Covers the prefabs, so future spawns inherit it, and everything already in the scene.
            foreach (Character character in Resources.FindObjectsOfTypeAll<Character>()) {
                if (character == null) { continue; }
                string prefabname = Utils.GetPrefabName(character.gameObject.name);
                if (!OriginalTolerateSmoke.TryGetValue(prefabname, out bool original)) {
                    original = character.m_tolerateSmoke;
                    OriginalTolerateSmoke[prefabname] = original;
                }
                character.m_tolerateSmoke = vanilla ? original : true;

                // Smoked has m_ttl 0, so it never expires on its own, and its only removal path is the branch of
                // Character.UpdateSmoke that m_tolerateSmoke now skips. Anyone smoked right this second would keep
                // taking damage forever, so clear it explicitly. Null on prefabs, SEMan is built in Awake.
                if (!vanilla && character.m_seman != null) {
                    character.m_seman.RemoveStatusEffect(SEMan.s_statusEffectSmoked, true);
                }
            }
        }

        /// <summary>
        ///  m_smokeDieChance is the destroy roll in Fire.UpdateFire and is read nowhere else. At 0 the guard
        ///  clause (m_smokeDieChance &lt; 1 &amp;&amp; Random.Range(0f, 1f) >= m_smokeDieChance) is always true, so the
        ///  fire is never destroyed by smoke. Only Fire and HouseFire carry the component.
        /// </summary>
        public static void ApplyFireSuffocation() {
            bool vanilla = ValConfig.SmokeSuffocation.Value;
            common.Logger.LogDebug($"Applying SmokeSuffocation:{vanilla}");

            foreach (Fire fire in Resources.FindObjectsOfTypeAll<Fire>()) {
                if (fire == null) { continue; }
                string prefabname = Utils.GetPrefabName(fire.gameObject.name);
                if (!OriginalFireDieChance.TryGetValue(prefabname, out float original)) {
                    original = fire.m_smokeDieChance;
                    OriginalFireDieChance[prefabname] = original;
                }
                fire.m_smokeDieChance = vanilla ? original : 0f;
            }
        }
    }
}
