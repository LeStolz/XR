using Unity.Netcode;
using UnityEngine;

public class SphereSelector : NetworkBehaviour
{
    void OnMouseDown()
    {
        SpheresManager.Singleton.SelectSphere(gameObject);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetPropertiesRpc(string name, float scale)
    {
        gameObject.name = name;
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
