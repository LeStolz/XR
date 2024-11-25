using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class SocketManager : MonoBehaviour
{
    public static SocketManager Singleton { get; private set; }
    NetworkDriver driver;
    NetworkPipeline pipeline;
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
            disconnectTimeoutMS: 60 * 1000
        ));
        pipeline = driver.CreatePipeline(
           typeof(FragmentationPipelineStage),
           typeof(ReliableSequencedPipelineStage)
        );

        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 9000;

        driver.Bind(endpoint);
        driver.Listen();

        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        ServerManager.Singleton.OnConditionEnd += LoadArConnectScene;
        ServerManager.Singleton.OnConditionStart += LoadArMainScene;
        ServerManager.Singleton.OnTrialInit += InitArTrial;

        Debug.developerConsoleVisible = true;
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

    void LoadArConnectScene()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            driver.BeginSend(pipeline, connections[i], out var writer);
            writer.WriteFixedString128("LoadArConnectScene");
            driver.EndSend(writer);
        }
    }

    void LoadArMainScene((int RingCount, int TargetCount) _)
    {
        for (int i = 0; i < connections.Length; i++)
        {
            driver.BeginSend(pipeline, connections[i], out var writer);
            writer.WriteFixedString128("LoadArMainScene");
            driver.EndSend(writer);
        }
    }

    void InitArTrial(ArCondition arCondition)
    {
        for (int i = 0; i < connections.Length; i++)
        {
            var arConditionJson = JsonUtility.ToJson(arCondition);
            DataStreamWriter writer;

            for (int s = 0; s < arConditionJson.Length; s += 100)
            {
                driver.BeginSend(pipeline, connections[i], out writer);
                writer.WriteFixedString128(
                    $"StartInitArTrial {arConditionJson.Substring(s, Mathf.Min(100, arConditionJson.Length - s))}"
                );
                driver.EndSend(writer);
            }

            driver.BeginSend(pipeline, connections[i], out writer);
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

        ServerManager.Singleton.OnConditionEnd -= LoadArConnectScene;
        ServerManager.Singleton.OnConditionStart -= LoadArMainScene;
        ServerManager.Singleton.OnTrialInit -= InitArTrial;
    }
}
