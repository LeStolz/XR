using UnityEngine;

public class SquareCamera : MonoBehaviour
{
    [SerializeField] bool anchorLeft;
    [SerializeField] float y;
    [SerializeField] float width;
    [SerializeField] float height;
    [SerializeField] new Camera camera;

    void Start()
    {
        SquarizeCamera();
    }

    void SquarizeCamera()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        float height = this.height > 0 ? this.height : width * screenWidth / screenHeight;
        float x = anchorLeft ? 0.03f : 1 - width - 0.03f;

        camera.rect = new Rect(x, y, width, height);
    }
}
