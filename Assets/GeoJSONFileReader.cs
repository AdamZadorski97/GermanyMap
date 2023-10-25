using System;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Newtonsoft.Json.Linq;
using System.Linq;
using MEC;
using UnityEngine.UI;

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
    private CoroutineHandle zoomHandle;
    private int currentElementIndex = 0;
    private int currentZoom = 0;
    public GeoJSONData geoJSONData;
    public List<SerializableGeoDate> serializableGeoDate;

    public OnlineMaps OnlineMaps;

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
    public void SetActiveRenderers(bool state)
    {
        zoomHandle = Timing.RunCoroutine(ZoomWait(state));
    }

    private IEnumerator<float> ZoomWait(bool state)
    {
      yield return Timing.WaitForSeconds(0.01f);
        Transform land = ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0).GetChild(0).transform;
        land.gameObject.SetActive(state);

        if (state == true)
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





    public void SetupLandParentScaleAndPosition()
    {
        int newZoom = OnlineMaps.zoom;
        if (newZoom != currentZoom)
        {
            Transform land = ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0).GetChild(0).transform;
            currentZoom = newZoom;

            land.transform.localPosition = mapTransformProperties.zoomRanges[OnlineMaps.zoom].mapPositions;
            land.transform.localScale = mapTransformProperties.zoomRanges[OnlineMaps.zoom].mapScales;

            foreach (LandController landController in LandControllersSearch)
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
        foreach (GameObject land in landPrefabsClones)
        {

            ObjectPooler.Instance.ReturnObjectToPool(land);

        }
        landPrefabsClones.Clear();
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
            List<Vector3> lineRendererPositions = new List<Vector3>();
            List<Vector3> innerMeshPositions = new List<Vector3>();
            if (feature.geometry != null)
            {


                if (feature.geometry.type == "Polygon")
                {

                    GameObject land = ObjectPooler.Instance.GetPooledObject();
                    land.name = "Polygon";
                    List<List<double[]>> polygonCoordinates = GeometryUtilities.ConvertJArrayToPolygonList((JArray)feature.geometry.coordinates);
                    UpdatePositionLists(polygonCoordinates, ref lineRendererPositions, ref innerMeshPositions);
                    RenderLandGeometry(land, lineRendererPositions, innerMeshPositions);
                    SetupLand(land, feature);
                }
                else if (feature.geometry.type == "MultiPolygon")
                {
                    List<List<List<double[]>>> multiPolygonCoordinates = GeometryUtilities.ConvertJArrayToMultiPolygonList((JArray)feature.geometry.coordinates);

                    foreach (var polygon in multiPolygonCoordinates)
                    {
                        GameObject land = ObjectPooler.Instance.GetPooledObject();
                        land.name = "MultiPolygon";
                        lineRendererPositions.Clear();
                        innerMeshPositions.Clear();
                        UpdatePositionLists(polygon, ref lineRendererPositions, ref innerMeshPositions);
                        RenderLandGeometry(land, lineRendererPositions, innerMeshPositions);
                        SetupLand(land, feature);
                    }
                }
            }

            featuresProcessed++;
            if (featuresProcessed == 10)
            {
                yield return Timing.WaitForOneFrame; // Pause the method and continue from here in the next frame.
                featuresProcessed = 0;
            }
            processDone = true;
        }
        /*ObjectPooler.Instance.landParrent.transform.Rotate(0,0,180);*/
    }
    public void SetupLand(GameObject land, GeoJSONFeature feature)
    {
        LandController landController = land.GetComponent<LandController>();
        land.SetActive(true);
        land.transform.SetParent(ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0)
            .GetChild(0));
        if (!landPrefabsClones.Contains(land))
        {
            land.transform.localPosition = Vector3.zero;
            land.transform.localScale = Vector3.one * 0.01666666f;
            landPrefabsClones.Add(land);
        }

        landController.lineRenderer.startWidth = mapTransformProperties.zoomRanges[OnlineMaps.zoom].linewidthMultiplier;
        landController.lineRenderer.endWidth = mapTransformProperties.zoomRanges[OnlineMaps.zoom].linewidthMultiplier;
        landController.textMesh.transform.localScale = Vector3.one * mapTransformProperties.zoomRanges[OnlineMaps.zoom].textSizeMultiplier;
        LandControllersSearch.Add(landController);

        if (!isKreise && !isRegierungsbezirke && !isBundeslaender)
        {
            landController.plz = feature.properties.plz;
            landController.SetupText();
        }
        else
        {
            landController.textMesh.text = "";
        }
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
            OnlineMaps.SetPosition(matchingControllers[0].realCoordinates[0].x, matchingControllers[0].realCoordinates[0].z);
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
        landController.realCoordinates = lineRendererPositions;
        // Przemnóż pozycje lineRendererPositions i innerMeshPositions przy użyciu krzywych
        List<Vector3> newLineRendererPositions = ApplyAnimationCurvesToPositions(lineRendererPositions, mapTransformProperties.animationCurveVertical, mapTransformProperties.animationCurveHorisontal);
        List<Vector3> newInnerMeshPositions = ApplyAnimationCurvesToPositions(innerMeshPositions, mapTransformProperties.animationCurveVertical, mapTransformProperties.animationCurveHorisontal);
      
        landController.lineRenderer.positionCount = lineRendererPositions.Count;
        landController.lineRenderer.SetPositions(newLineRendererPositions.ToArray());

        int[] indices = GeometryUtilities.TriangulatePolygons(newInnerMeshPositions);
        Mesh mesh = GeometryUtilities.CreateMeshFromIndices(newInnerMeshPositions, indices);
        landController.meshFilter.mesh = mesh;

        MeshCollider meshCollider = land.AddComponent<MeshCollider>();
        landController.meshCollider = meshCollider;
        meshCollider.sharedMesh = mesh;
        meshCollider.transform.localPosition = Vector3.zero;
        land.transform.localPosition = Vector3.zero;
        landController.transform.localPosition = Vector3.zero;
    }








}