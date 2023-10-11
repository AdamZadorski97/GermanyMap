using System;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;
using System.Collections;
using InfinityCode.OnlineMapsDemos;
using MEC;

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

[Serializable]
public class ZoomRange
{
    public float minZoom;
    public float maxZoom;
    public int elementIndex;
}

[Serializable]
public class MapTransformsProperties
{
    public Vector3 mapPositions;
    public Vector3 mapScales;
}

public class GeoJSONFileReader : MonoBehaviour
{
    const string PREFS_KEY_RELOADING_ENABLED = "_ReloadingEnabled";

    public List<string> geoJSONFilePaths;
    public float scale = 100000;
    public Vector3 pivotOffset = Vector3.zero;
    public List<ZoomRange> zoomRanges;
    public List<GameObject> landPrefabsClones = new List<GameObject>();
    public List<LandController> landPrefabsControllersClones = new List<LandController>();
    [SerializeField] private string geoJSONFilePath;
    [SerializeField] private GameObject landPrefab;
    [SerializeField] private CameraController cameraController;
    private CoroutineHandle geoJSONProcessingHandle;
    private int currentElementIndex = 0;
    private int currentZoom = 0;
    private GeoJSONData geoJSONData;
    public List<SerializableGeoDate> serializableGeoDate;

    public OnlineMaps OnlineMaps;
    public OnlineMapsBuildings.Tile Tile;
    public Navigation Navigation;
    public List<MapTransformsProperties> mapTransformsProperties;

    void Start()
    {
        currentZoom = OnlineMaps.zoom;
        int newElementIndex = GetCurrentElementIndex(OnlineMaps.zoom);
      LoadGeoJSON(geoJSONFilePaths[newElementIndex]);
       // Draw2DMeshesFromLineRenderers();
        //ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0).GetChild(0).transform.Rotate(0,0,180);// ok 
    }

    void Update()
    {
        SetupLandParentScaleAndPosition();
        //float currentZoom = cameraController.currentZoom;
        int newElementIndex = GetCurrentElementIndex(OnlineMaps.zoom);

        if (newElementIndex != currentElementIndex && PlayerPrefs.GetInt(PREFS_KEY_RELOADING_ENABLED, 1) == 1)
        {
            currentElementIndex = newElementIndex;
            SwitchElement(currentElementIndex);
           
          
        }
    }



