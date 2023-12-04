
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class LandController : MonoBehaviour
{
    public GeoJSONFeature geoJSONFeature;
    public LineRenderer lineRenderer;
    public TextMesh textMesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public Material highlightMaterial; // Assign a material with the green color in the Inspector
    public Material defaultMaterial;
    public Material clickMaterial;
    public MeshRenderer meshRenderer;
    public List<Vector3> realCoordinates = new List<Vector3>();
    public List<Vector3> realCoordinatesToCentering = new List<Vector3>();
    public Vector3 center;
    public string detailedText;
    public string onMapText;
    public MapType mapType;
    private bool isHighlighted = false;
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
    public string Bundesland { get; set; }
    public string NAME_0 { get; set; }
    [ShowInInspector]
    public int ID_1 { get; set; }
    [ShowInInspector]
    public string NAME_1 { get; set; }
    [ShowInInspector]
    public int ID_2 { get; set; }
    [ShowInInspector]
    public string Regierungsbezirk { get; set; }
    [ShowInInspector]
    public int ID_3 { get; set; }
    [ShowInInspector]
    public string KREIS_Name { get; set; }
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

    private Vector3 mouseDownPosition;
    private const float clickThreshold = 0.1f;
    private Sequence OnClickSeqence;
    public Vector3 textIntialScale;
   


    private void OnMouseDown()
    {
        mouseDownPosition = Input.mousePosition;
    }

    private void OnMouseUp()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // If it is over a UI element, return early and do nothing
            return;
        }

        // Your existing distance check to determine if it's a click and not a drag
        if (Vector3.Distance(mouseDownPosition, Input.mousePosition) < clickThreshold)
        {
            OnMouseClick();
        }
    }
    private Vector3 originalPosition;

    public void SetupText(string onMapText = "", string _detailedText = "")
    {
        detailedText = _detailedText;
        if (lineRenderer.positionCount == 0)
        {
            Debug.LogError("LineRenderer has no positions.");
            return;
        }

        Vector3[] linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);
        textMesh.transform.localPosition = Vector3.zero;
        textMesh.text = onMapText;
        originalPosition = transform.position;
        CenterPivotAndAdjustLineRenderer();
    }

    public void OnMouseClick()
    {
        Vector3 center = CalculateCenter(realCoordinatesToCentering);
        float targetZoom = 7;
        float duration = 0.25f;
        Vector2 startPositionAndZoom = new Vector2(GeoJSONFileReader.Instance.OnlineMaps.position.x, GeoJSONFileReader.Instance.OnlineMaps.position.y);
        float startZoom = GeoJSONFileReader.Instance.OnlineMaps.zoom;
        Debug.Log(center);
        // GeoJSONFileReader.Instance.OnlineMaps.SetPositionAndZoom(realCoordinatesToCentering[0].x, realCoordinatesToCentering[0].z, 7);
        DOTween.To(() => startPositionAndZoom, x => GeoJSONFileReader.Instance.OnlineMaps.SetPosition(x.x, x.y), new Vector2(center.x, center.z), duration);

        OnClickSeqence = DOTween.Sequence();
        OnClickSeqence.Append(meshRenderer.material.DOColor(clickMaterial.color, duration));
        OnClickSeqence.Append(meshRenderer.material.DOColor(defaultMaterial.color, duration));
    }

    public void HighlightOnMouseEnter()
    {
        if (!isHighlighted)
        {
            isHighlighted = true;
            MapUserInterface.Instance.SetText(detailedText);
            string identifier = GeoJSONFileReader.Instance.GetIdentifierBasedOnCurrentType(this);


            if (identifier != null)
            {
                if (GeoJSONFileReader.Instance.identifierToLandControllersMap != null)
                {
                    if (GeoJSONFileReader.Instance.identifierToLandControllersMap.TryGetValue(identifier, out var landControllers))
                    {
                        foreach (var controller in landControllers)
                        {
                            controller.Highlight();
                        }
                    }
                }


            }
            else
            {
                Highlight();
            }
        }
    }

    private void Highlight()
    {
      
            meshRenderer.enabled = true;
            OnClickSeqence = DOTween.Sequence();
            OnClickSeqence.Append(meshRenderer.material.DOColor(highlightMaterial.color, 0.25f));

            Vector3 textHighlightScale = textMesh.transform.localScale * 1.1f;
            textMesh.transform.DOScale(textHighlightScale, 0.2f);
        
    }

    public void ResetLand()
    {
        if (OnClickSeqence != null)
        {
            meshRenderer.material = defaultMaterial; // Assuming defaultMaterial is the original material
            textMesh.transform.DOScale(textIntialScale, 0.2f); // Resetting the scale of textMesh
        }
    }
    public void DehighlightOnMouseExit()
    {
        if (isHighlighted)
        {
            meshRenderer.enabled = false;
            string identifier = GeoJSONFileReader.Instance.GetIdentifierBasedOnCurrentType(this);
            if (identifier != null)
            {
                if (GeoJSONFileReader.Instance.identifierToLandControllersMap.TryGetValue(identifier, out var landControllers))
                {
                    foreach (var controller in landControllers)
                    {
                        isHighlighted = false;
                        controller.ResetLand();
                    }
                }
            }
            else
            {
                isHighlighted = false;
                ResetLand();
            }
        }
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
    private Vector3 CalculateCenter(List<Vector3> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (Vector3 coord in coordinates)
        {
            sum += coord;
        }
        return sum / coordinates.Count;
    }
    public float GetAreaSize()
    {
        if (meshFilter == null || meshFilter.mesh == null)
            return 0f;

        Mesh mesh = meshFilter.mesh;
        float area = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            area += TriangleArea(v1, v2, v3);
        }

        return area;
    }

    private float TriangleArea(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // Calculate the area of a triangle given its vertices
        Vector3 crossProduct = Vector3.Cross(v2 - v1, v3 - v1);
        return crossProduct.magnitude * 0.5f;
    }
}
