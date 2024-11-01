using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Thumbnail
{
    public string name;
    public Sprite sprite;
}

[Serializable]
public struct LayoutThumbnail
{
    public int ringCount;
    public int targetCount;
    public Sprite sprite;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Singleton { get; private set; }

    public string settingsPath = "";
    public string playerId = "";
    public string playerSeat = "";
    public string serverIp = "";

    public int layoutId = 0;
    public int posId = 0;
    public int povId = 0;

    public List<Thumbnail> posThumbs;
    public List<Thumbnail> povThumbs;
    public List<LayoutThumbnail> layoutThumbs;

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(gameObject);
    }
}