    public void SetupLandParentScaleAndPosition()
    {
        int newZoom = OnlineMaps.zoom;
        if (newZoom != currentZoom)
        {
 
         

            Transform land = ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0).GetChild(0).transform;
            currentZoom = newZoom;

            switch (OnlineMaps.zoom)
            {
                case 2:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                case 3:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                case 4:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                case 5:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                case 6:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                case 7:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                case 8:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;

                    break;
                case 9:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                case 10:
                    land.localPosition = mapTransformsProperties[OnlineMaps.zoom].mapPositions;
                    land.localScale = mapTransformsProperties[OnlineMaps.zoom].mapScales;
                    break;
                default:
                    Debug.Log("Nieobsługiwany zoom");
                    break;
            }
        }
    }


    int GetCurrentElementIndex(float zoom)
    {
        for (int i = 0; i < zoomRanges.Count; i++)
        {
            if (zoom <= zoomRanges[i].minZoom && zoom >= zoomRanges[i].maxZoom)
            {
                return i;
            }
        }

        return 0;
    }

    void SwitchElement(int index)
    {
        Timing.KillCoroutines(geoJSONProcessingHandle);
        foreach (var land in landPrefabsClones)
        {

            ObjectPooler.Instance.ReturnObjectToPool(land);
        }
        Debug.Log($"Switch Element {index}");
        LoadGeoJSON(geoJSONFilePaths[index]);
        Draw2DMeshesFromLineRenderers();
      //  SetupLandParentScaleAndPosition();
    }
    public bool jsonLoaded;

    [Button]
    public void LoadGeoJSON(string currentGeoJSONPath)
    {
        jsonLoaded = false;
        Debug.Log($"Load Json: {currentGeoJSONPath}");
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
        jsonLoaded = true;
    }

    [Button]
    public void Draw2DMeshesFromLineRenderers()
    {
        if (geoJSONData != null && geoJSONData.features != null)
        {
            geoJSONProcessingHandle = Timing.RunCoroutine(ProcessGeoJSONFeatures());
        }
        else
        {
            Debug.LogError("No GeoJSON data or features found.");
        }
    }

    private bool CheckParrent()
    {
        if (ObjectPooler.Instance.landParrent.childCount >= 1)
            return true;
        return false;
    }
    public bool processDone = true;
    IEnumerator<float> ProcessGeoJSONFeatures()
    {
        do
        {
            yield return Timing.WaitForOneFrame;
            Debug.Log("Process Waiting");
        }
        while (!processDone);

     


            processDone = false;
        while (!CheckParrent())
        {
            yield return Timing.WaitForOneFrame;
        }

        serializableGeoDate = GeoDataFileReader.geoDates;

        int featuresProcessed = 0;
        foreach (GeoJSONFeature feature in geoJSONData.features)
        {
            if (feature.geometry != null)
            {
                List<Vector3> lineRendererPositions = new List<Vector3>();
                List<Vector3> innerMeshPositions = new List<Vector3>();

                if (feature.geometry.type == "Polygon")
                {
                    GameObject land = ObjectPooler.Instance.GetPooledObject();
                    land.SetActive(true);
                    land.transform.SetParent(ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0)
                        .GetChild(0));
                    if(!landPrefabsClones.Contains(land))
                    landPrefabsClones.Add(land);
                    land.name = "Polygon";
                    land.transform.localPosition = Vector3.zero;
                    land.transform.localScale = Vector3.one * 0.01666666f;
                    List<List<double[]>> polygonCoordinates =
                        ConvertJArrayToPolygonList((JArray)feature.geometry.coordinates);
                    UpdatePositionLists(polygonCoordinates, ref lineRendererPositions, ref innerMeshPositions);
                    RenderLandGeometry(land, lineRendererPositions, innerMeshPositions);

                    foreach (var postCode in feature.properties.plz)
                    {
                        for (int i = 0; i <= serializableGeoDate[i].bundesland_nutscode.Length; i++)
                        {
                            foreach (var postalCode in serializableGeoDate[i].bundesland_nutscode)
                            {
                                if (postCode == postalCode) //failing here
                                {
                                    UpdateRegionsGeoData(land, serializableGeoDate, i);
                                }
                            }
                        }
                    }
                }
                else if (feature.geometry.type == "MultiPolygon")
                {
                    List<List<List<double[]>>> multiPolygonCoordinates =
                        ConvertJArrayToMultiPolygonList((JArray)feature.geometry.coordinates);
                    foreach (var polygon in multiPolygonCoordinates)
                    {
                        GameObject land = ObjectPooler.Instance.GetPooledObject();
                        land.SetActive(true);
                        land.transform.SetParent(ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0)
                            .GetChild(0));
                        land.name = "MultiPolygon";
                        if (!landPrefabsClones.Contains(land))
                            landPrefabsClones.Add(land);
                        land.transform.localPosition = Vector3.zero;
                        land.transform.localScale = Vector3.one * 0.01666666f;
                        lineRendererPositions.Clear();
                        innerMeshPositions.Clear();
                        UpdatePositionLists(polygon, ref lineRendererPositions, ref innerMeshPositions);
                        RenderLandGeometry(land, lineRendererPositions, innerMeshPositions);


                        foreach (var postCode in feature.properties.plz)
                        {
                            for (int i = 0; i <= serializableGeoDate[i].bundesland_nutscode.Length; i++)
                            {
                                foreach (var postalCode in serializableGeoDate[i].bundesland_nutscode)
                                {
                                    if (postCode == postalCode) //failing here
                                    {
                                        UpdateRegionsGeoData(land, serializableGeoDate, i);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            featuresProcessed++;
            if (featuresProcessed == 5)
            {
                yield return Timing.WaitForOneFrame; // Pause the method and continue from here in the next frame.
                featuresProcessed = 0;
            }
            processDone = true;
        }
        /*ObjectPooler.Instance.landParrent.transform.Rotate(0,0,180);*/
    }

    private void UpdateRegionsGeoData(GameObject land, List<SerializableGeoDate> aSerializableGeoDate, int number)
    {
        LandController landController = land.GetComponent<LandController>();
        if (landController.plz == aSerializableGeoDate[number].bundesland_nutscode)
        {
            landController.plz = aSerializableGeoDate[number].bundesland_nutscode;
            landController.serializableGeoDate = aSerializableGeoDate[number];
            //rest of data
        }
        else
        {
            Debug.Log("postal code match failed " + landController.plz);
        }
    }

    private void UpdatePositionLists(List<List<double[]>> coordinates, ref List<Vector3> lineRendererPositions,
        ref List<Vector3> innerMeshPositions)
    {
        foreach (var linearRing in coordinates)
        {
            foreach (var coordinate in linearRing)
            {
                if (coordinate.Length >= 2)
                {
                    // Swapped latitude and longitude for Unity's XY plane.
                    Vector3 position = new Vector3(
                        (float)coordinate[0] * scale,
                        0f,
                        (float)coordinate[1] * scale
                    );

                    if (lineRendererPositions.Contains(position))
                    {
                        lineRendererPositions.Add(position);
                        innerMeshPositions.Add(position);
                        return;
                    }
                    else
                    {
                        lineRendererPositions.Add(position);
                        innerMeshPositions.Add(position);
                    }
                }
            }
        } 
    }

    private void RenderLandGeometry(GameObject land, List<Vector3> lineRendererPositions, List<Vector3> innerMeshPositions)
    {
        LandController landController = land.GetComponent<LandController>();
        landController.lineRenderer.positionCount = lineRendererPositions.Count;
        landController.lineRenderer.SetPositions(lineRendererPositions.ToArray());

        int[] indices = TriangulatePolygons(innerMeshPositions);
        Mesh mesh = CreateMeshFromIndices(innerMeshPositions, indices);
        landController.meshFilter.mesh = mesh;

        MeshCollider meshCollider = land.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.transform.localPosition = Vector3.zero;
        land.transform.localPosition = Vector3.zero;
        landController.transform.localPosition = Vector3.zero;

        //meshCollider.transform.Rotate(180, 0, 180);
        //land.transform.Rotate(180, 0, 180);
        //land.transform.position = OnlineMaps.position;
        //land.transform.position = OnlineMaps.marker3DManager.container.transform.position;
        //Debug.Log("xyz" + OnlineMaps.markerManager.transform.position);
        //Debug.Log("xyzzzz" + OnlineMaps.marker3DManager.container.transform.position);
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

    private List<List<List<double[]>>> ConvertJArrayToMultiPolygonList(JArray jArray)
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

    private List<List<double[]>> ConvertJArrayToPolygonList(JArray jArray)
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