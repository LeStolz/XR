using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Rotator : NetworkBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Camera mainCamera;
    [SerializeField] Camera topDownCamera;
    [SerializeField] Image mainImage;
    [SerializeField] Image topDownImage;

    public event Action<float> OnRotate;

    float radius;
    Vector3 initialPosition;
    Quaternion initialRotation;
    int targetCount;
    float stoppedDraggingTime = 0;
    float snapAngle = -1;
    bool started = false;

    void OnMouseDown()
    {
        stoppedDraggingTime = -1;
    }

    void OnMouseUp()
    {
        stoppedDraggingTime = Time.time;
    }

    void Start()
    {
        ClientManager.Singleton.OnTrialInit += OnTrialInit;
    }

    void Update()
    {
        if (radius <= 0) return;

        float currentAngle = Mathf.Atan2(
            transform.position.z - target.position.z,
            transform.position.x - target.position.x
        );
        float newSnapAngle = Mathf.Round(currentAngle / (2 * Mathf.PI / targetCount)) * (2 * Mathf.PI / targetCount);

        if (snapAngle != newSnapAngle)
        {
            snapAngle = newSnapAngle;
            OnRotate?.Invoke(snapAngle);
        }

        if (stoppedDraggingTime < 0)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            currentMousePosition.z = 10;
            currentMousePosition = topDownCamera.ScreenToWorldPoint(currentMousePosition);

            RotateAround(Mathf.Atan2(
                currentMousePosition.z - target.position.z,
                currentMousePosition.x - target.position.x
            ));
        }
        else if (Mathf.Abs(currentAngle - snapAngle) > Util.EPS)
        {
            var currentQuaternion = Quaternion.Slerp(
                Quaternion.Euler(0, currentAngle * Mathf.Rad2Deg, 0),
                Quaternion.Euler(0, snapAngle * Mathf.Rad2Deg, 0),
                (Time.time - stoppedDraggingTime) * 5
            );

            RotateAround(currentQuaternion.eulerAngles.y * Mathf.Deg2Rad);
        }
    }

    void OnTrialInit()
    {
        if (!started) return;

        transform.SetPositionAndRotation(initialPosition, initialRotation);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ClientManager.Singleton.OnTrialInit -= OnTrialInit;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetInitialPositionRpc(
        float radius, float initialHeight, int targetCount, float scale
    )
    {
        scale *= 10f;
        topDownImage.transform.localScale = new Vector3(scale / 2, scale / 2, scale / 2);
        mainImage.transform.localScale = new Vector3(scale / 1.5f, scale / 1.5f, scale / 1.5f);

        this.targetCount = targetCount;
        this.radius = radius - 0.02f;

        RotateAround(360 * Mathf.Deg2Rad);

        mainCamera.transform.position = new(
            mainCamera.transform.position.x,
            initialHeight,
            mainCamera.transform.position.z
        );

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        started = true;
    }

    void RotateAround(float currentAngle)
    {
        transform.SetPositionAndRotation(new Vector3(
            target.position.x + radius * Mathf.Cos(currentAngle),
            transform.position.y,
            target.position.z + radius * Mathf.Sin(currentAngle)
        ), Quaternion.Euler(0, 180 - currentAngle * Mathf.Rad2Deg, 0));
    }
}
