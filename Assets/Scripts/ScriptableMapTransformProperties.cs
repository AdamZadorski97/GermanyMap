using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapTransformProperties", menuName = "ScriptableObjects/MapTransformProperties", order = 1)]
public class ScriptableMapTransformProperties : ScriptableObject
{
    public AnimationCurve animationCurveVertical;
    public AnimationCurve animationCurveHorisontal;
    public List<MapTransformProperties> zoomRanges;
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
