using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ServerEvaluationUI : MonoBehaviour
{
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button configButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject pos;
    [SerializeField] private GameObject pov;
    [SerializeField] private GameObject layout;
    [SerializeField] private TextMeshProUGUI posText;
    [SerializeField] private TextMeshProUGUI povText;
    [SerializeField] private TextMeshProUGUI layoutText;

    void Start()
    {
        gameObject.SetActive(NetworkManager.Singleton.IsServer);

        settings.SetActive(false);
        settingsButton.onClick.AddListener(() =>
        {
            settings.SetActive(true);
        });

        closeButton.onClick.AddListener(() =>
        {
            settings.SetActive(false);
        });

        configButton.onClick.AddListener(() =>
        {
            SocketManager.Singleton.LoadArConnectScene();
            NetworkManager.Singleton.SceneManager.LoadScene("Connect", LoadSceneMode.Single);
        });

        var posThumb = GameManager.Singleton.posThumbs[GameManager.Singleton.posId];
        var povThumb = GameManager.Singleton.povThumbs[GameManager.Singleton.povId];
        var layoutThumb = GameManager.Singleton.layoutThumbs[GameManager.Singleton.layoutId];

        posText.text = posThumb.name;
        povText.text = povThumb.name;
        layoutText.text = $"{layoutThumb.ringCount} rings x {layoutThumb.targetCount} targets";

        pos.GetComponent<Image>().sprite = posThumb.sprite;
        pov.GetComponent<Image>().sprite = povThumb.sprite;
        layout.GetComponent<Image>().sprite = layoutThumb.sprite;
    }
}
