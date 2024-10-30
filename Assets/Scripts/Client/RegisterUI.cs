using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class RegisterUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField serverIpTextField;
    [SerializeField] private TMP_InputField idTextField;
    [SerializeField] private TMP_Dropdown seatDropdown;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;

    IEnumerator RegisterClient()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsConnectedClient);

        GameManager.Singleton.playerId = idTextField.text;
        GameManager.Singleton.playerSeat = seatDropdown.captionText.text;
        GameManager.Singleton.serverIp = serverIpTextField.text;
        ServerManager.Singleton.RegisterClientRpc(GameManager.Singleton.playerId);
        readyButtonText.text = "Syncing...";
    }

    void Ready()
    {
        if (idTextField.text == "") return;

        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            ServerConnectionManager.Singleton.TryPingServer(
                serverIpTextField.text == "" ? "127.0.0.1" : serverIpTextField.text
            );
        }
        else
        {
            StartCoroutine(RegisterClient());
        }
    }

    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            gameObject.SetActive(false);
            return;
        }

        idTextField.text = GameManager.Singleton.playerId;
        seatDropdown.value = seatDropdown.options.FindIndex(
            option => option.text == GameManager.Singleton.playerSeat
        );
        serverIpTextField.text = GameManager.Singleton.serverIp;

        if (NetworkManager.Singleton.IsConnectedClient)
        {
            Ready();
        }

        readyButton.onClick.AddListener(Ready);
        ServerConnectionManager.Singleton.OnConnected += () =>
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                serverIpTextField.text == "" ? "127.0.0.1" : serverIpTextField.text,
                7777
            );
            NetworkManager.Singleton.StartClient();

            StartCoroutine(RegisterClient());
        };
    }
}
