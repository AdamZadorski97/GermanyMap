using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using System.Linq;

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
    [SerializeField]
    public string geoJSONFilePath;

    private GeoJSONData geoJSONData;

    [SerializeField]
    private GameObject landPrefab;

    public float scale = 100000; // Adjust the scale factor as needed
    public float simplify = 0.001f;

    public Vector3 pivotOffset = Vector3.zero; // Offset for adjusting the mesh pivot

    [Button]
    public void LoadGeoJSON()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, geoJSONFilePath);

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
                    // Create a new GameObject for each feature
                    GameObject land = Instantiate(landPrefab);

                    // Extract coordinates for the LineRenderer and the inner mesh
                    List<Vector3> lineRendererPositions = new List<Vector3>();
                    List<Vector3> innerMeshPositions = new List<Vector3>();

                    foreach (var polygon in feature.geometry.coordinates)
                    {
                        foreach (var ring in polygon)
                        {
                            for (int i = 0; i < ring.Count; i++)
                            {
                                var coordinate = ring[i];
                                if (coordinate.coordinates.Length >= 2)
                                {
                                    // Assuming your coordinates represent longitude (x), latitude (y), and z as 0
                                    Vector3 position = new Vector3(
                                        (float)coordinate.coordinates[0] * scale, // Scale the longitude
                                        (float)coordinate.coordinates[1] * scale, // Scale the latitude
                                        0f
                                    );
                                    lineRendererPositions.Add(position);
                                    innerMeshPositions.Add(position);
                                }
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
}