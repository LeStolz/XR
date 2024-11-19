using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections;
using SFB;

public class ServerManager : NetworkBehaviour
{
    static public ServerManager Singleton { get; private set; }

    public Dictionary<ulong, string> ClientId_SpectatorId
    {
        get;
        private set;
    } = new Dictionary<ulong, string>();
    public event Action OnClientConnected;
    public event Action OnClientDisconnected;
    public event Action OnConditionEnd;
    public event Action<(int RingCount, int TargetCount)> OnConditionStart;
    public event Action<ArCondition> OnTrialInit;

    ConditionResult conditionResult;
    TrialResult currentTrialResult;
    int trialsLeft = 0;
    bool setSceneManagerOnLoadComplete = false;

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
        NetworkManager.Singleton.StartServer();

        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += UnregisterClient;
        SocketManager.Singleton.StartServer();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RegisterClientRpc(FixedString32Bytes spectatorIdFixedString, RpcParams rpcParams = default)
    {
        string spectatorId = spectatorIdFixedString.ToString();
        ulong clientId = rpcParams.Receive.SenderClientId;

        ClientId_SpectatorId[clientId] = spectatorId;
        OnClientConnected?.Invoke();
    }

    void UnregisterClient(ulong clientId)
    {
        if (ClientId_SpectatorId.ContainsKey(clientId))
        {
            ClientId_SpectatorId.Remove(clientId);
            OnClientDisconnected?.Invoke();
        }
    }

    public void UnregisterArClient()
    {
        var client = ClientId_SpectatorId.FirstOrDefault(kvp => kvp.Value == "AR");

        if (client.Value == "AR")
        {
            UnregisterClient(client.Key);
        }
    }

    public void StartCondition(Condition condition, string settingsPath)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        conditionResult = new()
        {
            condition = condition,
            trialResults = new()
        };

        trialsLeft = condition.ringCount * condition.targetCount * 2;

        NetworkManager.SceneManager.LoadScene("Main", LoadSceneMode.Single);

        if (!setSceneManagerOnLoadComplete)
        {
            setSceneManagerOnLoadComplete = true;

            NetworkManager.SceneManager.OnLoadEventCompleted += (sceneName, loadSceneMode, _, _) =>
            {
                if (
                    sceneName != "Main" ||
                    conditionResult.condition.ringCount == 0 ||
                    conditionResult.condition.targetCount == 0
                ) return;

                SetSettingsFile(settingsPath);

                OnConditionStart?.Invoke(
                    (conditionResult.condition.ringCount, conditionResult.condition.targetCount)
                );
                SpheresManager.Singleton.SpawnSpheres(
                    (conditionResult.condition.ringCount, conditionResult.condition.targetCount)
                );

                InitTrial();
            };
        }
    }

    void SetSettingsFile(string settingsPath)
    {
        if (settingsPath == "") return;

        string settingsData = File.ReadAllText(settingsPath);

        SpheresManager.Singleton.layoutDimensionsServer = JsonUtility.FromJson<LayoutDimensions>(settingsData);
    }

    public IEnumerator EndCondition(bool wait = true)
    {
        SaveConditionResult();
        OnConditionEnd?.Invoke();
        if (wait) yield return new WaitForSeconds(5);
        NetworkManager.SceneManager.LoadScene("Connect", LoadSceneMode.Single);
    }

    void InitTrial()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (trialsLeft == 0)
        {
            ClientManager.Singleton.EndConditionRpc();
            StartCoroutine(EndCondition());
            return;
        }

        trialsLeft--;
        var actualAnswer = SpheresManager.Singleton.GetRandomSphere();

        currentTrialResult = new(
            Util.GetTimeSinceEpoch(),
            actualAnswer.Item1,
            actualAnswer.Item2
        );

        ClientManager.Singleton.InitTrialRpc();
        OnTrialInit?.Invoke(new(
            conditionResult.condition.pos,
            conditionResult.condition.pov,
            conditionResult.condition.assisted,
            int.Parse(actualAnswer.Item1.Split(";")[0]),
            int.Parse(actualAnswer.Item1.Split(";")[1]),
            conditionResult.condition.ringCount,
            conditionResult.condition.targetCount,
            SpheresManager.Singleton.layoutDimensionsServer.conditionalDimensions.Find(
                cd =>
                    cd.ringCount == conditionResult.condition.ringCount &&
                    cd.targetCount == conditionResult.condition.targetCount
            ).scale,
            SpheresManager.Singleton.GetLowestRingOrigin(),
            SpheresManager.Singleton.layoutDimensionsServer.radius,
            SpheresManager.Singleton.layoutDimensionsServer.conditionalDimensions.Find(
                cd =>
                    cd.ringCount == conditionResult.condition.ringCount &&
                    cd.targetCount == conditionResult.condition.targetCount
            ).gap
        ));
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void StartTrialRpc()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        currentTrialResult.timestamp = Util.GetTimeSinceEpoch();

        ClientManager.Singleton.StartTrialRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SaveClientTrialAnswerRpc(
        string seat,
        string answerId,
        Vector3 answerPosition,
        int confidence,
        ulong timestamp,
        RpcParams rpcParams = default
    )
    {
        if (currentTrialResult.timestamp == 0) return;

        var spectatorAnswer = new SpectatorAnswer(
            ClientId_SpectatorId[rpcParams.Receive.SenderClientId],
            seat,
            answerId,
            answerPosition,
            confidence,
            timestamp
        );

        currentTrialResult.spectatorAnswers.Add(spectatorAnswer);

        if (currentTrialResult.spectatorAnswers.Count == ClientId_SpectatorId.Where(
            kvp => kvp.Value != "AR"
        ).ToList().Count)
        {
            conditionResult.trialResults.Add(currentTrialResult);
            InitTrial();
        }
    }

    void SaveConditionResult()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        string data = JsonUtility.ToJson(conditionResult);
        string path = StandaloneFileBrowser.SaveFilePanel(
            "Save Condition Result",
            "",
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            "json"
        );

        File.CreateText(path).Dispose();
        File.WriteAllText(path, data);
    }
}
