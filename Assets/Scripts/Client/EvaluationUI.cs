using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvaluationUI : MonoBehaviour
{
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI idText;
    public ulong Timestamp { get; private set; } = 0;

    void Start()
    {
        idText.text = $"ID: {GameManager.Singleton.playerId} - Seat: {GameManager.Singleton.playerSeat}";

        submitButton.onClick.AddListener(() =>
        {
            Timestamp = Util.GetTimeSinceEpoch();
            ClientManager.Singleton.ToggleConfidenceScene();
        });
    }
}
