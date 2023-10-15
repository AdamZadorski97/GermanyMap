using System;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq;
using MEC;
using DG.Tweening;
using UnityEngine.UI;
using Navigation = InfinityCode.OnlineMapsDemos.Navigation;

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
public enum MapType
{
    none,
    Plz1Stelig = 0,
    Plz2Stelig = 1,
    Plz3Stelig = 2,
    Plz5Stelig = 3,
    Kreise = 4,
    Regierungsbezirke = 5,
    Bundeslaender = 6
}
public class GeoJSONFileReader : MonoBehaviour
{
    public static GeoJSONFileReader Instance { get; private set; }


    const string PREFS_KEY_RELOADING_ENABLED = "_ReloadingEnabled";


    public bool isKreise = true;
    public bool isRegierungsbezirke = true;
    public bool isBundeslaender;
    public bool isPlz1Stelig = false;
    public bool isPlz2Stelig = false;
    public bool isPlz3Stelig = false;
    public bool isPlz5Stelig = false;

    public bool autoPlzZoom;





    public List<string> geoJSONFilePaths;
    public float scale = 100000;
    public ScriptableZoomRange zoomRange;
    public ScriptableMapTransformProperties mapTransformProperties;
    private List<GameObject> landPrefabsClones = new List<GameObject>();
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

