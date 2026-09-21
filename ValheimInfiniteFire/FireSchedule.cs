using BepInEx.Configuration;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using ValheimInfiniteFire.common;

namespace ValheimInfiniteFire
{
    /// <summary>
    ///  A per piece clock: each toggleable fire type can be given a window of the in game day it stays unlit.
    ///
    ///  This never writes fire state into the world. Fireplace.IsBurning() is the one gate every consumer of
    ///  "is this fire lit" goes through, so a postfix forcing it false turns the visuals off (UpdateState), stops
    ///  the fuel drain (UpdateFireplace), and stops ignite, cinders and snow melt, all through vanilla own code.
    ///
    ///  The alternative, writing ZDOVars.s_state = 2 on a timer, was tried and rejected. It needs ZDO ownership,
    ///  which is not granted until ZDOMan.ReleaseZDOS runs on the server and round trips back, so walking into a
    ///  base spawns torches the local peer cannot yet correct. It only reaches loaded pieces, so any base nobody
    ///  is standing in is missed. And it leaves an unlit state behind in the save after the mod is removed.
    ///  Deciding per call instead means there is nothing to apply, nothing to own and nothing to undo.
    /// </summary>
    internal static class FireSchedule
    {
        private readonly struct Window
        {
            public readonly float From;
            public readonly float To;
            public Window(float from, float to) { From = from; To = to; }
        }

        /// <summary>Prefab hash, as ZDO.GetPrefab() reports it, to its off window. Enabled types only, so an unused
        /// feature costs one Count compare and a world with two scheduled types compares two windows.</summary>
        private static readonly Dictionary<int, Window> Windows = new Dictionary<int, Window>();
        /// <summary>Prefab hashes inside their off window as of the last clock read, each mapped to the world
        /// second that window opened. Being in here means dark, and the start time is what dates a manual light
        /// so it expires on its own at the next boundary.</summary>
        private static readonly Dictionary<int, long> OffSince = new Dictionary<int, long>();
        /// <summary>OnPrefabsRegistered fires on every world entry, so only ever subscribe once per prefab.</summary>
        private static readonly HashSet<string> SubscribedSchedule = new HashSet<string>();
        /// <summary>key=value of every rejected setting already warned about. ConfigurationManager raises
        /// SettingChanged per keystroke, so without this a typo logs a line for every partial value.</summary>
        private static readonly HashSet<string> WarnedValues = new HashSet<string>();

        private static double LastClockRead = double.NegativeInfinity;
        private static bool SubscribedToSync;

        /// <summary>
        ///  ZDO key holding the world second a player last lit this fire by hand. It lives in the world rather
        ///  than in a local set so every client sees the same torch lit, and it dates itself out of relevance
        ///  rather than needing to be cleared. Nothing but this mod reads it, so it is inert once the mod is gone.
        /// </summary>
        private static readonly int LitByHandKey = "InfiniteFire_litByHand".GetStableHashCode();

        public const string SettingDescription =
            "Hours of the in game day this piece stays unlit, as Start-End on a 24 hour clock. 06:00-18:00 keeps it " +
            "dark through the day and lit at night. Accepts 6, 6.5 and 06:30, and wraps past midnight (22:00-04:00). " +
            "Leave empty for no schedule. A start equal to its end is ambiguous and is treated as no schedule.";

        /// <summary>Number of scheduled types. The patches read this first so an unused feature costs nothing.</summary>
        public static int Count => Windows.Count;

        public static void OnPrefabsRegistered() {
            foreach (Fireplace fire in Resources.FindObjectsOfTypeAll<Fireplace>()) {
                // Fires that cannot be toggled are left out on purpose. Fireplace.Interact refuses to relight them,
                // so a player could never override or recover one that the schedule had put out.
                if (fire == null || !fire.m_canTurnOff) { continue; }

                string prefabname = Utils.GetPrefabName(fire.gameObject.name);
                if (!ValConfig.ScheduleConfigs.TryGetValue(prefabname, out ConfigEntry<string> schedule)) {
                    schedule = ValConfig.BindServerConfig("Schedule", prefabname, "", SettingDescription);
                    ValConfig.ScheduleConfigs[prefabname] = schedule;
                    common.Logger.LogDebug($"Registering {prefabname} with Schedule [{schedule.Value}]");
                }
                if (SubscribedSchedule.Add(prefabname)) {
                    schedule.SettingChanged += (sender, args) => ReadConfig();
                }
            }

            // A server push that lands before the entries above exist is dropped by Jotunn, and the local default
            // would be used silently. Re-reading on sync closes that window.
            if (!SubscribedToSync) {
                SubscribedToSync = true;
                SynchronizationManager.OnConfigurationSynchronized += (sender, args) => ReadConfig();
            }
            ReadConfig();
        }

