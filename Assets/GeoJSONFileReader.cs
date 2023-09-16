using System;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;

public class GeoJSONCoordinate
{
    public double[] coordinates { get; set; }
}

public class GeoJSONGeometry
{
    public string type { get; set; }
    public dynamic coordinates { get; set; }
}

public class GeoJSONData
{
    public string type { get; set; }
    public GeoJSONFeature[] features { get; set; }
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
    public float scale = 100000;
    public float simplify = 0.001f;
    public Vector3 pivotOffset = Vector3.zero;
    [SerializeField] private string geoJSONFilePath;
    [SerializeField] private GameObject landPrefab;

    private GeoJSONData geoJSONData;

    private void Start()
    {
        LoadGeoJSON(geoJSONFilePath);
        Draw2DMeshesFromLineRenderers();
    }

    [Button]
    public void LoadGeoJSON(string currentGeoJSONPath)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, currentGeoJSONPath);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            geoJSONData = JsonConvert.DeserializeObject<GeoJSONData>(json);

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
            foreach (GeoJSONFeature feature in geoJSONData.features)
            {
                if (feature.geometry != null)
                {
                    GameObject land = Instantiate(landPrefab);
                    List<Vector3> lineRendererPositions = new List<Vector3>();
                    List<Vector3> innerMeshPositions = new List<Vector3>();

                    if (feature.geometry.type == "Polygon")
                    {
                        List<List<double[]>> polygonCoordinates = ConvertJArrayToNestedList2((JArray)feature.geometry.coordinates);
                        foreach (var linearRing in polygonCoordinates)
                        {
                            foreach (var coordinate in linearRing)
                            {
                                if (coordinate.Length >= 2)
                                {
                                    Vector3 position = new Vector3(
                                        (float)coordinate[0] * scale,
                                        (float)coordinate[1] * scale,
                                        0f
                                    );
                                    lineRendererPositions.Add(position);
                                    innerMeshPositions.Add(position);
                                }
                            }
                        }
                    }
                    else if (feature.geometry.type == "MultiPolygon")
                    {
                        List<List<List<double[]>>> multiPolygonCoordinates = ConvertJArrayToNestedList((JArray)feature.geometry.coordinates);
                        foreach (var polygon in multiPolygonCoordinates)
                        {
                            foreach (var linearRing in polygon)
                            {
                                foreach (var coordinate in linearRing)
                                {
                                    if (coordinate.Length >= 2)
                                    {
                                        Vector3 position = new Vector3(
                                            (float)coordinate[0] * scale,
                                            (float)coordinate[1] * scale,
                                            0f
                                        );
                                        lineRendererPositions.Add(position);
                                        innerMeshPositions.Add(position);
                                    }
                                }
                            }
                        }
                    }

                    LineRenderer lineRenderer = land.GetComponent<LandController>().lineRenderer;
                    lineRenderer.positionCount = lineRendererPositions.Count;
                    lineRenderer.SetPositions(lineRendererPositions.ToArray());

                    int[] indices = TriangulatePolygons(innerMeshPositions);
                    Mesh mesh = CreateMeshFromIndices(innerMeshPositions, indices);

                    MeshFilter meshFilter = land.GetComponent<LandController>().meshFilter;
                    if (meshFilter != null)
                    {
                        meshFilter.mesh = mesh;
                    }

                    MeshCollider meshCollider = land.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = mesh;
                }
            }
        }
        else
        {
            Debug.LogError("No GeoJSON data or features found.");
        }
    }

    private Mesh CreateMeshFromIndices(List<Vector3> vertices, int[] indices)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private int[] TriangulatePolygons(List<Vector3> vertices)
    {
        Vector3[] verticesArray = vertices.ToArray();
        Triangulator tr = new Triangulator(verticesArray);
        int[] indices = tr.Triangulate();
        return indices;
    }

    List<List<List<double[]>>> ConvertJArrayToNestedList(JArray jArray)
    {
        var outerList = new List<List<List<double[]>>>();

        foreach (JArray firstLevel in jArray)
        {
            var middleList = new List<List<double[]>>();

            foreach (JArray secondLevel in firstLevel)
            {
                var innerList = new List<double[]>();

                foreach (JArray thirdLevel in secondLevel)
                {
                    innerList.Add(thirdLevel.ToObject<double[]>());
                }

                middleList.Add(innerList);
            }

            outerList.Add(middleList);
        }

        return outerList;
    }
    List<List<double[]>> ConvertJArrayToNestedList2(JArray jArray)
    {
        var outerList = new List<List<double[]>>();

        foreach (JArray firstLevel in jArray)
        {
            var innerList = new List<double[]>();

            foreach (JArray secondLevel in firstLevel)
            {
                innerList.Add(secondLevel.ToObject<double[]>());
            }

            outerList.Add(innerList);
        }

        return outerList;
    }

}