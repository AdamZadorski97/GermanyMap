using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapTransformProperties", menuName = "ScriptableObjects/MapTransformProperties", order = 1)]
public class ScriptableMapTransformProperties : ScriptableObject
{
    public AnimationCurve animationCurveVertical;
    public AnimationCurve animationCurveHorisontal;
    public List<MapTransformProperties> mapTransformProperties;
    public List<MapZoomProperties> zoomProperties;
}


[Serializable]
public class MapZoomProperties
{
    public int zoomLevelToSwitch;
    public MapType mapType;
}






[Serializable]
public class MapTransformProperties
{
    public int zoom;
    public Vector3 mapPositions;
    public Vector3 mapScales;
    public float linewidthMultiplier;
    public float textSizeMultiplier;
}
