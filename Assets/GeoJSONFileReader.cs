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
using DG.Tweening;

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

    public int ID_0 { get; set; }
    public string ISO { get; set; }
    public string name { get; set; }
    public string NAME_0 { get; set; }
    public int ID_1 { get; set; }
    public string NAME_1 { get; set; }
    public int ID_2 { get; set; }
    public string NAME_2 { get; set; }
    public int ID_3 { get; set; }
    public string NAME_3 { get; set; }
    public string NL_NAME_3 { get; set; }
    public string VARNAME_3 { get; set; }
    public string TYPE_3 { get; set; }
    public string ENGTYPE_3 { get; set; }
}


public enum MapType
{
    none = 0,
    Plz1Stelig = 1,
    Plz2Stelig = 2,
    Plz3Stelig = 3,
    Plz5Stelig = 4,
    Kreise = 5,
    Regierungsbezirke = 6,
    Bundeslaender = 7
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
    public ScriptableMapTransformProperties mapTransformProperties;
    private List<GameObject> landPrefabsClones = new List<GameObject>();
    [SerializeField] private string geoJSONFilePath;
    [SerializeField] private GameObject landPrefab;
    [SerializeField] private CameraController cameraController;
    private CoroutineHandle geoJSONProcessingHandle;
    private CoroutineHandle zoomHandle;
    private int currentElementIndex = 0;
    public int currentZoom = 0;
    public GeoJSONData geoJSONData;
    public List<SerializableGeoDate> serializableGeoDate;

    public OnlineMaps OnlineMaps;

    public InputField destinationInput;
    public List<GeoJSONFeature> GeoJsonFeaturesSearchable;
    public List<LandController> LandControllersSearch;
    public OnlineMaps map;
    public MapType currentType;

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
    public void SetActiveRenderers(int value)
    {
        zoomHandle = Timing.RunCoroutine(ZoomWait(value));
    }

    private IEnumerator<float> ZoomWait(int value)
    {
      //  yield return Timing.WaitForOneFrame;
        Transform land = ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).transform;

        if (value == -1)
        {
            if(currentZoom > OnlineMaps.MINZOOM)
            currentZoom--;
        }
        if (value == 1)
        {
            if (currentZoom < OnlineMaps.MAXZOOM)
                currentZoom++;
        }
        
