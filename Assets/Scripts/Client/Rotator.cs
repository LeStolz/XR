using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private Transform target;
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

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        radius = Vector2.Distance(
            new(target.position.x, target.position.z),
            new(transform.position.x, transform.position.z)
        );

        started = true;
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
}
