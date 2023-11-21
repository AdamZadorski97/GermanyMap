
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
    public List<Vector3> realCoordinatesToCentering = new List<Vector3>();
    public Vector3 center;

    [ShowInInspector]
    public string geometry { get; set; }
    [ShowInInspector]
    public string plz { get; set; }
    [ShowInInspector]
    public string note { get; set; }
    [ShowInInspector]
    public int einwohner { get; set; }
    [ShowInInspector]
    public double qkm { get; set; }

    [ShowInInspector]
    public int ID_0 { get; set; }
    [ShowInInspector]
    public string ISO { get; set; }
    [ShowInInspector]
    public string name { get; set; }
    public string NAME_0 { get; set; }
    [ShowInInspector]
    public int ID_1 { get; set; }
    [ShowInInspector]
    public string NAME_1 { get; set; }
    [ShowInInspector]
    public int ID_2 { get; set; }
    [ShowInInspector]
    public string NAME_2 { get; set; }
    [ShowInInspector]
    public int ID_3 { get; set; }
    [ShowInInspector]
    public string NAME_3 { get; set; }
    [ShowInInspector]
    public string NL_NAME_3 { get; set; }
    [ShowInInspector]
    public string VARNAME_3 { get; set; }
    [ShowInInspector]
    public string TYPE_3 { get; set; }
    [ShowInInspector]
    public string ENGTYPE_3 { get; set; }

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
    private Vector3 originalPosition;

    public void SetupText(string text)
    {
        if (lineRenderer.positionCount == 0)
        {
            Debug.LogError("LineRenderer has no positions.");
            return;
        }

        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);
        textMesh.transform.localPosition = Vector3.zero;
        textMesh.text = text;
        originalPosition = transform.position;
        CenterPivotAndAdjustLineRenderer();

    }

    public void Highlight()
    {
        if (plz != "")
        {
            MapType currentType = GeoJSONFileReader.Instance.currentType;


            if (currentType == MapType.Plz1Stelig || currentType == MapType.Plz2Stelig || currentType == MapType.Plz3Stelig || currentType == MapType.Plz5Stelig)
                MapUserInterface.Instance.SetText(plz);
            if (currentType == MapType.Kreise)
                MapUserInterface.Instance.SetText(NAME_3);
            if (currentType == MapType.Bundeslaender)
                MapUserInterface.Instance.SetText(name);
            if (currentType == MapType.Regierungsbezirke)
                MapUserInterface.Instance.SetText(NAME_2);
        }
        meshRenderer.material = highlightMaterial;
    }

    public void ResetLand()
    {
        meshRenderer.material = originalMaterial;
    }


    private void CenterPivotAndAdjustLineRenderer()
    {
        if (meshFilter == null || lineRenderer == null) return;

        // Calculate the centroid of the mesh
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 vertex in meshFilter.mesh.vertices)
        {
            centroid += transform.TransformPoint(vertex); // Get the world position of each vertex
        }
        centroid /= meshFilter.mesh.vertexCount;

        // Offset to move the pivot to the centroid
        Vector3 pivotOffset = centroid - transform.position;

        // Adjust vertices of mesh
        Vector3[] vertices = meshFilter.mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = transform.InverseTransformPoint(transform.TransformPoint(vertices[i]) - pivotOffset);
        }
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateBounds();

        // Adjust line renderer positions
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector3 linePos = lineRenderer.GetPosition(i);
            lineRenderer.SetPosition(i, transform.InverseTransformPoint(transform.TransformPoint(linePos) - pivotOffset));
        }

        // Adjust the transform position to reflect the new pivot
        transform.position = centroid;

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            try
            {
                meshCollider.sharedMesh = meshFilter.mesh;
            }
            catch (System.Exception)
            {

                throw;
            }
          
        }
    }

}
