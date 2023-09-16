using System;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using System.Linq;
using Unity.VisualScripting;

public class GeoJSONCoordinate
{
    public double[] coordinates { get; set; }
}

public class GeoJSONGeometry
{
    public string type { get; set; }
    public List<List<GeoJSONCoordinate>> coordinates { get; set; }  // For Polygons
    public List<List<List<GeoJSONCoordinate>>> multiCoordinates { get; set; }  // For MultiPolygons
}

public class GeoJSONData
{
    public string type { get; set; }
    public GeoJSONFeature[] features { get; set; }
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
    public List<string> geoJSONFilePaths;
    public float scale = 100000; // Adjust the scale factor as needed
    public float simplify = 0.001f;

    public Vector3 pivotOffset = Vector3.zero; // Offset for adjusting the mesh pivot

    [SerializeField] private CameraController _cameraController;
    [SerializeField] private string geoJSONFilePath;
    [SerializeField] private GameObject landPrefab;

    private GeoJSONData geoJSONData;

    private void Start()
    {
        LoadGeoJSON(geoJSONFilePath);
        Draw2DMeshesFromLineRenderers();
    }

    private void AdjustGeoMapping()
    {
        float zoomScope = _cameraController.zoomOutMax - _cameraController.zoomOutMin;
        int zoomStages = (int)zoomScope % geoJSONFilePaths.Count;

        if (_cameraController.currentZoom >= 150)
        {
            LoadGeoJSON(geoJSONFilePaths[3]);
            Draw2DMeshesFromLineRenderers();
            return;
        }

        if (_cameraController.currentZoom >= 100 && _cameraController.currentZoom < 150)
        {
            LoadGeoJSON(geoJSONFilePaths[2]);
            Draw2DMeshesFromLineRenderers();
            return;
        }

        if (_cameraController.currentZoom >= 50 && _cameraController.currentZoom < 100)
        {
            LoadGeoJSON(geoJSONFilePaths[1]);
            Draw2DMeshesFromLineRenderers();
            return;
        }

        if (_cameraController.currentZoom >= _cameraController.zoomOutMin && _cameraController.currentZoom < 50)
        {
            LoadGeoJSON(geoJSONFilePaths[0]);
            Draw2DMeshesFromLineRenderers();
            return;
        }
    }

