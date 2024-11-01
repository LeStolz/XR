using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class SocketManager : MonoBehaviour
{
    public static SocketManager Singleton { get; private set; }
    NetworkDriver driver;
    NativeList<NetworkConnection> connections;

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

    public void StartServer()
    {
        var networkSettings = new NetworkSettings();
        driver = driver.IsCreated ? driver : NetworkDriver.Create(networkSettings.WithNetworkConfigParameters(
            disconnectTimeoutMS: 90000000
        ));

        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 9000;

        if (driver.Bind(endpoint) != 0)
            Debug.LogError("Failed to start server");
        else
            driver.Listen();

        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    void Update()
    {
        if (!driver.IsCreated) return;

        driver.ScheduleUpdate().Complete();

        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        NetworkConnection c;
        while ((c = driver.Accept()) != default)
        {
            connections.Add(c);
        }

        for (int i = 0; i < connections.Length; i++)
        {
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[i], out DataStreamReader stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    FixedString128Bytes func = stream.ReadFixedString128();

                    Debug.Log(func);

                    if (func.Equals("RegisterClientRpc AR"))
                    {
                        ServerManager.Singleton.RegisterClientRpc("AR");
                    }
                    else if (func.Equals("UnregisterArClient"))
                    {
                        ServerManager.Singleton.UnregisterArClient();
                    }
                    else if (func.Equals("StartTrialRpc"))
                    {
                        ServerManager.Singleton.StartTrialRpc();
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    ServerManager.Singleton.UnregisterArClient();
                    connections[i] = default;
                }
            }
        }
    }

    public void LoadArConnectScene()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            driver.BeginSend(NetworkPipeline.Null, connections[i], out var writer);
            writer.WriteFixedString128("LoadArConnectScene");
            driver.EndSend(writer);
        }
    }

    public void LoadArMainScene()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            driver.BeginSend(NetworkPipeline.Null, connections[i], out var writer);
            writer.WriteFixedString128("LoadArMainScene");
            driver.EndSend(writer);
        }
    }

    public void InitArTrial(ArCondition arCondition)
    {
        for (int i = 0; i < connections.Length; i++)
        {
            var arConditionJson = JsonUtility.ToJson(arCondition);
            DataStreamWriter writer;

            for (int s = 0; s < arConditionJson.Length; s += 100)
            {
                driver.BeginSend(NetworkPipeline.Null, connections[i], out writer);
                writer.WriteFixedString128(
                    $"StartInitArTrial {arConditionJson.Substring(s, Mathf.Min(100, arConditionJson.Length - s))}"
                );
                driver.EndSend(writer);
            }

            driver.BeginSend(NetworkPipeline.Null, connections[i], out writer);
            writer.WriteFixedString128("EndInitArTrial");
            driver.EndSend(writer);
        }
    }

    void OnDestroy()
    {
        if (driver.IsCreated)
        {
            driver.Dispose();
            connections.Dispose();
        }
    }
}
