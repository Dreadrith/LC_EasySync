using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EasySync
{

    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    [BepInDependency("io.github.CSync")]
    public class EasySyncPlugin : BaseUnityPlugin
    {
        public const string MOD_ID = "EasySync";
        public const string MOD_NAME = "EasySync";
        public const string MOD_VERSION = "0.0.2";
        
        private static readonly Harmony _harmony = new Harmony(MOD_ID);
        public static EasySyncPlugin instance;
        private ManualLogSource logSource => Logger;

        private void Awake()
        {
            Logger.LogInfo($"Plugin {MOD_ID} is loaded!");
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
