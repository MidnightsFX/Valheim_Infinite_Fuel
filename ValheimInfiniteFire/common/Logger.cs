using BepInEx.Logging;
using System;


namespace ValheimInfiniteFire.common
{
    internal static class Logger
    {
        public static LogLevel Level = LogLevel.Info;

        public static void enableDebugLogging(object sender, EventArgs e)
        {
            if (ValConfig.EnableDebugMode.Value)
            {
                Level = LogLevel.Debug;
            }
            else
            {
                Level = LogLevel.Info;
            }
            // set log level
        }

        public static void CheckEnableDebugLogging()
        {
            if (ValConfig.EnableDebugMode.Value)
            {
                Level = LogLevel.Debug;
            }
            else
            {
                Level = LogLevel.Info;
            }
        }

        public static void LogDebug(string message)
        {
            if (Level >= LogLevel.Debug)
            {
                ValheimInfiniteFire.Log.LogInfo(message);
            }
        }
        public static void LogInfo(string message)
        {
            if (Level >= LogLevel.Info)
            {
                ValheimInfiniteFire.Log.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (Level >= LogLevel.Warning)
            {
                ValheimInfiniteFire.Log.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (Level >= LogLevel.Error)
            {
                ValheimInfiniteFire.Log.LogError(message);
            }
        }
    }
}
