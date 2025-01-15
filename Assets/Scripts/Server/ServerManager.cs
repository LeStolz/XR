#if UNITY_EDITOR
using UnityEditor;
#endif
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
using Newtonsoft.Json;

public class ServerManager : NetworkBehaviour
{
    public readonly int TRIAL_PER_CONDITION_COUNT = 12;
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

        trialsLeft = TRIAL_PER_CONDITION_COUNT;

        NetworkManager.SceneManager.LoadScene("Main", LoadSceneMode.Single);

        if (!setSceneManagerOnLoadComplete)
        {
            setSceneManagerOnLoadComplete = true;

            NetworkManager.SceneManager.OnLoadEventCompleted += async (sceneName, loadSceneMode, _, _) =>
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
                await SpheresManager.Singleton.SpawnSpheres(
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

    public IEnumerator EndCondition(bool notForced = true)
    {
        SaveConditionResult();
        OnConditionEnd?.Invoke();
        if (notForced)
        {
            yield return new WaitForSeconds(5);
        }
        NetworkManager.SceneManager.LoadScene("Connect", LoadSceneMode.Single);
    }

    async void InitTrial()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (trialsLeft == 0)
        {
            ClientManager.Singleton.EndConditionRpc();
            await SpheresManager.Singleton.UpdatePrecalculatedSpheres();
            StartCoroutine(EndCondition());
            return;
        }

        trialsLeft--;
        var actualAnswer = SpheresManager.Singleton.GetRandomSphere();

        currentTrialResult = new(
            GetTimeSinceEpoch(),
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

    static ulong GetTimeSinceEpoch()
    {
        DateTime epochStart = new(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        return (ulong)(DateTime.UtcNow - epochStart).TotalMilliseconds;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void StartTrialRpc()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        currentTrialResult.timestamp = GetTimeSinceEpoch();

        ClientManager.Singleton.StartTrialRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SaveClientTrialTimestampRpc(RpcParams rpcParams = default)
    {
        var spectatorAnswer = new SpectatorAnswer(
            ClientId_SpectatorId.GetValueOrDefault(rpcParams.Receive.SenderClientId, "Test"),
            "center",
            "0;0",
            Vector3.zero,
            -100,
            GetTimeSinceEpoch()
        );

        currentTrialResult.spectatorAnswers.Add(spectatorAnswer);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SaveClientTrialAnswerRpc(
        string seat,
        string answerId,
        Vector3 answerPosition,
        int confidence,
        RpcParams rpcParams = default
    )
    {
        IEnumerator SaveClientTrialAnswer()
        {
            yield return new WaitUntil(() =>
            {
                try
                {
                    currentTrialResult.spectatorAnswers.Find(
                        sa => sa.spectatorId == ClientId_SpectatorId.GetValueOrDefault(rpcParams.Receive.SenderClientId, "Test")
                    );
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            var spectatorAnswerIndex = currentTrialResult.spectatorAnswers.FindIndex(
                sa => sa.spectatorId == ClientId_SpectatorId.GetValueOrDefault(rpcParams.Receive.SenderClientId, "Test")
            );

            currentTrialResult.spectatorAnswers[spectatorAnswerIndex] = new SpectatorAnswer(
                ClientId_SpectatorId.GetValueOrDefault(rpcParams.Receive.SenderClientId, "Test"),
                seat,
                answerId,
                answerPosition,
                confidence,
                currentTrialResult.spectatorAnswers[spectatorAnswerIndex].timestamp
            );

            if (
                currentTrialResult.spectatorAnswers
                    .Where(sa => sa.confidence != -100)
                    .ToList().Count >=
                ClientId_SpectatorId
                    .Where(kvp => kvp.Value != "AR")
                    .ToList().Count
            )
            {
                conditionResult.trialResults.Add(currentTrialResult);
                InitTrial();
            }
        }

        StartCoroutine(SaveClientTrialAnswer());
    }

    void SaveConditionResult()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        string path = StandaloneFileBrowser.SaveFilePanel(
            "Save Condition Result",
            "",
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            "csv"
        );

        File.CreateText(path).Dispose();
        File.WriteAllText(path, Util.ResultToCSV(conditionResult));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ServerManager))]
class ServerManagerEditor : Editor
{
    public override async void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var serverManager = (ServerManager)target;

        if (serverManager == null) return;

        Undo.RecordObject(serverManager, "Server Manager");

        if (GUILayout.Button("Skip Trial"))
        {
            serverManager.StartTrialRpc();
            serverManager.SaveClientTrialTimestampRpc();
            serverManager.SaveClientTrialAnswerRpc(
                "center",
                "0;0",
                Vector3.zero,
                100
            );
        }

        if (GUILayout.Button("Save Precalculated Conditions From Cloud To File"))
        {
            string path = StandaloneFileBrowser.OpenFolderPanel(
                "Save Conditions To Folder",
                "",
                false
            )[0];

            var keys = await CloudSaveManager.Singleton.GetAllKeys();

            foreach (var key in keys)
            {
                var data = await CloudSaveManager.Singleton.Load<List<string>>(key);
                File.WriteAllText($"{path}/{key}.json", JsonConvert.SerializeObject(data));
            }
        }

        if (GUILayout.Button("Save Precalculated Conditions"))
        {
            string path = StandaloneFileBrowser.OpenFilePanel(
                "Open Precalculated Conditions",
                "",
                "json",
                false
            )[0];

            string dataJson = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(dataJson);

            foreach (var key in data)
            {
                await CloudSaveManager.Singleton.Save(key.Key, key.Value);
            }
        }

        if (GUILayout.Button("Generate spheres"))
        {
            var ringCount = 6;
            var targetCount = 8;
            var sphereNames = new List<string>();

            for (int i = 0; i < ringCount; i++)
            {
                for (int j = 0; j < targetCount; j++)
                {
                    sphereNames.Add($"{i};{j}");
                }
            }
            sphereNames = Util.Shuffle(sphereNames);

            var s = JsonConvert.SerializeObject(sphereNames);
            Debug.Log(s);
        }
    }
}
#endif