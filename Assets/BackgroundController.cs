using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundController : MonoBehaviour
{
    const string PREFS_KEY_BACKGROUND_ENABLED = "_BackgroundEnabled";

    public GameObject settingsPanel;
    public GameObject backgroundPanel;
    public GameObject backgroundOff;
    public GameObject backgroundOn;
    public Button backgroundOnButton;
    public Button backgroundOffButton;

    private void Awake()
    {
        backgroundOnButton.onClick.AddListener(BackgroundButtonOnPressed);
        backgroundOffButton.onClick.AddListener(BackgroundButtonOffPressed);
    }

    private void OnEnable()
    {
        bool backgroundEnabled = IsBackgroundEnabled();
        backgroundOn.SetActive(backgroundEnabled);
        backgroundPanel.SetActive(backgroundEnabled);
        backgroundOff.SetActive(backgroundEnabled == false);

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

    private void ToggleBackground(bool backgroundEnabled)
    {
        PlayerPrefs.SetInt(PREFS_KEY_BACKGROUND_ENABLED, (backgroundEnabled ? 1 : 0));
        PlayerPrefs.Save();
    }

    private bool IsBackgroundEnabled()
    {
        return (PlayerPrefs.GetInt(PREFS_KEY_BACKGROUND_ENABLED, 1) == 1);
    }

    public void EnableSettingsPanel()
    {
        settingsPanel.SetActive(true);
    }

    public void DisableSettingsPanel()
    {
        settingsPanel.SetActive(false);
    }
}