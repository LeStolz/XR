using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ServerEvaluationUI : MonoBehaviour
{
    [SerializeField] Button settingsButton;
    [SerializeField] Button configButton;
    [SerializeField] Button closeButton;
    [SerializeField] GameObject settings;
    [SerializeField] GameObject pos;
    [SerializeField] GameObject pov;
    [SerializeField] GameObject layout;
    [SerializeField] TextMeshProUGUI posText;
    [SerializeField] TextMeshProUGUI povText;
    [SerializeField] TextMeshProUGUI layoutText;

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
            StartCoroutine(ServerManager.Singleton.EndCondition(false));
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
