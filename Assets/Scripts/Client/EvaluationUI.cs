using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EvaluationUI : MonoBehaviour
{
    [SerializeField] Button submitButton;
    [SerializeField] TextMeshProUGUI idText;

    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        idText.text = $"ID: {GameManager.Singleton.playerId} - Seat: {GameManager.Singleton.playerSeat}";

        submitButton.onClick.AddListener(() =>
        {
            ClientManager.Singleton.StartConfidenceSelect();
        });

        ClientManager.Singleton.OnTrialInit += OnTrialInit;
        ClientManager.Singleton.OnConditionEnd += OnConditionEnd;
        ClientManager.Singleton.OnConfidenceSelect += OnConfidenceSelect;
    }

    void OnTrialInit()
    {
        gameObject.SetActive(true);
    }

    void OnConditionEnd()
    {
        gameObject.SetActive(false);
    }

    void OnConfidenceSelect()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        ClientManager.Singleton.OnTrialInit -= OnTrialInit;
        ClientManager.Singleton.OnConditionEnd -= OnConditionEnd;
        ClientManager.Singleton.OnConfidenceSelect -= OnConfidenceSelect;
    }
}
