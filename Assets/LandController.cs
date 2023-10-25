using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandController : MonoBehaviour
{
    public GeoJSONFeature geoJSONFeature;
    public LineRenderer lineRenderer;
    public TextMesh textMesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public Material highlightMaterial; // Assign a material with the green color in the Inspector
    public Material originalMaterial;
    public MeshRenderer meshRenderer;
    public List<Vector3> realCoordinates = new List<Vector3>();
    public Vector3 center;


    [ShowInInspector]
    public string plz { get; set; }
    [ShowInInspector]
    public string note { get; set; }
    [ShowInInspector]
    public int einwohner { get; set; }
    [ShowInInspector]
    public double qkm { get; set; }

    [ShowInInspector]
    public List<Vector3> coordinates { get; set; }



    [ShowInInspector]
    public SerializableGeoDate serializableGeoDate { get; set; }


    private void OnMouseEnter()
    {
        Highlight();
    }


    private void OnMouseExit()
    {
        ResetLand();
    }

    public void SetupText()
    {
        if (lineRenderer.positionCount == 0)
        {
            Debug.LogError("LineRenderer has no positions.");
            return;
        }

        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);

        center = GetCenterOfPositions(linePositions);
        textMesh.transform.localPosition = center;
        textMesh.text = plz;
    }

    public void Highlight()
    {
        if (plz != "")
        {
            MapUserInterface.Instance.SetPlzText(plz);
        }
        meshRenderer.material = highlightMaterial;
    }

    public void ResetLand()
    {
        meshRenderer.material = originalMaterial;
    }


    Vector3 GetCenterOfPositions(Vector3[] positions)
    {
        if (positions.Length == 0)
        {
            Debug.LogError("Array of positions is empty.");
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        foreach (Vector3 position in positions)
        {
            sum += position;
        }

        return sum / positions.Length;
    }
}