    [Button]
    public void LoadGeoJSON(string currentGeoJSONPath)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, currentGeoJSONPath);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            // Create settings for JSON deserialization with the custom converter
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new GeoJSONCoordinateConverter() }
            };

            // Deserialize using JSON.NET with the custom converter
            geoJSONData = JsonConvert.DeserializeObject<GeoJSONData>(json, settings);

            if (geoJSONData != null)
            {
                Debug.Log("GeoJSON data parsed successfully.");
            }
            else
            {
                Debug.LogError("Failed to parse GeoJSON data.");
            }
        }
    }

    [Button]
    public void Draw2DMeshesFromLineRenderers()
    {
        if (geoJSONData != null && geoJSONData.features != null)
        {
            int index = 0;
            foreach (GeoJSONFeature feature in geoJSONData.features)
            {
                if (feature.geometry != null && feature.geometry.coordinates != null)
                {
                    GameObject land = Instantiate(landPrefab);
                    land.gameObject.name += feature.geometry.type;
                    List<Vector3> lineRendererPositions = new List<Vector3>();
                    List<Vector3> innerMeshPositions = new List<Vector3>();

                    if (feature.geometry.type == "Polygon")
                    {
                        foreach (var ring in feature.geometry.coordinates[0])  // We access the first ring directly here
                        {
                            var coordinate = ring;
                            if (coordinate.coordinates.Length >= 2)
                            {
                                Vector3 position = new Vector3(
                                    (float)coordinate.coordinates[1] * scale,
                                    (float)coordinate.coordinates[0] * scale,
                                    0f
                                );
                                lineRendererPositions.Add(position);
                                innerMeshPositions.Add(position);
                            }
                        }
                    }
                    else if (feature.geometry.type == "MultiPolygon")
                    {
                        foreach (var polygon in feature.geometry.coordinates)
                        {
                            foreach (var geoCoord in polygon)
                            {
                                double[] coordinate = geoCoord.coordinates;
                                if (coordinate.Length >= 2)
                                {
                                    Vector3 position = new Vector3(
                                        (float)coordinate[1] * scale,  // Latitude
                                        (float)coordinate[0] * scale,  // Longitude
                                        0f
                                    );
                                    lineRendererPositions.Add(position);
                                    innerMeshPositions.Add(position);
                                }
                            }

                            // Close the loop for the LineRenderer, if needed
                            if (lineRendererPositions.Count > 0 &&
                                lineRendererPositions[0] != lineRendererPositions[lineRendererPositions.Count - 1])
                            {
                                lineRendererPositions.Add(lineRendererPositions[0]);
                                innerMeshPositions.Add(innerMeshPositions[0]);
                            }
                        }
                    }
                    LandController landController = land.GetComponent<LandController>();
                    landController.plz = feature.properties.plz;
                    landController.note = feature.properties.note;
                    landController.einwohner = feature.properties.einwohner;
                    landController.qkm = feature.properties.qkm;

                    // Create the outer LineRenderer from the LineRenderer positions
                    LineRenderer lineRenderer = landController.lineRenderer;
                    lineRenderer.positionCount = lineRendererPositions.Count;
                    lineRenderer.SetPositions(lineRendererPositions.ToArray());

                    // Triangulate the inner mesh and create a 2D mesh
                    int[] indices = TriangulatePolygons(innerMeshPositions);
                    Vector3 center = CalculateMeshCenter(innerMeshPositions);

                    // Create the mesh with the adjusted pivot point
                    Mesh mesh = CreateMeshFromIndices(innerMeshPositions, indices, center + pivotOffset);

                    // Assign the mesh to the GameObject
                    MeshFilter meshFilter = landController.meshFilter;
                    if (meshFilter != null)
                    {
                        meshFilter.sharedMesh = mesh;
                    }

                    MeshCollider meshCollider = land.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = mesh;

                    // Move the GameObject to the center position
                    land.transform.position = center;

                    TextMesh textMesh = landController.textMesh;

                    // Set the text of the TextMesh to the GameObject's name
                    if (textMesh != null)
                    {
                        textMesh.text = index.ToString();
                    }

                    // Increment the index for naming purposes
                    index++;
                }
            }
        }
        else
        {
            Debug.LogError("No GeoJSON data or features found.");
        }
    }

    private Mesh CreateMeshFromIndices(List<Vector3> vertices, int[] indices, Vector3 pivot)
    {
        Mesh mesh = new Mesh();

        // Calculate the offset to adjust the pivot correctly
        Vector3 offset = -pivot;

        // Set vertices
        mesh.vertices = vertices.Select(v => v + offset).ToArray();

        // Set triangles (indices)
        mesh.triangles = indices;

        // Recalculate normals to ensure proper shading
        mesh.RecalculateNormals();

        // Recalculate bounds for correct culling
        mesh.RecalculateBounds();

        return mesh;
    }

    private Vector3 CalculateMeshCenter(List<Vector3> vertices)
    {
        if (vertices.Count <= 0)
        {
            return Vector3.zero;
        }

        Vector3 min = vertices[0];
        Vector3 max = vertices[0];

        foreach (Vector3 vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        return (min + max) / 2f;
    }

    private int[] TriangulatePolygons(List<Vector3> vertices)
    {
        // Convert List<Vector3> to Vector3[]
        Vector3[] verticesArray = vertices.ToArray();

        // Use the Triangulator class to perform triangulation
        Triangulator tr = new Triangulator(verticesArray);
        int[] indices = tr.Triangulate();

        return indices;
    }

    private void ExtractCoordinatesForRendering(List<GeoJSONCoordinate> ring, List<Vector3> lineRendererPositions, List<Vector3> innerMeshPositions)
    {
        for (int i = 0; i < ring.Count; i++)
        {
            var coordinate = ring[i];
            if (coordinate.coordinates.Length >= 2)
            {
                Vector3 position = new Vector3(
                    (float)coordinate.coordinates[1] * scale, // Scale the latitude
                    (float)coordinate.coordinates[0] * scale, // Scale the longitude
                    0f
                );
                lineRendererPositions.Add(position);
                innerMeshPositions.Add(position);
            }
        }
    }
}