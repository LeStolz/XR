using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArUI : MonoBehaviour
{
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI submitButtonText;
    [SerializeField] private TextMeshProUGUI statusText;

    void Start()
    {
        submitButton.onClick.AddListener(() =>
        {
            Disable();
            ServerManager.Singleton.StartTrialRpc();
        });
    }

    public void SetStatus(string status)
    {
        statusText.text = status;
    }

    public void Enable()
    {
        submitButtonText.text = "Submit";
        submitButton.interactable = true;
    }

    public void Disable()
    {
        submitButtonText.text = "Syncing...";
        submitButton.interactable = false;
    }
}
