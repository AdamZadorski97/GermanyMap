using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapUserInterface : MonoBehaviour
{
    public static MapUserInterface Instance { get; private set; }

    [SerializeField]
    private Button buttonSetKreise;
    [SerializeField]
    private Button buttonSetRegierungsbezirke;
    [SerializeField]
    private Button buttonSetBundeslaender;
    [SerializeField]
    private Button buttonSetPlz;
    [SerializeField]
    private Button buttonFindPlz;

    public Toggle toggleAutoPlzZoom;

    public TMP_Text textPlzNumber;


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
        
        SetupButtons();
        SetupToggles();
    }

    private void SetupButtons()
    {
        buttonSetKreise.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Kreise));
        buttonSetBundeslaender.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Bundeslaender));
        buttonSetRegierungsbezirke.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Regierungsbezirke));
        buttonSetPlz.onClick.AddListener(() => GeoJSONFileReader.Instance.LoadGeoJSON(GeoJSONFileReader.Instance.geoJSONFilePaths[4]));
        buttonFindPlz.onClick.AddListener(() => GeoJSONFileReader.Instance.SearchPlace());
    }
    private void SetupToggles()
    {
        toggleAutoPlzZoom.isOn = GeoJSONFileReader.Instance.autoPlzZoom;
        toggleAutoPlzZoom.onValueChanged.AddListener(delegate { GeoJSONFileReader.Instance.SetAutoPlzZoom(); });
    }

    public void SetPlzText(string message)
    {
        textPlzNumber.text = message;
    }
    

}
