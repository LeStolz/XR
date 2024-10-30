using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private GameObject conditionScene;
    [SerializeField] private GameObject layoutScene;
    [SerializeField] private HorizontalScrollSnap posScroller;
    [SerializeField] private HorizontalScrollSnap povScroller;
    [SerializeField] private List<Layout> layouts;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI connectedClientsLabel;

    void Awake()
    {
        layouts.Find(l => l.layoutId == GameManager.Singleton.layoutId).toggle.isOn = true;
        posScroller.StartingScreen = GameManager.Singleton.posId;
        povScroller.StartingScreen = GameManager.Singleton.povId;
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

            GameManager.Singleton.layoutId = layout.layoutId;
            GameManager.Singleton.posId = posScroller.CurrentPage;
            GameManager.Singleton.povId = povScroller.CurrentPage;

            ServerManager.Singleton.StartEvaluation(
                new(
                    GameManager.Singleton.posThumbs[posScroller.CurrentPage].name,
                    GameManager.Singleton.povThumbs[povScroller.CurrentPage].name.Split(" - ")[0],
                    GameManager.Singleton.povThumbs[povScroller.CurrentPage].name.Contains("Assisted"),
                    ringCount,
                    targetCount
                )
            );
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
}
