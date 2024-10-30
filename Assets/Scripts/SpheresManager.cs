using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpheresManager : NetworkBehaviour
{
    public const float RADIUS = 3f;
    public float INITIAL_HEIGHT = 3f;
    public static readonly Dictionary<int, float> RING_COUNT_GAP = new() {
        { 3, 2f },
        { 6, 1f }
    };

    public static SpheresManager Singleton { get; private set; }

    [SerializeField] private new Camera camera;
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private Material defaultSphereMaterial;
    [SerializeField] private Material selectedSphereMaterial;

    private List<GameObject> spheres = new();
    private GameObject selectedSphere = null;
    private float startHeight;

    private List<GameObject> randomSpheres = new();
    private int randomSpheresIndex = 0;

    void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            INITIAL_HEIGHT = camera.transform.position.y;
        }
    }

    private void ClearSpheres()
    {
        foreach (GameObject sphere in spheres)
        {
            Destroy(sphere);
        }

        spheres.Clear();
        randomSpheres.Clear();
        randomSpheresIndex = 0;
        selectedSphere = null;
    }

    public void ResetSpheres()
    {
        if (selectedSphere == null) return;

        selectedSphere.GetComponent<Renderer>().material = defaultSphereMaterial;
        selectedSphere = null;
    }

    public void SelectSphere(GameObject selectedSphere)
    {
        ResetSpheres();
        this.selectedSphere = selectedSphere;
        selectedSphere.GetComponent<Renderer>().material = selectedSphereMaterial;
    }

    public Vector3 GetLowestRingOrigin()
    {
        return new(0, startHeight, 0);
    }

    public void SpawnSpheres(int ringCount, int targetCount)
    {
        ClearSpheres();

        startHeight = INITIAL_HEIGHT - RING_COUNT_GAP[ringCount] * (ringCount - 1) / 2;

        for (int ring = 0; ring < ringCount; ring++)
        {
            float height = ring * RING_COUNT_GAP[ringCount] + startHeight;

            for (int i = 0; i < targetCount; i++)
            {
                float angle = -i * Mathf.PI * 2 / targetCount + Mathf.PI;

                Vector3 position = new(Mathf.Cos(angle) * RADIUS, height, Mathf.Sin(angle) * RADIUS);

                GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity);
                spheres.Add(sphere);
                randomSpheres.Add(sphere);
                randomSpheres.Add(sphere);
                sphere.GetComponent<NetworkObject>().Spawn(true);
                sphere.GetComponent<SphereSelector>().SetNameRpc($"{ring};{i}");
            }
        }

        Shuffle(randomSpheres);
        randomSpheresIndex = 0;
    }

    public Tuple<string, Vector3> GetRandomSphere()
    {
        if (
            randomSpheres.Count == 0 ||
            randomSpheresIndex >= randomSpheres.Count
        ) return null;

        var sphere = new Tuple<string, Vector3>(
            randomSpheres[randomSpheresIndex].name,
            randomSpheres[randomSpheresIndex].transform.position
        );
        randomSpheresIndex++;

        return sphere;
    }

    public Tuple<string, Vector3> GetSelectedSphere()
    {
        if (selectedSphere == null) return null;

        return new(selectedSphere.name, selectedSphere.transform.position);
    }




    private void Shuffle<T>(IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;

        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            (ts[r], ts[i]) = (ts[i], ts[r]);
        }
    }
}