        SetupLandParentScaleAndPosition();
        land.gameObject.SetActive(false);
        yield return Timing.WaitForSeconds(0.1f);
        land.gameObject.SetActive(true);
        //float currentZoom = cameraController.currentZoom;
    }




    public void SetAutoPlzZoom()
    {
        autoPlzZoom = MapUserInterface.Instance.toggleAutoPlzZoom.isOn;
    }

    private void SetupMapOnStart()
    {
        currentZoom = OnlineMaps.zoom;
        if (isKreise)
            SetupMap(MapType.Kreise);
        else if (isRegierungsbezirke)
            SetupMap(MapType.Regierungsbezirke);
        else if (isBundeslaender)
            SetupMap(MapType.Bundeslaender);
        else
            SetupMap(MapType.Plz1Stelig);
    }

    public void SetupMap(MapType type)
    {
        if (currentType == type)
            return;
        currentType = type;

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
        Debug.Log($"Setup {type} Zoom: {OnlineMaps.zoom}");
        LoadGeoJSON(geoJSONFilePaths[(int)type - 1]);
        SwitchElement((int)type - 1);
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


    public MapTransformProperties GetMapProperties(int zoom)
    {
        foreach(MapTransformProperties properties in mapTransformProperties.zoomRanges)
        {
            if(properties.zoom == zoom)
                return properties;
        }
        return null;
    }


    public void SetupLandParentScaleAndPosition()
    {

        Transform land = ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0).GetChild(0).transform;

        MapTransformProperties zoomProperties = GetMapProperties(currentZoom);

        land.transform.DOLocalMove(zoomProperties.mapPositions, 0.1f);
        land.transform.DOScale(zoomProperties.mapScales, 0.1f);



        foreach (LandController landController in LandControllersSearch)
        {
            landController.lineRenderer.startWidth = zoomProperties.linewidthMultiplier;
            landController.lineRenderer.endWidth = zoomProperties.linewidthMultiplier;
            landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
        }


    }


    

    void SwitchElement(int index)
    {
        Timing.KillCoroutines(geoJSONProcessingHandle);
        foreach (GameObject land in landPrefabsClones)
        {

            ObjectPooler.Instance.ReturnObjectToPool(land);

        }
        landPrefabsClones.Clear();
        //Debug.Log($"Switch Element {index}");
        LoadGeoJSON(geoJSONFilePaths[index]);
        Draw2DMeshesFromLineRenderers();
        //  SetupLandParentScaleAndPosition();
    }
    public bool jsonLoaded;

    [Button]

    public void LoadGeoJSON(string currentGeoJSONPath)
    {

        jsonLoaded = false;
        // Debug.Log($"Load Json: {currentGeoJSONPath}");
        string filePath = Path.Combine(Application.streamingAssetsPath, currentGeoJSONPath);
        if (File.Exists(filePath))
        {

            string json = File.ReadAllText(filePath);
            geoJSONData = JsonConvert.DeserializeObject<GeoJSONData>(json);

            if (geoJSONData != null)
            {
                // Debug.Log("GeoJSON data parsed successfully.");
            }
            else
            {
                //    Debug.LogError("Failed to parse GeoJSON data.");
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


        int minFeaturesToProcess = 0;
        if (currentType == MapType.Plz1Stelig)
        {
            minFeaturesToProcess = 1;
        }
        if (currentType == MapType.Plz2Stelig)
        {
            minFeaturesToProcess = 2;
        }
        if (currentType == MapType.Plz3Stelig)
        {
            minFeaturesToProcess = 5;
        }
        if (currentType == MapType.Plz5Stelig)
        {
            minFeaturesToProcess = 5;
        }
        if (currentType == MapType.Kreise)
        {
            minFeaturesToProcess = 10;
        }
        if (currentType == MapType.Bundeslaender)
        {
            minFeaturesToProcess = 2;
        }
        if (currentType == MapType.Regierungsbezirke)
        {
            minFeaturesToProcess = 2;
        }


        int featuresProcessed = 0;
        foreach (GeoJSONFeature feature in geoJSONData.features)
        {
            List<Vector3> lineRendererPositions = new List<Vector3>();
            List<Vector3> innerMeshPositions = new List<Vector3>();

            List<Vector3> geographicalPositions = new List<Vector3>();
            List<Vector3> innerGeographicalPositions = new List<Vector3>();

            if (feature.geometry != null)
            {


                if (feature.geometry.type == "Polygon")
                {
                    GameObject land = ObjectPooler.Instance.GetPooledObject();
                    land.name = "Polygon";
                    List<List<double[]>> polygonCoordinates = GeometryUtilities.ConvertJArrayToPolygonList((JArray)feature.geometry.coordinates);
                    UpdatePositionLists(polygonCoordinates, ref lineRendererPositions, ref geographicalPositions, ref innerMeshPositions, ref innerGeographicalPositions);
                    RenderLandGeometry(land, lineRendererPositions, innerMeshPositions, geographicalPositions, innerGeographicalPositions);
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
                        UpdatePositionLists(polygon, ref lineRendererPositions, ref geographicalPositions, ref innerMeshPositions, ref innerGeographicalPositions);
                        RenderLandGeometry(land, lineRendererPositions, innerMeshPositions, geographicalPositions, innerGeographicalPositions);
                        SetupLand(land, feature);
                    }
                }
            }

            featuresProcessed++;




            if (featuresProcessed == minFeaturesToProcess)
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
            .GetChild(0).GetChild(0));
        if (!landPrefabsClones.Contains(land))
        {
            land.transform.localPosition = Vector3.zero;
            land.transform.localScale = Vector3.one * 0.01666666f;
            landPrefabsClones.Add(land);
        }

        landController.lineRenderer.startWidth = mapTransformProperties.zoomRanges[currentZoom].linewidthMultiplier;
        landController.lineRenderer.endWidth = mapTransformProperties.zoomRanges[currentZoom].linewidthMultiplier;
        landController.textMesh.transform.localScale = Vector3.one * mapTransformProperties.zoomRanges[currentZoom].textSizeMultiplier;
        LandControllersSearch.Add(landController);


        if (currentType == MapType.Plz1Stelig || currentType == MapType.Plz2Stelig || currentType == MapType.Plz3Stelig || currentType == MapType.Plz5Stelig)
        {
            landController.plz = feature.properties.plz;
            landController.SetupText(feature.properties.plz);
        }
        if (currentType == MapType.Kreise)
        {
            landController.NAME_3 = feature.properties.NAME_3;
            landController.SetupText(feature.properties.NAME_3);
        }
        if (currentType == MapType.Bundeslaender)
        {
            landController.name = feature.properties.name;
            landController.SetupText(feature.properties.name);
        }
        if (currentType == MapType.Regierungsbezirke)
        {
            landController.NAME_2 = feature.properties.NAME_2;
            landController.SetupText(feature.properties.NAME_2);
        }



    }

    public List<LandController> FindAllByPLZ(List<LandController> features, string plz)
    {
        return features.Where(feature => feature.plz != null && feature.plz == plz).ToList();
    }

    public List<LandController> FindAllByKreise(List<LandController> features, string kreise)
    {
        return features.Where(feature => feature.NAME_3.ToLower() != null && feature.NAME_3.ToLower() == kreise.ToLower()).ToList();
    }
    public List<LandController> FindAllByBundesLander(List<LandController> features, string bundesLander)
    {
        return features.Where(feature => feature.name.ToLower() != null && feature.name.ToLower() == bundesLander.ToLower()).ToList();
    }
    public List<LandController> FindAllByRegierungsbezirke(List<LandController> features, string regierungsbezirke)
    {
        return features.Where(feature => feature.NAME_2.ToLower() != null && feature.NAME_2.ToLower() == regierungsbezirke.ToLower()).ToList();
    }


    public void SearchPlace()
    {
        string searchText = destinationInput.text;
        int zoom = 0; ;
        List<LandController> matchingControllers = new List<LandController>();

        if (currentType == MapType.Plz1Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 6;
        }
        if (currentType == MapType.Plz2Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 7;
        }
        if (currentType == MapType.Plz3Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 8;
        }
        if (currentType == MapType.Plz5Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 10;
        }
        if (currentType == MapType.Bundeslaender)
        {
            matchingControllers = FindAllByBundesLander(LandControllersSearch, searchText);
            zoom = 6;
        }
        if (currentType == MapType.Kreise)
        {
            matchingControllers = FindAllByKreise(LandControllersSearch, searchText);
            zoom = 6;
        }
        if (currentType == MapType.Regierungsbezirke)
        {
            matchingControllers = FindAllByRegierungsbezirke(LandControllersSearch, searchText);
            zoom = 6;
        }




        if (matchingControllers.Count > 0)
        {
            OnlineMaps.SetPositionAndZoom(matchingControllers[0].realCoordinatesToCentering[0].x, matchingControllers[0].realCoordinatesToCentering[0].z, zoom);
            SetupLandParentScaleAndPosition();
            foreach (LandController controller in matchingControllers)
            {

                controller.Highlight();
            }
        }
        else
        {
            Debug.Log("No matching LandControllers found for PLZ: " + searchText);
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

    private void UpdatePositionLists(List<List<double[]>> coordinates, ref List<Vector3> lineRendererPositions, ref List<Vector3> geographicalPositions,
        ref List<Vector3> innerMeshPositions, ref List<Vector3> innerGeographicalPositions)
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
                    Vector3 geographicPositions = new Vector3(
                        (float)coordinate[0],
                        0f,
                        (float)coordinate[1]
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

                    if (geographicalPositions.Contains(geographicPositions))
                    {
                        geographicalPositions.Add(geographicPositions);
                        innerGeographicalPositions.Add(geographicPositions);
                        return;
                    }
                    else
                    {
                        geographicalPositions.Add(geographicPositions);
                        innerGeographicalPositions.Add(geographicPositions);
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

    private void RenderLandGeometry(GameObject land, List<Vector3> lineRendererPositions, List<Vector3> innerMeshPositions, List<Vector3> geographicalPositions, List<Vector3> innerGeographicalPositions)
    {
        LandController landController = land.GetComponent<LandController>();
        landController.realCoordinates = lineRendererPositions;
        landController.realCoordinatesToCentering = geographicalPositions;
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