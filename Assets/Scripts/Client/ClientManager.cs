using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ClientManager : NetworkBehaviour
{
    public static ClientManager Singleton { get; private set; }

    [SerializeField] private GameObject EvaluationScene;
    [SerializeField] private GameObject EvaluationRotator;
    [SerializeField] private GameObject EvaluationUI;
    [SerializeField] private GameObject EvaluationSyncingUI;
    [SerializeField] private GameObject EvaluationWellDoneUI;
    [SerializeField] private GameObject ConfidenceUI;
    [SerializeField] private GameObject ArUI;

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            EvaluationUI.SetActive(false);
            EvaluationSyncingUI.SetActive(false);
            ConfidenceUI.SetActive(false);
            EvaluationScene.SetActive(true);
            return;
        }

        if (GameManager.Singleton.playerId != "AR")
        {
            EvaluationUI.SetActive(true);
            EvaluationSyncingUI.SetActive(true);
            ConfidenceUI.SetActive(false);
            EvaluationScene.SetActive(true);
        }
        else
        {
            ArUI.SetActive(true);
            EvaluationScene.SetActive(false);
        }
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void InitTrialRpc(FixedString32Bytes target)
    {
        if (GameManager.Singleton.playerId == "AR")
        {
            ArUI.GetComponent<ArUI>().SetStatus($"Target is {target}");
            ArUI.GetComponent<ArUI>().Enable();
            return;
        }

        EvaluationScene.SetActive(true);
        EvaluationRotator.GetComponent<Rotator>().Reset();
        SpheresManager.Singleton.ResetSpheres();

        EvaluationSyncingUI.SetActive(true);
        EvaluationUI.SetActive(true);
        ConfidenceUI.SetActive(false);
        ConfidenceUI.GetComponent<ConfidenceUI>().Reset();
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void StartTrialRpc()
    {
        if (GameManager.Singleton.playerId == "AR")
        {
            ArUI.GetComponent<ArUI>().SetStatus("Waiting for spectators to answer...");
            ArUI.GetComponent<ArUI>().Disable();
            return;
        }

        EvaluationSyncingUI.SetActive(false);
    }

    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void EndConditionRpc()
    {
        if (GameManager.Singleton.playerId == "AR")
        {
            ArUI.GetComponent<ArUI>().Disable();
            return;
        }

        EvaluationWellDoneUI.SetActive(true);
        EvaluationUI.SetActive(false);
        EvaluationSyncingUI.SetActive(false);
        ConfidenceUI.SetActive(false);
        ArUI.SetActive(false);
    }

    public void ToggleConfidenceScene()
    {
        if (NetworkManager.Singleton.IsServer) return;
        if (GameManager.Singleton.playerId == "AR") return;
        if (SpheresManager.Singleton.GetSelectedSphere() == null) return;

        EvaluationScene.SetActive(false);
        EvaluationUI.SetActive(false);
        EvaluationSyncingUI.SetActive(false);
        ConfidenceUI.SetActive(true);
    }

    public void SubmitAnswer()
    {
        ServerManager.Singleton.SaveClientTrialAnswerRpc(
            GameManager.Singleton.playerSeat,
            SpheresManager.Singleton.GetSelectedSphere().Item1,
            SpheresManager.Singleton.GetSelectedSphere().Item2,
            ConfidenceUI.GetComponent<ConfidenceUI>().SelectedButtonIndex,
            EvaluationUI.GetComponent<EvaluationUI>().Timestamp
        );
    }
}
