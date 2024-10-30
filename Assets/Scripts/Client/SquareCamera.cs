using UnityEngine;

public class SquareCamera : MonoBehaviour
{
    [SerializeField] private bool anchorLeft;
    [SerializeField] private float y;
    [SerializeField] private float width;
    [SerializeField] private new Camera camera;

    void Start()
    {
        SquarizeCamera();
    }

    void SquarizeCamera()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        float height = width * screenWidth / screenHeight;
        float x = anchorLeft ? 0.03f : 1 - width - 0.03f;

        camera.rect = new Rect(x, y, width, height);
    }
}
