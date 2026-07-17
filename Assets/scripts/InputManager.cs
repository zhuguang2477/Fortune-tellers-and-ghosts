using UnityEngine;
using FishNet;

public class RadarInputManager : MonoBehaviour
{
    [Header("Настройки клавиш")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Привязка UI (опционально)")]
    public GameObject radarPanel;

    private RadarUI cachedRadarUI;

    private void Update()
    {
        if (!InstanceFinder.IsClientStarted) return;

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleRadar();
        }
    }

    private void ToggleRadar()
    {
        RadarUI radarUI = GetRadarUI();
        if (radarUI == null)
        {
            Debug.LogWarning("RadarUI не найден");
            return;
        }

        if (radarUI.gameObject.activeSelf)
            radarUI.HideRadar();
        else
            radarUI.ShowRadar();
    }

    private RadarUI GetRadarUI()
    {
        if (cachedRadarUI != null) return cachedRadarUI;

        if (radarPanel != null)
        {
            cachedRadarUI = radarPanel.GetComponent<RadarUI>();
            if (cachedRadarUI == null)
                cachedRadarUI = radarPanel.GetComponentInChildren<RadarUI>();
            if (cachedRadarUI != null)
                return cachedRadarUI;
        }

        cachedRadarUI = FindObjectOfType<RadarUI>(true);
        return cachedRadarUI;
    }
}