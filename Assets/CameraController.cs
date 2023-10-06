using System;
using UnityEngine;

class CameraController : MonoBehaviour
{
    public OnlineMaps OnlineMaps;
    
    Vector3 touchStart;
    public float zoomOutMin = 1;
    public float zoomOutMax = 8;
    public float currentZoom;

    private void Awake()
    {
        currentZoom = OnlineMaps.zoom;
    }

    void Update()
    {
        HandleCamera();
    }

    private void HandleCamera()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            HandleZoom(difference * 0.1f);
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 direction = touchStart - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Camera.main.transform.position += direction;
        }

        //HandleZoom(Input.GetAxis("Mouse ScrollWheel") * 10);
    }

    void HandleZoom(float increment)
    {
        float calculatedZoom = Mathf.Clamp(Camera.main.orthographicSize - increment, zoomOutMin, zoomOutMax);
        currentZoom = calculatedZoom;
        Camera.main.orthographicSize = calculatedZoom;
    }
}