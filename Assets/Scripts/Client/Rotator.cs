using Unity.Netcode;
using UnityEngine;

public class Rotator : NetworkBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Camera rotator;
    [SerializeField] private new Camera camera;

    private float radius;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isDragging = false;
    private bool started = false;

    void OnMouseDown()
    {
        isDragging = true;
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void Update()
    {
        if (isDragging && radius != 0)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            currentMousePosition.z = 10;
            currentMousePosition = camera.ScreenToWorldPoint(currentMousePosition);

            float currentAngle = Mathf.Atan2(
                currentMousePosition.z - target.position.z,
                currentMousePosition.x - target.position.x
            );

            transform.SetPositionAndRotation(new Vector3(
                target.position.x + radius * Mathf.Cos(currentAngle),
                transform.position.y,
                target.position.z + radius * Mathf.Sin(currentAngle)
            ), Quaternion.Euler(0, 180 - currentAngle * Mathf.Rad2Deg, 0));
        }
    }

    public void Reset()
    {
        if (!started) return;

        transform.SetPositionAndRotation(initialPosition, initialRotation);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetInitialPositionRpc(float newRadius, float newInitialHeight)
    {
        radius = newRadius;

        var currentAngle = 360 * Mathf.Deg2Rad;
        transform.SetPositionAndRotation(new Vector3(
            target.position.x + radius * Mathf.Cos(currentAngle),
            transform.position.y,
            target.position.z + radius * Mathf.Sin(currentAngle)
        ), Quaternion.Euler(0, 180 - currentAngle * Mathf.Rad2Deg, 0));

        rotator.transform.position = new(
            rotator.transform.position.x,
            newInitialHeight,
            rotator.transform.position.z
        );

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        started = true;
    }
}
