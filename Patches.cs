using GameNetcodeStuff;
using HarmonyLib;

namespace EasySync;

public class Patches
{
	[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
	[HarmonyPostfix]
	private static void PlayerControllerBConnectClientToPlayerObjectPostFix(PlayerControllerB __instance)
	{
		SyncManager.SyncAllInstances();
	}
	
	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
	private static void GameNetworkManagerStartDisconnectPostFix() 
	{
		SyncManager.RevertSyncAllInstances();
	}
}