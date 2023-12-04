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
using System.Collections;
using TMPro;

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
    public int currentZoom = 0;
    public GeoJSONData geoJSONData;
    public List<SerializableGeoDate> serializableGeoDate;

    public OnlineMaps OnlineMaps;

    public InputField destinationInput;
    public List<LandController> LandControllersSearch;
    public OnlineMaps map;
    public GeoDataFileReader geoDataFileReader;
    public MapType currentType;
    public Dictionary<string, List<LandController>> identifierToLandControllersMap;
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
        yield return Timing.WaitForOneFrame;
        Transform land = ObjectPooler.Instance.landParrent.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).transform;
        {
            if (value == -1)
            {
                if (currentZoom > OnlineMaps.MINZOOM)
                    currentZoom--;
            }
            if (value == 1)
            {
                if (currentZoom <= OnlineMaps.MAXZOOM)
                    currentZoom++;
            }
        }


        SetupLandParentScaleAndPosition();
        if (processDone)
        {
            land.gameObject.SetActive(false);
            yield return Timing.WaitForSeconds(0.1f);
            land.gameObject.SetActive(true);
        }
        yield return Timing.WaitForSeconds(1f);
        GetComponent<ZoomController>().OnZoomChange();

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


    public bool mapSwitch;
    public void SetupMap(MapType type)
    {
        StartCoroutine(SetupMapCoroutine(type));
    }

    IEnumerator SetupMapCoroutine(MapType type)
    {
        if (currentType == type) yield break;

        yield return new WaitForSeconds(0.25f);

        currentType = type;



        switch (type)
        {
            case MapType.Plz1Stelig:
                isPlz1Stelig = true;
                destinationInput.text = null;
                destinationInput.placeholder.GetComponent<Text>().text = "Find plz 1 stellig";
                break;
            case MapType.Plz2Stelig:
                isPlz2Stelig = true;
                destinationInput.text = null;
                destinationInput.placeholder.GetComponent<Text>().text = "Find plz 2 stellig";
                break;
            case MapType.Plz3Stelig:
                isPlz3Stelig = true;
                destinationInput.text = null;
                destinationInput.placeholder.GetComponent<Text>().text = "Find plz 3 stellig";
                break;
            case MapType.Plz5Stelig:
                isPlz5Stelig = true;
                destinationInput.text = null;
                destinationInput.placeholder.GetComponent<Text>().text = "Find plz 5 stellig";
                break;
            case MapType.Kreise:
                isKreise = true;
                destinationInput.text = null;
                destinationInput.placeholder.GetComponent<Text>().text = "Find kreise";
                break;
            case MapType.Regierungsbezirke:
                isRegierungsbezirke = true;
                destinationInput.text = null;
                destinationInput.placeholder.GetComponent<Text>().text = "Find regierungsbezirke";
                break;
            case MapType.Bundeslaender:
                isBundeslaender = true;
                destinationInput.text = null;
                destinationInput.placeholder.GetComponent<Text>().text = "Find bundeslaender";
                break;
        }
        LoadGeoJSON(geoJSONFilePaths[(int)type - 1]);
        SwitchElement((int)type - 1);
    }





    public MapTransformProperties GetMapProperties(int zoom)
    {
        foreach (MapTransformProperties properties in mapTransformProperties.mapTransformProperties)
        {
            if (properties.zoom == zoom)
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
            if (currentType == MapType.Plz5Stelig)
            {
                if (currentZoom < 10)
                {
                    landController.textIntialScale = Vector3.one * 0;
                    landController.textMesh.transform.localScale = Vector3.one * 0;
                }
                else
                {
                    landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                    landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
                }

            }

            if (currentType == MapType.Plz3Stelig)
            {
                if (currentZoom < 9)
                {
                    landController.textIntialScale = Vector3.one * 0;
                    landController.textMesh.transform.localScale = Vector3.one * 0;
                }
                else
                {
                    landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                    landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
                }

            }

            if (currentType == MapType.Plz2Stelig)
            {
                if (currentZoom < 8)
                {
                    landController.textIntialScale = Vector3.one * 0;
                    landController.textMesh.transform.localScale = Vector3.one * 0;
                }
                else
                {
                    landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                    landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
                }
            }

            if (currentType == MapType.Plz1Stelig)
            {
                landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
            }


            if (currentType == MapType.Kreise)
            {
                if (currentZoom < 9)
                {
                    landController.textIntialScale = Vector3.one * 0;
                    landController.textMesh.transform.localScale = Vector3.one * 0;
                }
                else
                {
                    landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                    landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
                }

            }

            if (currentType == MapType.Bundeslaender)
            {

                landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
            }

            if (currentType == MapType.Regierungsbezirke)
            {

                landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
            }
        }
    }

    void SwitchElement(int index)
    {
        if (geoJSONProcessingHandle != null)
        {
            Timing.KillCoroutines(geoJSONProcessingHandle);
            processDone = true;
        }

        foreach (GameObject land in landPrefabsClones)
        {

            ObjectPooler.Instance.ReturnObjectToPool(land);

        }
        landPrefabsClones.Clear();
        LandControllersSearch.Clear();
        LoadGeoJSON(geoJSONFilePaths[index]);
        Draw2DMeshesFromLineRenderers();
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
        while (!processDone)
        {
            yield return Timing.WaitForOneFrame;
            Debug.Log("Process Waiting");
        }

        processDone = false;
        while (!CheckParrent())
        {
            yield return Timing.WaitForOneFrame;
        }



        int minFeaturesToProcess = 0;
        if (currentType == MapType.Plz1Stelig)
        {
            minFeaturesToProcess = 1;
        }
        if (currentType == MapType.Plz2Stelig)
        {
            minFeaturesToProcess = 1;
        }
        if (currentType == MapType.Plz3Stelig)
        {
            minFeaturesToProcess = 3;
        }
        if (currentType == MapType.Plz5Stelig)
        {
            minFeaturesToProcess = 8;
        }
        if (currentType == MapType.Kreise)
        {
            minFeaturesToProcess = 1;
        }
        if (currentType == MapType.Bundeslaender)
        {
            minFeaturesToProcess = 1;
        }
        if (currentType == MapType.Regierungsbezirke)
        {
            minFeaturesToProcess = 1;
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

                    List<List<double[]>> polygonCoordinates = GeometryUtilities.ConvertJArrayToPolygonList((JArray)feature.geometry.coordinates);

                    // if (polygonCoordinates[0].Count < 1) continue;


                    UpdatePositionLists(polygonCoordinates, ref lineRendererPositions, ref geographicalPositions, ref innerMeshPositions, ref innerGeographicalPositions);

                    if (lineRendererPositions.Count < 15)
                    {
                        continue; // Skip this polygon in the MultiPolygon
                    }
                    GameObject land = ObjectPooler.Instance.GetPooledObject();
                    land.name = "Polygon";

                    RenderLandGeometry(land, lineRendererPositions, geographicalPositions);
                    SetupLand(land, feature);
                }
                else if (feature.geometry.type == "MultiPolygon")
                {
                    List<List<List<double[]>>> multiPolygonCoordinates = GeometryUtilities.ConvertJArrayToMultiPolygonList((JArray)feature.geometry.coordinates);

                    foreach (var polygon in multiPolygonCoordinates) // Iterate through each Polygon in MultiPolygon
                    {
                        foreach (var linearRing in polygon) // Iterate through each LinearRing in Polygon
                        {
                            lineRendererPositions.Clear();
                            innerMeshPositions.Clear();

                            // Process each LinearRing
                            if (currentType == MapType.Kreise)
                            {
                                foreach (var coordinate in linearRing) // Iterate through each coordinate in LinearRing
                                {
                                    Vector3 position = new Vector3(
                                        (float)coordinate[0] * scale, // Longitude
                                        0f,                           // Y-coordinate (assuming flat map)
                                        (float)coordinate[1] * scale  // Latitude
                                    );
                                    lineRendererPositions.Add(position);
                                    // Add more processing if needed
                                }
                            }
                            UpdatePositionLists(polygon, ref lineRendererPositions, ref geographicalPositions, ref innerMeshPositions, ref innerGeographicalPositions);
                            if (lineRendererPositions.Count < 15)
                            {
                                continue; // Skip this LinearRing
                            }

                            //Debug.Log(feature.properties.NAME_3 + " " + lineRendererPositions.Count);
                            GameObject land = ObjectPooler.Instance.GetPooledObject();
                            land.name = "MultiPolygon";

                            RenderLandGeometry(land, lineRendererPositions, geographicalPositions);
                            SetupLand(land, feature);
                        }
                    }
                }
            }

            featuresProcessed++;




            if (featuresProcessed == minFeaturesToProcess)
            {
                yield return Timing.WaitForOneFrame; // Pause the method and continue from here in the next frame.
                featuresProcessed = 0;
            }
            // SetupLandParentScaleAndPosition();


        }
        processDone = true;
        ProcessAndSetupTextForLandControllers();
        UpdateIdentifierToLandControllersMap();
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
        MapTransformProperties zoomProperties = GetMapProperties(currentZoom);
        landController.lineRenderer.startWidth = zoomProperties.linewidthMultiplier;
        landController.lineRenderer.endWidth = zoomProperties.linewidthMultiplier;
        if (currentType == MapType.Plz5Stelig)
        {
            if (currentZoom < 10)
            {
                landController.textIntialScale = Vector3.one * 0;
                landController.textMesh.transform.localScale = Vector3.one * 0;
            }
            else
            {
                landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
            }

        }

        if (currentType == MapType.Plz3Stelig)
        {
            if (currentZoom < 9)
            {
                landController.textIntialScale = Vector3.one * 0;
                landController.textMesh.transform.localScale = Vector3.one * 0;
            }
            else
            {
                landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
            }

        }

        if (currentType == MapType.Plz2Stelig)
        {
            if (currentZoom < 8)
            {
                landController.textIntialScale = Vector3.one * 0;
                landController.textMesh.transform.localScale = Vector3.one * 0;
            }
            else
            {
                landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
            }
        }

        if (currentType == MapType.Plz1Stelig)
        {
            landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
            landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
        }


        if (currentType == MapType.Kreise)
        {
            if (currentZoom < 9)
            {
                landController.textIntialScale = Vector3.one * 0;
                landController.textMesh.transform.localScale = Vector3.one * 0;
            }
            else
            {
                landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
                landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
            }

        }

        if (currentType == MapType.Bundeslaender)
        {

            landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
            landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
        }

        if (currentType == MapType.Regierungsbezirke)
        {

            landController.textIntialScale = Vector3.one * zoomProperties.textSizeMultiplier;
            landController.textMesh.transform.localScale = Vector3.one * zoomProperties.textSizeMultiplier;
        }



        LandControllersSearch.Add(landController);


        if (currentType == MapType.Plz1Stelig)
        {
            landController.plz = feature.properties.plz;
            landController.SetupText(feature.properties.plz, feature.properties.plz);

        }
        if (currentType == MapType.Plz2Stelig)
        {
            landController.plz = feature.properties.plz;
            landController.SetupText(feature.properties.plz, feature.properties.plz);

        }

        if (currentType == MapType.Plz3Stelig)
        {
            landController.plz = feature.properties.plz;
            landController.SetupText(feature.properties.plz, feature.properties.plz);

        }
        if (currentType == MapType.Plz5Stelig)
        {
            // Assuming feature.properties.plz is a string, convert it to int for comparison
            int plz = int.Parse(feature.properties.plz);
            landController.plz = feature.properties.plz;
            SerializableGeoDate matchedGeoDate = geoDataFileReader.geoDates.Find(geoDate => geoDate.POSTLEITZAHL == plz);


            if (matchedGeoDate != null)
            {
                string regierungsbezirkName = matchedGeoDate.REGIERUNGSBEZIRK_NAME;
                string kreisName = matchedGeoDate.KREIS_NAME;
                string bundeslandName = matchedGeoDate.BUNDESLAND_NAME;

                string displayText = $"plz: {plz}\nregierungsbezirk: {regierungsbezirkName}\nKreise: {kreisName}\nbundesland: {bundeslandName}\n";
                displayText = displayText.Replace(",", "");
                if (landController.lineRenderer.positionCount > 20)
                {
                    landController.SetupText(plz.ToString(), displayText);
                }
                else
                {
                    landController.SetupText("");
                }
            }
            else
            {
                Debug.Log($"Can't find matching postalcode {feature.properties.plz}");
                // Handle the case where no matching geo date is found
                landController.SetupText($"{plz}", $"{plz}");
            }
        }




        if (currentType == MapType.Kreise)
        {
            landController.KREIS_Name = feature.properties.NAME_3;
        }
        if (currentType == MapType.Bundeslaender)
        {
            landController.Bundesland = feature.properties.name;

        }
        if (currentType == MapType.Regierungsbezirke)
        {
            landController.Regierungsbezirk = feature.properties.NAME_2;

        }
    }

    public List<LandController> FindAllByPLZ(List<LandController> features, string plz)
    {
        return features.Where(feature => feature.plz != null && feature.plz == plz).ToList();
    }

    public List<LandController> FindAllByKreise(List<LandController> features, string kreise)
    {
        return features.Where(feature => feature.KREIS_Name.ToLower() != null && feature.KREIS_Name.ToLower() == kreise.ToLower()).ToList();
    }
    public List<LandController> FindAllByBundesLander(List<LandController> features, string bundesLander)
    {
        return features.Where(feature => feature.Bundesland.ToLower() != null && feature.Bundesland.ToLower() == bundesLander.ToLower()).ToList();
    }
    public List<LandController> FindAllByRegierungsbezirke(List<LandController> features, string regierungsbezirke)
    {
        return features.Where(feature => feature.Regierungsbezirk.ToLower() != null && feature.Regierungsbezirk.ToLower() == regierungsbezirke.ToLower()).ToList();
    }


    public void SearchPlace()
    {
        foreach (var landController in LandControllersSearch)
        {
            landController.DehighlightOnMouseExit();
        }


        string searchText = destinationInput.text;
        int zoom = 0; ;
        List<LandController> matchingControllers = new List<LandController>();

        if (currentType == MapType.Plz1Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 7;
        }
        if (currentType == MapType.Plz2Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 8;
        }
        if (currentType == MapType.Plz3Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 9;
        }
        if (currentType == MapType.Plz5Stelig)
        {
            matchingControllers = FindAllByPLZ(LandControllersSearch, searchText);
            zoom = 10;
        }
        if (currentType == MapType.Bundeslaender)
        {
            matchingControllers = FindAllByBundesLander(LandControllersSearch, searchText);
            zoom = 7;
        }
        if (currentType == MapType.Kreise)
        {
            matchingControllers = FindAllByKreise(LandControllersSearch, searchText);
            zoom = 10;
        }
        if (currentType == MapType.Regierungsbezirke)
        {
            matchingControllers = FindAllByRegierungsbezirke(LandControllersSearch, searchText);
            zoom = 7;

        }




        if (matchingControllers.Count > 0)
        {
            OnlineMaps.SetPositionAndZoom(matchingControllers[0].realCoordinatesToCentering[0].x, matchingControllers[0].realCoordinatesToCentering[0].z, zoom);
            SetupLandParentScaleAndPosition();
            foreach (LandController controller in matchingControllers)
            {

                controller.HighlightOnMouseEnter();
                currentZoom = zoom;
                SetupLandParentScaleAndPosition();
            }
        }
        else
        {
            Debug.Log("No matching LandControllers found for PLZ: " + searchText);
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

    private void SetupLandControllerText(LandController landController)
    {
        string textDetailed = "";
        string textOnMap = "";
        switch (currentType)
        {
            case MapType.Plz1Stelig:
            case MapType.Plz2Stelig:
            case MapType.Plz3Stelig:
            case MapType.Plz5Stelig:
                // Assuming the PLZ is a significant identifier for these types
                textDetailed = $"PLZ: {landController.plz}";
                textOnMap = landController.plz;
                break;
            case MapType.Kreise:
                SerializableGeoDate matchedGeoDate = geoDataFileReader.geoDates.Find(geoDate => geoDate.KREIS_NAME == landController.KREIS_Name);
                if (matchedGeoDate != null)
                {
                    string regierungsbezirkName = matchedGeoDate.REGIERUNGSBEZIRK_NAME;
                    string kreisName = matchedGeoDate.KREIS_NAME;
                    string bundeslandName = matchedGeoDate.BUNDESLAND_NAME;

                    textDetailed = $"regierungsbezirk: {regierungsbezirkName}\nKreise: {kreisName}\nbundesland: {bundeslandName}\n";
                    textDetailed = textDetailed.Replace(",", "");
                    textOnMap = kreisName;
                }
                else
                {
                    textDetailed = landController.KREIS_Name;
                    textOnMap = landController.KREIS_Name;
                }
                break;
            case MapType.Bundeslaender:
                textDetailed = $"Bundesland: {landController.Bundesland}";
                textOnMap = landController.Bundesland;
                break;
            case MapType.Regierungsbezirke:
                SerializableGeoDate matchedGeoDateRegierungsbezirke = geoDataFileReader.geoDates.Find(geoDate => geoDate.REGIERUNGSBEZIRK_NAME == landController.Regierungsbezirk);
                if (matchedGeoDateRegierungsbezirke != null)
                {
                    string regierungsbezirkName = matchedGeoDateRegierungsbezirke.REGIERUNGSBEZIRK_NAME;
                    string bundeslandName = matchedGeoDateRegierungsbezirke.BUNDESLAND_NAME;

                    textDetailed = $"Regierungsbezirk: {regierungsbezirkName}\nBundesland: {bundeslandName}\n";
                    textDetailed = textDetailed.Replace(",", "");
                    textOnMap = bundeslandName;
                }
                else
                {
                    textDetailed = $"Regierungsbezirk: {landController.Regierungsbezirk}";
                    textOnMap = landController.Regierungsbezirk;
                }
                break;
            default:
                textDetailed = ""; // Default case, no text
                break;
        }

        // Assuming the LandController has a method to set up text
        landController.SetupText(textOnMap, textDetailed);
    }

    public void ProcessAndSetupTextForLandControllers()
    {
        if (currentType == MapType.Plz5Stelig) return;

        // Group by different unique identifiers based on the current type
        var groupedLandControllers = GroupLandControllersByType();

        foreach (var group in groupedLandControllers)
        {

            LandController largestLandController = FindLargestLandController(group);

            foreach (LandController landController in group)
            {
                // Set text for the largest landController
                if (landController == largestLandController)
                {
                    SetupLandControllerText(landController);
                }
                else
                {

                    landController.SetupText("");
                }
            }
        }
    }

    private IEnumerable<IGrouping<string, LandController>> GroupLandControllersByType()
    {
        switch (currentType)
        {
            case MapType.Plz1Stelig:
            case MapType.Plz2Stelig:
            case MapType.Plz3Stelig:
            case MapType.Plz5Stelig:
                return LandControllersSearch.GroupBy(controller => controller.plz);
            case MapType.Kreise:
                return LandControllersSearch.GroupBy(controller => controller.KREIS_Name);
            case MapType.Bundeslaender:
                return LandControllersSearch.GroupBy(controller => controller.Bundesland);
            case MapType.Regierungsbezirke:
                return LandControllersSearch.GroupBy(controller => controller.Regierungsbezirk);
            default:
                return new List<IGrouping<string, LandController>>();
        }
    }

    private LandController FindLargestLandController(IGrouping<string, LandController> group)
    {
        return group.OrderByDescending(controller => controller.GetAreaSize()).FirstOrDefault();
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

    private void RenderLandGeometry(GameObject land, List<Vector3> lineRendererPositions,
                                    List<Vector3> geographicalPositions)
    {
        LandController landController = land.GetComponent<LandController>();
        landController.realCoordinates = lineRendererPositions;
        landController.realCoordinatesToCentering = geographicalPositions;

        List<Vector3> newPositions = ApplyAnimationCurvesToPositions(lineRendererPositions,
                                    mapTransformProperties.animationCurveVertical,
                                    mapTransformProperties.animationCurveHorisontal);

        landController.lineRenderer.positionCount = newPositions.Count;
        landController.lineRenderer.SetPositions(newPositions.ToArray());

        int[] indices = GeometryUtilities.TriangulatePolygons(newPositions);
        Mesh mesh = GeometryUtilities.CreateMeshFromIndices(newPositions, indices);
        landController.meshFilter.mesh = mesh;

        MeshCollider meshCollider = land.AddComponent<MeshCollider>();
        landController.meshCollider = meshCollider;
        meshCollider.sharedMesh = mesh;
        meshCollider.transform.localPosition = Vector3.zero;
        land.transform.localPosition = Vector3.zero;
        landController.transform.localPosition = Vector3.zero;
    }





    private void UpdateIdentifierToLandControllersMap()
    {
        identifierToLandControllersMap = new Dictionary<string, List<LandController>>();

        foreach (var landController in LandControllersSearch)
        {
            string key = GetIdentifierBasedOnCurrentType(landController);

            if (key != null)
                if (!identifierToLandControllersMap.ContainsKey(key))
                {
                    identifierToLandControllersMap[key] = new List<LandController>();
                }
            if (key != null)
                identifierToLandControllersMap[key].Add(landController);
        }
    }

    public string GetIdentifierBasedOnCurrentType(LandController landController)
    {
        switch (currentType)
        {
            case MapType.Plz1Stelig:
            case MapType.Plz2Stelig:
            case MapType.Plz3Stelig:
            case MapType.Plz5Stelig:
                return landController.plz;
            case MapType.Kreise:
                return landController.KREIS_Name;
            case MapType.Bundeslaender:
                return landController.Bundesland;
            case MapType.Regierungsbezirke:
                return landController.Regierungsbezirk;
            default:
                return "";
        }
    }
}