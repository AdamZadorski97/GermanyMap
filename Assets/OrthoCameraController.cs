using UnityEngine;

public class OrthoCameraController : MonoBehaviour
{
    public float panSpeedX = 0.005f;
    public float panSpeedY = 0.005f;
    public float zoomSpeed = 0.5f;
    public float minZoom = 1f;
    public float maxZoom = 10f;

    public AnimationCurve zoomFactorCurve;

    private Vector3 lastPanPosition;

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastPanPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            PanCamera(Input.mousePosition);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        ZoomCamera(scroll, zoomSpeed);
    }

    void PanCamera(Vector3 newPanPosition)
    {
        Vector3 offset = lastPanPosition - newPanPosition;
        Camera cam = GetComponent<Camera>();

        float zoomFactor = zoomFactorCurve.Evaluate(cam.orthographicSize);

        Vector3 move = new Vector3(offset.x * panSpeedX * zoomFactor, offset.y * panSpeedY * zoomFactor, 0);
        transform.Translate(move, Space.World);
        lastPanPosition = newPanPosition;
    }

    void ZoomCamera(float offset, float speed)
    {
        if (offset == 0)
        {
            return;
        }

        Camera cam = GetComponent<Camera>();
        float newSize = cam.orthographicSize - (offset * speed);
        newSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        cam.orthographicSize = newSize;
    }
}