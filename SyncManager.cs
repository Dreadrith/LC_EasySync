using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using static EasySync.EasySyncPlugin;
using LogLevel = BepInEx.Logging.LogLevel;

namespace EasySync;

public static class SyncManager
{
	private static readonly Dictionary<string, SyncedInstanceContainer> instancesToSync = new Dictionary<string, SyncedInstanceContainer>();

	public static SyncedInstanceContainer? RegisterForSyncing(object instance, string GUID)
	{
		if (ConditionLog("Cannot register a null instance for syncing!", instance == null, LogLevel.Error)) return null;
		var container = new SyncedInstanceContainer(instance, GUID);
		if (ConditionLog($"Could not register instance ({instance}) of type ({instance.GetType().Name}) for syncing! Does it inherit from SyncedInstance from CSync?", !container.isValid, LogLevel.Error)) return null;
		
		if (instancesToSync.TryGetValue(GUID, out _))
		{
			ConditionLog($"An instance with GUID {GUID} was already registered!", true, LogLevel.Warning);
			return null;
		}
		
		container.InitInstance();
		instancesToSync.Add(GUID, container);
		return container;
	}
	
	public static void UnregisterFromSyncing(string GUID)
	{
		if (instancesToSync.ContainsKey(GUID))
			instancesToSync.Remove(GUID);
	}

	internal static void SyncAllInstances()
	{
		foreach(var i in instancesToSync.Values)
			i.BeginSync();
	}

	internal static void RevertSyncAllInstances()
	{
		foreach(var i in instancesToSync.Values)
			i.RevertSync();
	}

