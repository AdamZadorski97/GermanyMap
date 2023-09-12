using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Sirenix.OdinInspector;
using System.Linq;
using System.Collections;

public class GeoJSONCoordinate
{
    public double[] coordinates { get; set; }
}
public class GeoJSONGeometry
{
    public string type { get; set; }
    public List<List<List<GeoJSONCoordinate>>> coordinates { get; set; }
}

public class GeoJSONData
{
    public string type { get; set; }
    public List<GeoJSONFeature> features { get; set; }
}

public class Crs
{
    public string type;
    public CrsProperties properties;
}

public class CrsProperties
{
    public string name;
}

public class GeoJSONFeature
{
    public string type { get; set; }
    public GeoJSONGeometry geometry { get; set; }
    public GeoJSONProperties properties { get; set; }
}

public class GeoJSONProperties
{
    public string plz { get; set; }
    public string note { get; set; }
    public int einwohner { get; set; }
    public double qkm { get; set; }
}

public class GeoJSONFileReader : MonoBehaviour
{
    [SerializeField]
    public string geoJSONFilePath;
    private GeoJSONData geoJSONData;
    [SerializeField]
    private GameObject landPrefab;
    public float scale = 100000;
    public float simplify = 0.001f;

    [Button]
    public void LoadGeoJSON()
    {
        Debug.Log("Start Load GeoJson");
        string filePath = Path.Combine(Application.streamingAssetsPath, geoJSONFilePath);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new GeoJSONCoordinateConverter() }
            };
            geoJSONData = JsonConvert.DeserializeObject<GeoJSONData>(json, settings);
            if (geoJSONData != null) { Debug.Log("GeoJSON data parsed successfully."); }
            else { Debug.LogError("Failed to parse GeoJSON data."); }
        }
        else
        {
            Debug.Log("Path doesn't exists");
        }
    }

    [Button]
    public void StartDrawingMeshes()
    {
        StartCoroutine(Draw2DMeshesFromLineRenderers());
    }

    private IEnumerator Draw2DMeshesFromLineRenderers()
    {
        if (geoJSONData == null || geoJSONData.features == null)
        {
            Debug.LogError("No GeoJSON data or features found.");
            yield break;  // exit the coroutine
        }

        Debug.Log($"Number of features: {geoJSONData.features.Count}");
        int index = 0;

        foreach (GeoJSONFeature feature in geoJSONData.features)
        {
            Debug.Log($"Feature Type: {feature.geometry?.type}");
            if (feature.geometry != null && (feature.geometry.type == "Polygon" || feature.geometry.type == "MultiPolygon"))
            {
                List<Vector3> lineRendererPositions = new List<Vector3>();
                List<Vector3> innerMeshPositions = new List<Vector3>();

                if (feature.geometry.type == "MultiPolygon")
                {
                    foreach (var polygon in feature.geometry.coordinates)
                    {
                        foreach (var ring in polygon)
                        {
                            foreach (var coordPair in ring)
                            {
                                ProcessCoordinate(coordPair, scale, lineRendererPositions, innerMeshPositions);
                            }
                        }
                    }
                }
                else if (feature.geometry.type == "Polygon")
                {
                    foreach (var ring in feature.geometry.coordinates)
                    {
                        foreach (var coordPair in ring)
                        {
                            foreach (var coord in coordPair)
                            {
                                ProcessCoordinate(coord, scale, lineRendererPositions, innerMeshPositions);
                            }
                        }
                    }
                }

                GameObject land = Instantiate(landPrefab);
                land.name = feature.properties.plz + feature.geometry.type;
                LineRenderer lineRenderer = land.GetComponent<LandController>().lineRenderer;
                lineRenderer.positionCount = lineRendererPositions.Count;
                lineRenderer.SetPositions(lineRendererPositions.ToArray());
                int[] indices = TriangulateMultiPolygon(lineRendererPositions).ToArray();
                Mesh mesh = CreateMeshFromIndices(lineRendererPositions.Concat(innerMeshPositions).ToList(), indices);
                Vector3 center = CalculateMeshCenter(mesh);
                land.GetComponent<LandController>().meshFilter.mesh = mesh;
                land.transform.position = center;
                TextMesh textMesh = land.GetComponent<LandController>().textMesh;
                textMesh.text = $"{feature.properties.plz}\n {lineRendererPositions.Count}";

                index++;
                yield return new WaitForEndOfFrame();
            }
            else
            {
                Debug.LogWarning("Feature geometry or polygon/multi-polygon data is null");
            }
        }
    }

    private void ProcessCoordinate(GeoJSONCoordinate coord, float scale, List<Vector3> lineRendererPositions, List<Vector3> innerMeshPositions)
    {
        double[] coordPair = coord.coordinates;
        if (coordPair.Length >= 2)
        {
            Vector3 position = new Vector3((float)coordPair[0] * scale, (float)coordPair[1] * scale, 0f);
            lineRendererPositions.Add(position);
            innerMeshPositions.Add(position);
        }
        else
        {
            Debug.LogWarning("Skipping invalid coordinate with fewer than 2 elements.");
        }
    }

    private Mesh CreateMeshFromIndices(List<Vector3> vertices, int[] indices)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++) { normals[i] = -normals[i]; }
        mesh.normals = normals;
        mesh.RecalculateBounds();
        return mesh;
    }

    private Vector3 CalculateMeshCenter(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        if (vertices.Length == 0)
        {
            Debug.LogError("Vertices array is empty.");
            return Vector3.zero;  // return a zero vector if the array is empty
        }
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];
        foreach (Vector3 vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }
        return (min + max) * 0.5f;
    }

    private List<int> TriangulatePolygon(List<Vector3> vertices)
    {
        // Initialize a list to store indices
        List<int> indices = new List<int>();

        // Add your triangulation logic for a single Polygon here
        // Example logic: return a list of indices that connect the vertices sequentially
        int numVertices = vertices.Count;

        for (int i = 1; i < numVertices - 1; i++)
        {
            indices.Add(0);
            indices.Add(i);
            indices.Add(i + 1);
        }

        return indices;
    }

    private List<int> TriangulateMultiPolygon(List<Vector3> vertices)
    {
        // Initialize a list to store indices
        List<int> indices = new List<int>();

        // Add your triangulation logic for a MultiPolygon here
        // You need to consider multiple rings if present
        // Example logic: return a list of indices that connect the vertices sequentially
        // across all rings
        // Note: You'll need to handle cases where there are multiple rings.

        // Example: For simplicity, we can assume that all rings are connected sequentially.
        // You might need more complex logic to handle more complex MultiPolygons.

        int numVertices = vertices.Count;

        for (int i = 1; i < numVertices - 1; i++)
        {
            indices.Add(0);
            indices.Add(i);
            indices.Add(i + 1);
        }

        return indices;
    }
}