using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    List<GameObject> randomSpheresServer = new();
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
    }

    public async Task SpawnSpheres((int RingCount, int TargetCount) counts)
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

        if (NetworkManager.Singleton.IsServer)
        {
            var keyPrefix = GetSphereKeyPrefix(ringCount, targetCount);

            var precalculatedSphereNames = await CloudSaveManager.Singleton.Load<List<string>>(
                $"{keyPrefix}sphereNames"
            );
            var precalculatedSpheresIndex = await CloudSaveManager.Singleton.Load<int>(
                $"{keyPrefix}spheresIndex"
            );
            var precalculatedAvgTrialCountPerTarget = await CloudSaveManager.Singleton.Load<float>(
                $"{keyPrefix}avgTrialCountPerTarget"
            );

            if (precalculatedSphereNames == null)
            {
                await UpdatePrecalculatedSpheres();

                precalculatedSphereNames = randomSpheresServer.ConvertAll(
                    sphere => sphere.name
                );
                precalculatedSpheresIndex = 0;
            }

            var newRandomSphereNames = precalculatedSphereNames
                .GetRange(precalculatedSpheresIndex, ServerManager.Singleton.TRIAL_PER_CONDITION_COUNT)
                .ConvertAll(name => spheres.Find(sphere => sphere.name == name));
            randomSpheresServer = newRandomSphereNames;

            randomSpheresIndexServer = 0;
        }
    }

    string GetSphereKeyPrefix(int ringCount, int targetCount)
    {
        var keyPrefix = $"{ringCount}_{targetCount}" +
            $"_{GameManager.Singleton.posThumbs[GameManager.Singleton.posId].name}" +
            $"_{GameManager.Singleton.povThumbs[GameManager.Singleton.povId].name}_";
        return string.Concat(keyPrefix.Where(c => !char.IsWhiteSpace(c)));
    }

    public async Task UpdatePrecalculatedSpheres()
    {
        var ringCount = GameManager.Singleton.layoutThumbs[GameManager.Singleton.layoutId].ringCount;
        var targetCount = GameManager.Singleton.layoutThumbs[GameManager.Singleton.layoutId].targetCount;
        var TRIAL_PER_CONDITION_COUNT = ServerManager.Singleton.TRIAL_PER_CONDITION_COUNT;
        var keyPrefix = GetSphereKeyPrefix(ringCount, targetCount);

        var precalculatedSphereNames = await CloudSaveManager.Singleton.Load<List<string>>(
            $"{keyPrefix}sphereNames"
        );
        var precalculatedSpheresIndex = await CloudSaveManager.Singleton.Load<int>(
            $"{keyPrefix}spheresIndex"
        );
        var precalculatedAvgTrialCountPerTarget = await CloudSaveManager.Singleton.Load<float>(
            $"{keyPrefix}avgTrialCountPerTarget"
        );
        var precalculatedSphereNamesWasNull = precalculatedSphereNames == null;
        precalculatedSpheresIndex += TRIAL_PER_CONDITION_COUNT;

        if (precalculatedSphereNames == null || precalculatedSpheresIndex >= precalculatedSphereNames.Count)
        {
            randomSpheresServer = Util.Shuffle(spheres);

            precalculatedSphereNames = randomSpheresServer.ConvertAll(
                sphere => sphere.name
            );
            precalculatedSpheresIndex = 0;

            await CloudSaveManager.Singleton.Save($"{keyPrefix}spheresIndex", precalculatedSpheresIndex);
            await CloudSaveManager.Singleton.Save($"{keyPrefix}sphereNames", precalculatedSphereNames);
        }

        if (precalculatedSphereNamesWasNull) return;

        await CloudSaveManager.Singleton.Save($"{keyPrefix}spheresIndex", precalculatedSpheresIndex);
        await CloudSaveManager.Singleton.Save(
            $"{keyPrefix}avgTrialCountPerTarget",
            precalculatedAvgTrialCountPerTarget + 1f * TRIAL_PER_CONDITION_COUNT / targetCount / ringCount
        );
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
