using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpheresManager : NetworkBehaviour
{
    public static SpheresManager Singleton { get; private set; }

    public LayoutDimensions layoutDimensionsServer;
    [SerializeField] GameObject rotator;
    [SerializeField] GameObject spherePrefab;
    [SerializeField] Material defaultSphereMaterial;
    [SerializeField] Material focusedSphereMaterial;
    [SerializeField] Material selectedSphereMaterial;

    readonly List<GameObject> spheres = new();
    readonly List<GameObject> randomSpheresServer = new();
    GameObject selectedSphere = null;
    int randomSpheresIndexServer = 0;

    float snapAngle;
    ConditionalDimensions dimensions;
    float startHeight;

    void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
    }

    void Start()
    {
        rotator.GetComponent<Rotator>().OnRotate += OnRotate;
        ClientManager.Singleton.OnTrialInit += ResetSpheres;
    }

    void OnRotate(float snapAngle)
    {
        this.snapAngle = snapAngle;
        SelectSphere(selectedSphere);
    }

    void ResetSpheres()
    {
        foreach (GameObject sphere in spheres)
        {
            var sphereAngle = Mathf.Atan2(
                sphere.transform.position.z - transform.position.z,
                sphere.transform.position.x - transform.position.x
            );
            var isFocused =
                Mathf.Abs(sphereAngle - snapAngle) < Util.EPS ||
                Mathf.Abs(2 * Mathf.PI + sphereAngle - snapAngle) < Util.EPS;

            sphere.GetComponent<Renderer>().material = isFocused
                ? focusedSphereMaterial
                : defaultSphereMaterial;

            sphere.transform.localScale = isFocused
                ? dimensions.scale * 1.6f * Vector3.one
                : dimensions.scale * Vector3.one;
        }

        selectedSphere = null;
    }

    public void SelectSphere(GameObject selectedSphere)
    {
        ResetSpheres();
        this.selectedSphere = selectedSphere;

        if (selectedSphere == null) return;

        selectedSphere.GetComponent<Renderer>().material = selectedSphereMaterial;
    }

    public Vector3 GetLowestRingOrigin()
    {
        return new(layoutDimensionsServer.x, startHeight, layoutDimensionsServer.z);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    void SetDimensionsRpc(ConditionalDimensions dimensions)
    {
        this.dimensions = dimensions;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    void SpawnSphereRpc(Vector3 position, string name, float scale)
    {
        GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity);
        spheres.Add(sphere);
        sphere.name = name;
        sphere.transform.localScale = new Vector3(scale, scale, scale);

        if (NetworkManager.Singleton.IsServer)
        {
            randomSpheresServer.Add(sphere);
        }
    }

    public void SpawnSpheres((int RingCount, int TargetCount) counts)
    {
        int ringCount = counts.RingCount;
        int targetCount = counts.TargetCount;

        dimensions = layoutDimensionsServer.conditionalDimensions.Find(
            cd => cd.ringCount == ringCount && cd.targetCount == targetCount
        );
        SetDimensionsRpc(dimensions);

        if (NetworkManager.Singleton.IsServer)
        {
            rotator.GetComponent<Rotator>().SetInitialPositionRpc(
                layoutDimensionsServer.radius,
                layoutDimensionsServer.initialHeight,
                targetCount,
                dimensions.scale
            );
        }

        startHeight = layoutDimensionsServer.initialHeight - dimensions.gap * (ringCount - 1) / 2;

        for (int ring = 0; ring < ringCount; ring++)
        {
            float height = ring * dimensions.gap + startHeight;

            for (int i = 0; i < targetCount; i++)
            {
                float angle = -i * Mathf.PI * 2 / targetCount + Mathf.PI;

                Vector3 position = new(
                    Mathf.Cos(angle) * layoutDimensionsServer.radius,
                    height,
                    Mathf.Sin(angle) * layoutDimensionsServer.radius
                );

                SpawnSphereRpc(position, $"{ring};{i}", dimensions.scale);
            }
        }

        var spheresFirst = Util.Shuffle(randomSpheresServer);
        var spheresSecond = Util.Shuffle(randomSpheresServer);
        randomSpheresServer.Clear();
        randomSpheresServer.AddRange(spheresFirst);
        randomSpheresServer.AddRange(spheresSecond);
        randomSpheresIndexServer = 0;

        Debug.Log(string.Join(", ", randomSpheresServer.Select(sphere => sphere.name)));
    }

    public Tuple<string, Vector3> GetRandomSphere()
    {
        if (
            randomSpheresServer.Count == 0 ||
            randomSpheresIndexServer >= randomSpheresServer.Count
        ) return null;

        var sphere = new Tuple<string, Vector3>(
            randomSpheresServer[randomSpheresIndexServer].name,
            randomSpheresServer[randomSpheresIndexServer].transform.position
        );
        SelectSphere(randomSpheresServer[randomSpheresIndexServer]);
        randomSpheresIndexServer++;

        return sphere;
    }

    public Tuple<string, Vector3> GetSelectedSphere()
    {
        if (selectedSphere == null) return null;

        return new(selectedSphere.name, selectedSphere.transform.position);
    }
}