        /// <summary>
        ///  Parses every schedule setting into Windows. Called on world entry, on any setting change and on config
        ///  sync, so parsing never happens on the path the patches take.
        /// </summary>
        private static void ReadConfig() {
            Windows.Clear();
            OffSince.Clear();
            LastClockRead = double.NegativeInfinity;

            foreach (KeyValuePair<string, ConfigEntry<string>> entry in ValConfig.ScheduleConfigs) {
                string raw = entry.Value.Value;
                if (string.IsNullOrEmpty(raw) || raw.Trim().Length == 0) { continue; }

                if (!TryParseWindow(raw, out float from, out float to)) {
                    // Warning, not debug. A typo that silently leaves a base lit is the failure nobody diagnoses.
                    if (WarnedValues.Add(entry.Key + "=" + raw)) {
                        common.Logger.LogWarning($"Ignoring Schedule for {entry.Key}, [{raw}] is not a Start-End time range.");
                    }
                    continue;
                }
                Windows[entry.Key.GetStableHashCode()] = new Window(from, to);
                common.Logger.LogDebug($"Scheduling {entry.Key} off from {from:0.##} to {to:0.##}");
            }
        }

        /// <summary>
        ///  Whether this fire should be dark right now. Re-reads the clock at most once per ScheduleCheckInterval,
        ///  and only while a fire is actually loaded and asking.
        /// </summary>
        public static bool IsOffNow(ZDO zdo) {
            if (Windows.Count == 0) { return false; }

            // Both go missing between worlds, and reading the clock without them would stamp a midnight that
            // then stood for a whole interval.
            ZNet znet = ZNet.instance;
            if (znet == null || EnvMan.instance == null) { return false; }
            double now = znet.GetTimeSeconds();
            // The second test catches the clock moving backwards, which happens on a world change.
            if (now - LastClockRead >= ValConfig.ScheduleCheckInterval.Value || now < LastClockRead) {
                ReadClock(now);
            }

            if (!OffSince.TryGetValue(zdo.GetPrefab(), out long since)) { return false; }
            // Lit by hand at any point since this window opened, so leave it be until the window closes.
            return zdo.GetLong(LitByHandKey, long.MinValue) < since;
        }

        private static void ReadClock(double now) {
            LastClockRead = now;
            float hour = ClockHour(now);

            foreach (KeyValuePair<int, Window> scheduled in Windows) {
                Window w = scheduled.Value;
                // Half open, so the two ends of a full day setting cannot both match.
                bool off = w.From < w.To ? (hour >= w.From && hour < w.To) : (hour >= w.From || hour < w.To);
                if (!off) {
                    OffSince.Remove(scheduled.Key);
                } else if (!OffSince.ContainsKey(scheduled.Key)) {
                    // Worked out rather than stamped with now, so a player who walks up halfway through a window,
                    // or joins the server halfway through one, still dates it from where it actually opened and
                    // agrees with everyone else about which fires somebody lit by hand.
                    OffSince[scheduled.Key] = WindowOpenedAt(now, w.From);
                }
            }
        }

        /// <summary>
        ///  World second at which the most recent run of this off window began, which is the same answer on every
        ///  client because it is derived from the shared clock alone.
        /// </summary>
        private static long WindowOpenedAt(double now, float fromHour) {
            EnvMan env = EnvMan.instance;
            // With the clock pinned by the time console command there is no real boundary to find, so date the
            // window from this moment instead.
            if (env == null || env.m_debugTimeOfDay) { return (long)now; }

            long dayLength = env.m_dayLengthSec > 0 ? env.m_dayLengthSec : 1200L;
            double opened = System.Math.Floor(now / dayLength) * dayLength + UnrescaleDayFraction(fromHour / 24f) * dayLength;
            // A window that opened before midnight opened yesterday.
            if (opened > now) { opened -= dayLength; }
            return (long)opened;
        }

        /// <summary>Inverse of RescaleDayFraction: clock reading back to a share of the real day.</summary>
        private static float UnrescaleDayFraction(float clock) {
            if (clock >= 0.25f && clock <= 0.75f) {
                return 0.15f + (clock - 0.25f) / 0.5f * 0.7f;
            }
            if (clock < 0.25f) {
                return clock / 0.25f * 0.15f;
            }
            return 0.85f + (clock - 0.75f) / 0.25f * 0.15f;
        }

