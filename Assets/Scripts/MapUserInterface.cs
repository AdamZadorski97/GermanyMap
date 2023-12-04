using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapUserInterface : MonoBehaviour
{
    public static MapUserInterface Instance { get; private set; }

    [SerializeField]
    private Button[] mapTypeButtons;  // Array to hold all map type buttons.
    [SerializeField]
    private Button buttonFindPlz, buttonZoomIn, buttonZoomOut;
    [SerializeField]
    private Toggle toggleAutoPlzZoom;
    [SerializeField]
    private TMP_Text textPlzNumber;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeUI();
    }

    // Initializes UI elements like buttons and toggles.
    private void InitializeUI()
    {
        SetupMapTypeButtons();
        SetupZoomButtons();
        SetupToggles();
    }

    // Configures map type buttons with listeners.
    private void SetupMapTypeButtons()
    {
        MapType[] mapTypes = { MapType.Kreise, MapType.Bundeslaender, MapType.Regierungsbezirke, MapType.Plz1Stelig, MapType.Plz2Stelig, MapType.Plz3Stelig, MapType.Plz5Stelig };
        for (int i = 0; i < mapTypeButtons.Length; i++)
        {
            int index = i;  // Prevent closure issue in lambda.
            mapTypeButtons[i].onClick.AddListener(() => SetupMap(mapTypes[index]));
        }
    }

    // Configures Zoom In and Zoom Out buttons.
    private void SetupZoomButtons()
    {
        buttonZoomIn.onClick.AddListener(() => ChangeZoom(1));
        buttonZoomOut.onClick.AddListener(() => ChangeZoom(-1));
        buttonFindPlz.onClick.AddListener(GeoJSONFileReader.Instance.SearchPlace);
    }

    // Changes map zoom level.
    private void ChangeZoom(int zoomChange)
    {
        float targetZoom = GeoJSONFileReader.Instance.OnlineMaps.zoom + zoomChange;
        DOTween.To(() => GeoJSONFileReader.Instance.OnlineMaps.zoom, x => SetZoom(x), targetZoom, 0.2f).SetEase(Ease.Linear);
        GeoJSONFileReader.Instance.SetActiveRenderers(zoomChange);
    }

    // Sets the zoom level of the map.
    private void SetZoom(float zoomLevel)
    {
        var onlineMaps = GeoJSONFileReader.Instance.OnlineMaps;
        onlineMaps.SetPositionAndZoom(onlineMaps.position.x, onlineMaps.position.y, zoomLevel);
    }

    // Sets up map based on type.
    private void SetupMap(MapType mapType)
    {
        GeoJSONFileReader.Instance.mapSwitch = true;
        GeoJSONFileReader.Instance.SetupMap(mapType);
    }

    // Configures the toggle for auto zoom.
    private void SetupToggles()
    {
        toggleAutoPlzZoom.isOn = GeoJSONFileReader.Instance.autoPlzZoom;
        toggleAutoPlzZoom.onValueChanged.AddListener(GeoJSONFileReader.Instance.SetAutoPlzZoom);
    }

    // Sets the displayed text.
    public void SetText(string message)
    {
        textPlzNumber.text = message;
    }
}