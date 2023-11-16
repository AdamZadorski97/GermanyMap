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
    private Button buttonSetPlz1;
    [SerializeField]
    private Button buttonSetPlz2;
    [SerializeField]
    private Button buttonSetPlz3;
    [SerializeField]
    private Button buttonSetPlz5;
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

        buttonSetKreise.onClick.RemoveAllListeners();
        buttonSetBundeslaender.onClick.RemoveAllListeners();
        buttonSetRegierungsbezirke.onClick.RemoveAllListeners();
        buttonSetPlz1.onClick.RemoveAllListeners();
        buttonSetPlz2.onClick.RemoveAllListeners();
        buttonSetPlz3.onClick.RemoveAllListeners();
        buttonSetPlz5.onClick.RemoveAllListeners();
        buttonFindPlz.onClick.RemoveAllListeners();

        buttonSetKreise.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Kreise));
        buttonSetBundeslaender.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Bundeslaender));
        buttonSetRegierungsbezirke.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Regierungsbezirke));
        buttonSetPlz1.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Plz1Stelig));
        buttonSetPlz2.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Plz2Stelig));
        buttonSetPlz3.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Plz3Stelig));
        buttonSetPlz5.onClick.AddListener(() => GeoJSONFileReader.Instance.SetupMap(MapType.Plz5Stelig));
        buttonFindPlz.onClick.AddListener(() => GeoJSONFileReader.Instance.SearchPlace());
    }
    private void SetupToggles()
    {
        toggleAutoPlzZoom.isOn = GeoJSONFileReader.Instance.autoPlzZoom;
        toggleAutoPlzZoom.onValueChanged.AddListener(delegate { GeoJSONFileReader.Instance.SetAutoPlzZoom(); });
    }

    public void SetText(string message)
    {
        textPlzNumber.text = message;
    }
    

}
