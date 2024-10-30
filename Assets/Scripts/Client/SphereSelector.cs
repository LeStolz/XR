using Unity.Netcode;
using UnityEngine;

public class SphereSelector : NetworkBehaviour
{
    void OnMouseDown()
    {
        SpheresManager.Singleton.SelectSphere(gameObject);
        Debug.Log(gameObject.name);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetNameRpc(string name)
    {
        gameObject.name = name;
    }
}
