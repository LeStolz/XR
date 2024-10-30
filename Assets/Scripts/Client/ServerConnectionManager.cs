using UnityEngine;
using Unity.Networking.Transport;
using System;
using Unity.Netcode;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;

public class ServerConnectionManager : MonoBehaviour
{
	static public ServerConnectionManager Singleton { get; private set; }
	public bool Connected { get; private set; } = false;
	public event Action OnConnected;

	NetworkDriver driver;
	NetworkConnection connection;

	void Awake()
	{
		if (Singleton != null)
		{
			Destroy(gameObject);
			return;
		}

		Singleton = this;
		DontDestroyOnLoad(gameObject);
	}

	public void TryPingServer(string serverIp)
	{
		if (Connected) return;

		driver = driver.IsCreated ? driver : NetworkDriver.Create();
		connection = default;

		var endpoint = NetworkEndPoint.Parse(serverIp, 7777);
		connection = driver.Connect(endpoint);
	}

	void OnDestroy()
	{
		if (Connected)
		{
			driver.ScheduleUpdate().Complete();
			connection.Disconnect(driver);
			driver.ScheduleUpdate().Complete();
		}
		driver.Dispose();
	}

	void Update()
	{
		if (NetworkManager.Singleton.IsServer) return;
		if (!driver.IsCreated || !connection.IsCreated) return;
		if (
			driver.GetConnectionState(connection) == NetworkConnection.State.Connected &&
			Connected == false
		)
		{
			Connected = true;
			OnConnected?.Invoke();
		}
		driver.ScheduleUpdate().Complete();

		NetworkEvent.Type cmd;

		while (
			(cmd = connection.PopEvent(driver, out DataStreamReader _)) != NetworkEvent.Type.Empty
		)
		{
			if (cmd == NetworkEvent.Type.Connect)
			{
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				connection = default;
				Connected = false;
			}
		}
	}
}
