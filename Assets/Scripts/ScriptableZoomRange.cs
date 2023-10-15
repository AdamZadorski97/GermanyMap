using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZoomRange", menuName = "ScriptableObjects/ZoomRange", order = 1)]
public class ScriptableZoomRange : ScriptableObject
{
    public List<ZoomRanges> zoomRanges;
}
[Serializable]
public class ZoomRanges
{
    public float minZoom;
    public float maxZoom;
    public int elementIndex;
}