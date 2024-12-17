using System;
using Unity.Netcode;
using UnityEngine;

public class ClientManager : NetworkBehaviour
{
    public static ClientManager Singleton { get; private set; }

    [SerializeField] GameObject EvaluationScene;
    [SerializeField] GameObject EvaluationSyncingUI;
    [SerializeField] GameObject EvaluationWellDoneUI;

    public event Action OnTrialInit;
    public event Action OnConditionEnd;
    public event Action OnConfidenceSelect;

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
        EvaluationWellDoneUI.SetActive(false);
        EvaluationScene.SetActive(true);
        EvaluationSyncingUI.SetActive(!NetworkManager.Singleton.IsServer);
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void InitTrialRpc()
    {
        EvaluationScene.SetActive(true);
        EvaluationSyncingUI.SetActive(true);
        OnTrialInit?.Invoke();
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void StartTrialRpc()
    {
        EvaluationSyncingUI.SetActive(false);
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void EndConditionRpc()
    {
        EvaluationWellDoneUI.SetActive(true);
        EvaluationSyncingUI.SetActive(false);
        OnConditionEnd?.Invoke();
    }

    public void StartConfidenceSelect()
    {
        if (NetworkManager.Singleton.IsServer) return;
        if (SpheresManager.Singleton.GetSelectedSphere() == null) return;

        ServerManager.Singleton.SaveClientTrialTimestampRpc();

        EvaluationScene.SetActive(false);
        EvaluationSyncingUI.SetActive(false);
        OnConfidenceSelect?.Invoke();
    }

    public void SubmitAnswer(int selectedButtonIndex)
    {
        ServerManager.Singleton.SaveClientTrialAnswerRpc(
            GameManager.Singleton.playerSeat,
            SpheresManager.Singleton.GetSelectedSphere().Item1,
            SpheresManager.Singleton.GetSelectedSphere().Item2,
            selectedButtonIndex
        );
    }
}
