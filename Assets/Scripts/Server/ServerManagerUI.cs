using System;
using System.Collections.Generic;
using System.Linq;
using SFB;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

[Serializable]
struct Layout
{
    public int layoutId;
    public Toggle toggle;
}

public class ServerManagerUI : MonoBehaviour
{
    [SerializeField] GameObject conditionScene;
    [SerializeField] GameObject layoutScene;
    [SerializeField] HorizontalScrollSnap posScroller;
    [SerializeField] HorizontalScrollSnap povScroller;
    [SerializeField] List<Layout> layouts;
    [SerializeField] Button nextButton;
    [SerializeField] Button pickSettingsFileButton;
    [SerializeField] TextMeshProUGUI pickSettingsFileButtonLabel;
    [SerializeField] Button restartButton;
    [SerializeField] TextMeshProUGUI connectedClientsLabel;

    void Awake()
    {
        layouts.Find(l => l.layoutId == GameManager.Singleton.layoutId).toggle.isOn = true;
        posScroller.StartingScreen = GameManager.Singleton.posId;
        povScroller.StartingScreen = GameManager.Singleton.povId;
        pickSettingsFileButtonLabel.text = GameManager.Singleton.settingsPath == ""
            ? "Pick settings file"
            : GameManager.Singleton.settingsPath;
    }

    void Start()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            gameObject.SetActive(false);
            return;
        }

        conditionScene.SetActive(true);
        layoutScene.SetActive(false);

        nextButton.onClick.AddListener(() =>
        {
            layoutScene.SetActive(true);
            conditionScene.SetActive(false);
        });

        restartButton.onClick.AddListener(() =>
        {
            var layoutThumbs = GameManager.Singleton.layoutThumbs;
            Layout layout = layouts.Find(l => l.toggle.isOn);
            int ringCount = layoutThumbs[layout.layoutId].ringCount;
            int targetCount = layoutThumbs[layout.layoutId].targetCount;
            string path = pickSettingsFileButtonLabel.text;

            GameManager.Singleton.layoutId = layout.layoutId;
            GameManager.Singleton.posId = posScroller.CurrentPage;
            GameManager.Singleton.povId = povScroller.CurrentPage;
            GameManager.Singleton.settingsPath = path;

            ServerManager.Singleton.StartCondition(
                new(
                    GameManager.Singleton.posThumbs[posScroller.CurrentPage].name,
                    GameManager.Singleton.povThumbs[povScroller.CurrentPage].name.Split(" - ")[0],
                    GameManager.Singleton.povThumbs[povScroller.CurrentPage].name.Contains("Assisted"),
                    ringCount,
                    targetCount
                ),
                GameManager.Singleton.settingsPath == "Pick settings file" ? "" : GameManager.Singleton.settingsPath
            );
        });

        pickSettingsFileButton.onClick.AddListener(() =>
        {
            var path = StandaloneFileBrowser.OpenFilePanel("Open settings file", "", "json", false);
            if (path.Length > 0)
            {
                pickSettingsFileButtonLabel.text = path[0];
            }
        });

        SetConnectedClientsLabel();
        ServerManager.Singleton.OnClientConnected += SetConnectedClientsLabel;
        ServerManager.Singleton.OnClientDisconnected += SetConnectedClientsLabel;
    }

    void SetConnectedClientsLabel()
    {
        var spectatorIds = ServerManager.Singleton.ClientId_SpectatorId.Values;
        connectedClientsLabel.text = $"Connected clients:\n{spectatorIds.Aggregate("", (acc, id) => acc + id + "\n")}";
    }

    void OnDestroy()
    {
        ServerManager.Singleton.OnClientConnected -= SetConnectedClientsLabel;
        ServerManager.Singleton.OnClientDisconnected -= SetConnectedClientsLabel;
    }
}