    public InputField destinationInput;
    public List<GeoJSONFeature> GeoJsonFeaturesSearchable;
    public List<LandController> LandControllersSearch;
    public OnlineMaps map;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        SetupMapOnStart();
        SetupLandParentScaleAndPosition();
    }

    public void SetAutoPlzZoom()
    {
        autoPlzZoom = MapUserInterface.Instance.toggleAutoPlzZoom.isOn;
    }

    private void SetupMapOnStart()
    {
        int newElementIndex = GetCurrentElementIndex(OnlineMaps.zoom);

        if (isKreise)
            SetupMap(MapType.Kreise);
        else if (isRegierungsbezirke)
            SetupMap(MapType.Regierungsbezirke);
        else if (isBundeslaender)
            SetupMap(MapType.Bundeslaender);
        else
            LoadGeoJSON(geoJSONFilePaths[newElementIndex]);
    }





    public void SetupMap(MapType type)
    {
        ResetMapFlags();

        switch (type)
        {
            case MapType.Plz1Stelig:
                isPlz1Stelig = true;
                break;
            case MapType.Plz2Stelig:
                isPlz2Stelig = true;
                break;
            case MapType.Plz3Stelig:
                isPlz3Stelig = true;
                break;
            case MapType.Plz5Stelig:
                isPlz5Stelig = true;
                break;
            case MapType.Kreise:
                isKreise = true;
                break;
            case MapType.Regierungsbezirke:
                isRegierungsbezirke = true;
                break;
            case MapType.Bundeslaender:
                isBundeslaender = true;
                break;
        }

        LoadGeoJSON(geoJSONFilePaths[(int)type]);
        SwitchElement((int)type);
    }

    private void ResetMapFlags()
    {
        isKreise = false;
        isRegierungsbezirke = false;
        isBundeslaender = false;
        isPlz1Stelig = false;
        isPlz2Stelig = false;
        isPlz3Stelig = false;
        isPlz5Stelig = false;
    }

    void Update()
    {
        SetupLandParentScaleAndPosition();

        //float currentZoom = cameraController.currentZoom;
        int newElementIndex = GetCurrentElementIndex(OnlineMaps.zoom);

        if (newElementIndex != currentElementIndex && PlayerPrefs.GetInt(PREFS_KEY_RELOADING_ENABLED, 1) == 1)
        {
            currentElementIndex = newElementIndex;

            if (!isKreise && !isRegierungsbezirke && !isBundeslaender && autoPlzZoom)
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

            land.DOLocalMove(mapTransformProperties.zoomRanges[OnlineMaps.zoom].mapPositions, 0.2f);
            land.DOScale(mapTransformProperties.zoomRanges[OnlineMaps.zoom].mapScales, 0.2f);

            foreach(LandController landController in LandControllersSearch)
            {
                landController.lineRenderer.startWidth = mapTransformProperties.zoomRanges[OnlineMaps.zoom].linewidthMultiplier;
                landController.lineRenderer.endWidth = mapTransformProperties.zoomRanges[OnlineMaps.zoom].linewidthMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * mapTransformProperties.zoomRanges[OnlineMaps.zoom].textSizeMultiplier;
            }


        }
    }


    int GetCurrentElementIndex(float zoom)
    {
        for (int i = 0; i < zoomRange.zoomRanges.Count; i++)
        {
            if (zoom <= zoomRange.zoomRanges[i].minZoom && zoom >= zoomRange.zoomRanges[i].maxZoom)
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
                    if (!landPrefabsClones.Contains(land))
                        landPrefabsClones.Add(land);
                    land.name = "Polygon";
                    land.transform.localPosition = Vector3.zero;
                    land.transform.localScale = Vector3.one * 0.01666666f;
                    List<List<double[]>> polygonCoordinates =
                        ConvertJArrayToPolygonList((JArray)feature.geometry.coordinates);
                    UpdatePositionLists(polygonCoordinates, ref lineRendererPositions, ref innerMeshPositions);
                    RenderLandGeometry(land, lineRendererPositions, innerMeshPositions);
                    land.GetComponent<LandController>().coordinates = lineRendererPositions;
                    LandControllersSearch.Add(land.GetComponent<LandController>());
                    if (!isKreise && !isRegierungsbezirke && !isBundeslaender)
                    {
                        land.GetComponent<LandController>().plz = feature.properties.plz;
                    }
                    land.GetComponent<LandController>().SetupText();
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
                        land.GetComponent<LandController>().coordinates = lineRendererPositions;
                        LandControllersSearch.Add(land.GetComponent<LandController>());
                        if (!isKreise && !isRegierungsbezirke && !isBundeslaender)
                        {
                            land.GetComponent<LandController>().plz = feature.properties.plz;
                            land.GetComponent<LandController>().plz = feature.properties.plz;
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


    public List<LandController> FindAllByPLZ(List<LandController> features, string plz)
    {
        return features.Where(feature => feature.plz != null && feature.plz == plz).ToList();
    }

    public void SearchPlace()
    {
        string plzToSearch = destinationInput.text;

        List<LandController> matchingControllers = FindAllByPLZ(LandControllersSearch, plzToSearch);

        if (matchingControllers.Count > 0)
        {
            OnlineMaps.SetPosition(matchingControllers[0].lineRenderer.GetPosition(0).x / scale, matchingControllers[0].lineRenderer.GetPosition(0).z / scale);
            foreach (LandController controller in matchingControllers)
            {
               
                controller.Highlight();
            }
        }
        else
        {
            Debug.Log("No matching LandControllers found for PLZ: " + plzToSearch);
        }
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
    public AnimationCurve animationCurveVertical;
    public AnimationCurve animationCurveHorisontal;
    List<Vector3> ApplyAnimationCurvesToPositions(List<Vector3> positions, AnimationCurve curveX, AnimationCurve curveZ)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 position = positions[i];
            // Przemnóż pozycję x i z przez wartości z krzywych
            position.x *= curveX.Evaluate(position.x);
            position.z *= curveZ.Evaluate(position.z);
            // Przypisz przekształconą pozycję z powrotem do listy
            positions[i] = position;
        }
        return positions;
    }

    private void RenderLandGeometry(GameObject land, List<Vector3> lineRendererPositions, List<Vector3> innerMeshPositions)
    {
        LandController landController = land.GetComponent<LandController>();

        // Przemnóż pozycje lineRendererPositions i innerMeshPositions przy użyciu krzywych
        List<Vector3> newLineRendererPositions = ApplyAnimationCurvesToPositions(lineRendererPositions, animationCurveVertical, animationCurveHorisontal);
        List<Vector3> newInnerMeshPositions = ApplyAnimationCurvesToPositions(innerMeshPositions, animationCurveVertical, animationCurveHorisontal);

        landController.lineRenderer.positionCount = lineRendererPositions.Count;
        landController.lineRenderer.SetPositions(newLineRendererPositions.ToArray());

        int[] indices = TriangulatePolygons(newInnerMeshPositions);
        Mesh mesh = CreateMeshFromIndices(newInnerMeshPositions, indices);
        landController.meshFilter.mesh = mesh;

        MeshCollider meshCollider = land.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.transform.localPosition = Vector3.zero;
        land.transform.localPosition = Vector3.zero;
        landController.transform.localPosition = Vector3.zero;
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