using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundController : MonoBehaviour
{
    const string PREFS_KEY_BACKGROUND_ENABLED = "_BackgroundEnabled";
    const string PREFS_KEY_RELOADING_ENABLED = "_ReloadingEnabled";

    public GameObject settingsPanel;
    public GameObject searchPanel;
    public GameObject backgroundPanel;
    public GameObject backgroundOff;
    public GameObject backgroundOn;
    public GameObject mapReloadingOff;
    public GameObject mapReloadingOn;
    public Button backgroundOnButton;
    public Button backgroundOffButton;
    public Button mapReloadingOnButton;
    public Button mapReloadingOffButton;

    private void Awake()
    {
        backgroundOnButton.onClick.AddListener(BackgroundButtonOnPressed);
        backgroundOffButton.onClick.AddListener(BackgroundButtonOffPressed);
        mapReloadingOnButton.onClick.AddListener(MapReloadingButtonOnPressed);
        mapReloadingOffButton.onClick.AddListener(MapReloadingButtonOffPressed);
    }

    private void OnEnable()
    {
        bool backgroundEnabled = IsBackgroundEnabled();
        bool mapReloadingEnabled = IsMapReloadingEnabled();
        backgroundOn.SetActive(backgroundEnabled);
        backgroundPanel.SetActive(backgroundEnabled);
        backgroundOff.SetActive(backgroundEnabled == false);

        mapReloadingOff.SetActive(mapReloadingEnabled == false);
        mapReloadingOn.SetActive(mapReloadingEnabled);

        Time.timeScale = 0;
    }

    private void OnDisable()
    {
        Time.timeScale = 1;
    }

    private void BackgroundButtonOnPressed()
    {
        backgroundOn.SetActive(false);
        backgroundOff.SetActive(true);
        backgroundPanel.SetActive(false);
        ToggleBackground(false);
    }

    private void BackgroundButtonOffPressed()
    {
        backgroundOff.SetActive(false);
        backgroundOn.SetActive(true);
        backgroundPanel.SetActive(true);
        ToggleBackground(true);
    }

    private void MapReloadingButtonOnPressed()
    {
        mapReloadingOn.SetActive(false);
        mapReloadingOff.SetActive(true);
        ToggleMapReloading(false);
    }

    private void MapReloadingButtonOffPressed()
    {
        mapReloadingOff.SetActive(false);
        mapReloadingOn.SetActive(true);
        ToggleMapReloading(true);
    }

    private void ToggleMapReloading(bool reloadingEnabled)
    {
        PlayerPrefs.SetInt(PREFS_KEY_RELOADING_ENABLED, (reloadingEnabled ? 1 : 0));
        PlayerPrefs.Save();
    }

    private void ToggleBackground(bool backgroundEnabled)
    {
        PlayerPrefs.SetInt(PREFS_KEY_BACKGROUND_ENABLED, (backgroundEnabled ? 1 : 0));
        PlayerPrefs.Save();
    }

    private bool IsBackgroundEnabled()
    {
        return (PlayerPrefs.GetInt(PREFS_KEY_BACKGROUND_ENABLED, 1) == 1);
    }

    private bool IsMapReloadingEnabled()
    {
        return (PlayerPrefs.GetInt(PREFS_KEY_RELOADING_ENABLED, 1) == 1);
    }

    public void EnableSettingsPanel()
    {
        settingsPanel.SetActive(true);
    }

    public void DisableSettingsPanel()
    {
        settingsPanel.SetActive(false);
    }
    
    public void EnableSearchPanel()
    {
        searchPanel.SetActive(true);
    }
    
    public void DisableSearchPanel()
    {
        searchPanel.SetActive(false);
    }
}