        /// <summary>
        ///  Time of day as an hour from 0 to 24, where 0 is midnight, 6 is sunrise, 12 is noon and 18 is sunset.
        ///
        ///  Recomputed from the world clock rather than read from EnvMan.GetDayFraction(), which returns
        ///  m_smoothDayFraction. That is lerped 1% per FixedUpdate, so it lags for seconds after a world load or a
        ///  sleep skip, and it is client local. The net time is server authoritative and pushed every 2 seconds,
        ///  so every peer agrees.
        ///
        ///  EnvMan.OnMorning and OnEvening look like ready made boundary hooks and are not usable: UpdateTriggers
        ///  returns early unless there is a local player, they are hardcoded to sunrise and sunset, and they edge
        ///  detect on the smoothed fraction.
        /// </summary>
        private static float ClockHour(double now) {
            EnvMan env = EnvMan.instance;
            if (env == null) { return 0f; }

            // The time console command and debug free cam pin the sky to a fixed hour. Following it keeps the
            // torches agreeing with what the player can see, and makes this feature testable.
            if (env.m_debugTimeOfDay) { return Mathf.Clamp01(env.m_debugTime) * 24f; }

            long dayLength = env.m_dayLengthSec > 0 ? env.m_dayLengthSec : 1200L;
            // Modulo in double before narrowing, as EnvMan does. Casting first loses precision on an old world.
            float raw = Mathf.Clamp01((float)(now % dayLength) / dayLength);
            return RescaleDayFraction(raw) * 24f;
        }

        /// <summary>
        ///  Copy of EnvMan.RescaleDayFraction. Night is 30% of the real seconds in a day but half of the clock, so
        ///  the mapping is piecewise: a night hour lasts about 15 world seconds and a day hour about 70.
        /// </summary>
        private static float RescaleDayFraction(float fraction) {
            if (fraction >= 0.15f && fraction <= 0.85f) {
                return 0.25f + (fraction - 0.15f) / 0.7f * 0.5f;
            }
            if (fraction < 0.5f) {
                return fraction / 0.15f * 0.25f;
            }
            return 0.75f + (fraction - 0.85f) / 0.15f * 0.25f;
        }

        /// <summary>Hour this fire off window ends, for the hover text. -1 when the type is not scheduled.</summary>
        public static float OffUntil(ZDO zdo) {
            return Windows.TryGetValue(zdo.GetPrefab(), out Window w) ? w.To : -1f;
        }

        public static string FormatHour(float hour) {
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.RoundToInt((hour - h) * 60f);
            if (m >= 60) { m = 0; h++; }
            return $"{h % 24:00}:{m:00}";
        }

        /// <summary>
        ///  Records that a player lit this fire by hand, so it stays lit for everyone until its window closes.
        ///  Claims the piece first exactly as Fireplace.Interact does, since this runs in place of that call.
        /// </summary>
        public static void LitByHand(ZNetView nview) {
            ZNet znet = ZNet.instance;
            if (znet == null) { return; }
            if (!nview.HasOwner()) { nview.ClaimOwnership(); }
            nview.GetZDO().Set(LitByHandKey, (long)znet.GetTimeSeconds());
        }

        /// <summary>
        ///  Parses Start-End into two hours. Accepts 6, 6.5 and 06:30 on either side. Anything unparseable, out of
        ///  range, or with a start equal to its end is rejected, and the caller treats that as no schedule.
        /// </summary>
        public static bool TryParseWindow(string value, out float from, out float to) {
            from = 0f;
            to = 0f;
            if (string.IsNullOrEmpty(value)) { return false; }

            string[] parts = value.Split('-');
            if (parts.Length != 2) { return false; }
            if (!TryParseHour(parts[0], out from) || !TryParseHour(parts[1], out to)) { return false; }
            // Equally readable as never and always, so refuse to guess.
            return from != to;
        }

        private static bool TryParseHour(string value, out float hour) {
            hour = 0f;
            if (value == null) { return false; }
            value = value.Trim();
            if (value.Length == 0) { return false; }

            if (value.IndexOf(':') >= 0) {
                string[] parts = value.Split(':');
                if (parts.Length != 2) { return false; }
                if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int h)) { return false; }
                if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int m)) { return false; }
                if (h < 0 || h > 24 || m < 0 || m > 59) { return false; }
                hour = h + m / 60f;
            // InvariantCulture, or a client on a comma decimal locale reads 6.5 as 65 or throws.
            } else if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out hour)) {
                return false;
            }

            if (hour < 0f || hour > 24f) { return false; }
            if (hour == 24f) { hour = 0f; }
            return true;
        }
    }
}
