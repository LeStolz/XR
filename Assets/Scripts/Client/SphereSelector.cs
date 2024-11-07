using Unity.Netcode;

public class SphereSelector : NetworkBehaviour
{
    void OnMouseDown()
    {
        SpheresManager.Singleton.SelectSphere(gameObject);
    }
}
