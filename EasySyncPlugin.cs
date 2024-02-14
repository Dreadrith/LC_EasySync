using BepInEx;
using BepInEx.Logging;

namespace EasySync
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(YetAnotherLethalLibrary.PluginInfo.PLUGIN_GUID)]
    [BepInDependency("io.github.CSync")]
    public class EasySyncPlugin : BaseUnityPlugin
    {
        public static EasySyncPlugin instance;
        private ManualLogSource logSource => Logger;

        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            instance = this;
            SyncManager.Initialize();
        }
        
        internal static bool ConditionLog(string message, bool condition = true, LogLevel logLevel = LogLevel.Info)
        {
            if (condition) instance.logSource.Log(logLevel, message);
            return condition;
        }
    }
}
