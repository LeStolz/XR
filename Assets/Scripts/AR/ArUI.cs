using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArUI : MonoBehaviour
{
    [SerializeField] Button submitButton;
    [SerializeField] TextMeshProUGUI submitButtonText;
    [SerializeField] TextMeshProUGUI statusText;

    void Start()
    {
        submitButton.onClick.AddListener(() =>
        {
            Disable();
            ArSocketManager.Singleton.StartTrialRpc();
        });
    }

    public void Enable(string target)
    {
        statusText.text = target;
        submitButtonText.text = "Submit";
        submitButton.interactable = true;
    }

    public void Disable()
    {
        statusText.text = "Waiting for spectators to answer...";
        submitButtonText.text = "Syncing...";
        submitButton.interactable = false;
    }
}
