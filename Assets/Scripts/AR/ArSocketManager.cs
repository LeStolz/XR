using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;
using System;

public class ArSocketManager : MonoBehaviour
{
    [SerializeField] GameObject ArUI;

    static public ArSocketManager Singleton { get; private set; }

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
    }

    void Start()
    {
        driver = driver.IsCreated ? driver : NetworkDriver.Create();
        connection = default;

        var endpoint = NetworkEndPoint.Parse("127.0.0.1", 9000);
        connection = driver.Connect(endpoint);
    }

    void OnDestroy()
    {
        driver.ScheduleUpdate().Complete();
        connection.Disconnect(driver);
        driver.ScheduleUpdate().Complete();
        driver.Dispose();
    }

    string arConditionJson = "";

    void Update()
    {
        if (!driver.IsCreated || !connection.IsCreated) return;
        driver.ScheduleUpdate().Complete();

        NetworkEvent.Type cmd;

        while (
            (cmd = connection.PopEvent(driver, out DataStreamReader stream)) != NetworkEvent.Type.Empty
        )
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                driver.BeginSend(connection, out var writer);
                writer.WriteFixedString128("RegisterClientRpc AR");
                driver.EndSend(writer);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                FixedString128Bytes func = stream.ReadFixedString128();

                if (func.Equals("LoadArConnectScene"))
                {
                    LoadArConnectScene();
                }
                else if (func.Equals("LoadArMainScene"))
                {
                    LoadArMainScene();
                }
                else if (func.ToString().StartsWith("StartInitArTrial"))
                {
                    arConditionJson += func.ToString().Remove(0, "StartInitArTrial ".Length);
                }
                else if (func.Equals("EndInitArTrial"))
                {
                    var arCondition = JsonUtility.FromJson<ArCondition>(arConditionJson);
                    InitArTrial(arCondition);
                    arConditionJson = "";
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                connection = default;
            }
        }
    }

    void LoadArConnectScene()
    {
        ArUI.SetActive(false);
    }

    void LoadArMainScene()
    {
        ArUI.SetActive(true);
    }

    void InitArTrial(ArCondition arCondition)
    {
        ArUI.GetComponent<ArUI>().Enable(
            JsonUtility.ToJson(arCondition)
        );
    }

    public void StartTrialRpc()
    {
        driver.BeginSend(connection, out var writer);
        writer.WriteFixedString128("StartTrialRpc");
        driver.EndSend(writer);

        ArUI.GetComponent<ArUI>().Disable();
    }
}