	public readonly struct SyncedInstanceContainer
	{
		private readonly object instance;
		private readonly string GUID;
		private readonly MethodInfo initInstanceMethod;
		private readonly MethodInfo revertSyncMethod;
		private readonly MethodInfo serializeToBytesMethod;
		private readonly MethodInfo syncInstanceMethod;
		private readonly PropertyInfo instanceProperty;
		private readonly PropertyInfo defaultProperty;
		private readonly PropertyInfo intSizeProperty;
		private readonly PropertyInfo messageManagerProperty;
		private readonly PropertyInfo isClientProperty;
		private readonly PropertyInfo isHostProperty;
		private readonly FieldInfo syncedField;
		internal readonly bool isValid;
		
		#region Properties
		public object Instance => instanceProperty.GetValue(null);
		public object Default => defaultProperty.GetValue(null);
		public int IntSize => (int)intSizeProperty.GetValue(null);
		public CustomMessagingManager MessageManager => (CustomMessagingManager)messageManagerProperty.GetValue(null);
		public bool IsClient => (bool)isClientProperty.GetValue(null);
		public bool IsHost => (bool)isHostProperty.GetValue(null);
		internal bool Synced
		{
			get => (bool)syncedField.GetValue(null);
			set => syncedField.SetValue(null, value);
		}
		#endregion
		internal SyncedInstanceContainer(object instance, string GUID)
		{
			this.instance = instance;
			var type = instance.GetType().BaseType;
			this.GUID = GUID;
			var bindingFlags = BindingFlags.Static | BindingFlags.Public;

			int i = 0;
			void ScreamIfNull(object obj)
			{
				i++;
				ConditionLog($"VARIABLE AT INDEX {i} IS NULL!!!", obj == null, LogLevel.Error);
			}
			
			instanceProperty = type.GetProperty("Instance", bindingFlags);
			defaultProperty = type.GetProperty("Default", bindingFlags);
			initInstanceMethod = type.GetMethod("InitInstance", BindingFlags.Instance | BindingFlags.Public);
			revertSyncMethod = type.GetMethod("RevertSync", bindingFlags);
			serializeToBytesMethod = type.BaseType.GetMethod("SerializeToBytes", bindingFlags);
			syncInstanceMethod = type.GetMethod("SyncInstance", bindingFlags);
			intSizeProperty = type.BaseType.GetProperty("IntSize", bindingFlags);
			messageManagerProperty = type.GetProperty("MessageManager", bindingFlags);
			isClientProperty = type.GetProperty("IsClient", bindingFlags);
			isHostProperty = type.GetProperty("IsHost", bindingFlags);
			syncedField = type.GetField("Synced", bindingFlags);
			
			ScreamIfNull(instanceProperty);
			ScreamIfNull(defaultProperty);
			ScreamIfNull(initInstanceMethod);
			ScreamIfNull(revertSyncMethod);
			ScreamIfNull(serializeToBytesMethod);
			ScreamIfNull(syncInstanceMethod);
			ScreamIfNull(intSizeProperty);
			ScreamIfNull(messageManagerProperty);
			ScreamIfNull(isClientProperty);
			ScreamIfNull(isHostProperty);
			ScreamIfNull(syncedField);
			
			isValid = instanceProperty != null &&
			          defaultProperty != null &&
			          initInstanceMethod != null &&
			          revertSyncMethod != null && 
			          intSizeProperty != null &&
			          messageManagerProperty != null &&
			          isClientProperty != null &&
			          isHostProperty != null &&
			          syncedField != null;
		}

		internal void InitInstance() => initInstanceMethod.Invoke(instance, new [] { instance });
		public void RevertSync() => revertSyncMethod.Invoke(null, null);
		private byte[] SerializeToBytes(object inst) => (byte[])serializeToBytesMethod.Invoke(null, new [] { inst });
		private void SyncInstance(byte[] data) => syncInstanceMethod.Invoke(null, new object[] { data });
		
		internal void BeginSync()
		{
			if (IsHost) {
				MessageManager.RegisterNamedMessageHandler($"{GUID}_OnRequestConfigSync", OnRequestSync);
				Synced = true;
				return;
			}
			Synced = false;
			MessageManager.RegisterNamedMessageHandler($"{GUID}_OnReceiveConfigSync", OnReceiveSync);
			RequestSync();
		}

		public void RequestSync()
		{
			if (!IsClient) return;
			using FastBufferWriter stream = new(IntSize, Allocator.Temp);
			SendMessage(stream, $"{GUID}_OnRequestConfigSync");
		}

		private void OnRequestSync(ulong clientId, FastBufferReader _) 
		{
			if (!IsHost) return;

			byte[] array = SerializeToBytes(Instance);
			int value = array.Length;

			using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

			try {
				stream.WriteValueSafe(in value);
				stream.WriteBytesSafe(array);

				SendMessage(stream, $"{GUID}_OnReceiveConfigSync", clientId);
			} catch(Exception e)
			{
				ConditionLog($"Error occurred syncing config with client: {clientId}\n{e}", true, LogLevel.Error);
			}
		}

		private void OnReceiveSync(ulong _, FastBufferReader reader) 
		{
			if (!reader.TryBeginRead(IntSize)) {
				ConditionLog("Config sync error: Could not begin reading buffer.", true, LogLevel.Error);
				return;
			}

			reader.ReadValueSafe(out int val);
			if (!reader.TryBeginRead(val)) {
				ConditionLog("Config sync error: Host could not sync.", true, LogLevel.Error);
				return;
			}

			byte[] data = new byte[val];
			reader.ReadBytesSafe(ref data, val);

			try {
				SyncInstance(data);
			} catch(Exception e) {
				ConditionLog($"Error syncing config instance!\n{e}", true, LogLevel.Error);
			}
		}
		
	}
	
	//Code from CSync.Util.Extensions by Owen3H
	private static void SendMessage(FastBufferWriter stream, string label, ulong clientId = 0)
	{
		int num = stream.Capacity > 1300 ? 1 : 0;
		NetworkDelivery networkDelivery = num != 0 ? NetworkDelivery.ReliableFragmentedSequenced : NetworkDelivery.Reliable;
		NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(label, clientId, stream, networkDelivery);
	}
}