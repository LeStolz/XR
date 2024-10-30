using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button spectatorButton;
    [SerializeField] private GameObject registerUI;
    [SerializeField] private GameObject serverManagerUI;

    void Start()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            InitClient(null, false);
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            InitServer(false);
            return;
        }

        serverButton.onClick.AddListener(() => InitServer());
        spectatorButton.onClick.AddListener(() => InitClient(""));
    }

    void InitServer(bool start = true)
    {
        if (start)
        {
            NetworkManager.Singleton.StartServer();
            ServerManager.Singleton.StartServer();
        }

        gameObject.SetActive(false);
        serverManagerUI.SetActive(true);
        GameManager.Singleton.playerId = "Server";
    }

    void InitClient(string id, bool start = true)
    {
        gameObject.SetActive(false);
        registerUI.SetActive(true);

        if (start)
        {
            GameManager.Singleton.playerId = id;
            GameManager.Singleton.playerSeat = "";
        }
    }
}
