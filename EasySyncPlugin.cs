using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EasySync
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("io.github.CSync")]
    public class EasySyncPlugin : BaseUnityPlugin
    {
        private static readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        public static EasySyncPlugin instance;
        private ManualLogSource logSource => Logger;

        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            instance = this;
            _harmony.PatchAll(typeof(Patches));
        }
        
        internal static bool ConditionLog(string message, bool condition = true, LogLevel logLevel = LogLevel.Info)
        {
            if (condition) instance.logSource.Log(logLevel, message);
            return condition;
        }
    }
}
