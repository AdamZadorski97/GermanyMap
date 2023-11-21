using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomController : MonoBehaviour
{
    GeoJSONFileReader geoJSONFileReader;
  List<  MapZoomProperties> zoomProperties = new List<MapZoomProperties>();


    private void Awake()
    {

        geoJSONFileReader = GetComponent<GeoJSONFileReader>();
        zoomProperties = geoJSONFileReader.mapTransformProperties.zoomProperties;
    }


    public void OnZoomChange()
    {
        int currentZoom = geoJSONFileReader.currentZoom;

        foreach (var prop in zoomProperties)
        {
            if (currentZoom > prop.zoomLevelToSwitch)
            {
                if (geoJSONFileReader.currentType != prop.mapType)
                {
                    Debug.Log($"Changing map type to {prop.mapType} at zoom level {currentZoom}");
                    geoJSONFileReader.SetupMap(prop.mapType);
                }
                break; // Break the loop once the appropriate map type is found
            }
        }
    }
}
