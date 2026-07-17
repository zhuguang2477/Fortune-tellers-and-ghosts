using UnityEngine;
using FishNet;
using FishNet.Connection;

public class RadarInputManager : MonoBehaviour
{
    [Header("Настройки клавиш")]
    public KeyCode toggleKey = KeyCode.Tab;
    public KeyCode skillKey = KeyCode.F;

    [Header("Настройки хрустального шара")]
    public float crystalBallRange = 2f;
    public Transform crystalBallTransform;

    [Header("Ссылки UI")]
    public GameObject radarPanel;

    private RadarUI cachedRadarUI;

    private void Update()
    {
        if (!InstanceFinder.IsClientStarted) return;

        if (Input.GetKeyDown(skillKey))
        {
            if (!IsLocalPlayerPrizrak())
            {
                Debug.Log("Только прорицатель может использовать навык хрустального шара");
                return;
            }

            if (IsNearCrystalBall())
            {
                Debug.Log("Прорицатель использует навык хрустального шара, радиус радара увеличен");
                RadarManager radarManager = FindObjectOfType<RadarManager>();
                if (radarManager != null)
                    radarManager.CmdActivateRadarBoost();
                else
                    Debug.LogWarning("RadarManager не найден");
            }
            else
            {
                Debug.Log("Слишком далеко от хрустального шара, невозможно использовать навык");
            }
        }

        if (Input.GetKeyDown(toggleKey))
        {
            if (!IsLocalPlayerPrizrak())
            {
                Debug.Log("Только прорицатель может использовать радар");
                return;
            }
            ToggleRadar();
        }
    }

    private bool IsNearCrystalBall()
    {
        if (crystalBallTransform == null)
        {
            Debug.LogWarning("Transform хрустального шара не назначен, перетащите объект хрустального шара в Inspector");
            return false;
        }
        GameObject localPlayer = InstanceFinder.ClientManager.Connection?.FirstObject?.gameObject;
        if (localPlayer == null) return false;
        float dist = Vector3.Distance(localPlayer.transform.position, crystalBallTransform.position);
        return dist <= crystalBallRange;
    }

    private bool IsLocalPlayerPrizrak()
    {
        NetworkConnection conn = InstanceFinder.ClientManager.Connection;
        if (conn == null)
        {
            Debug.LogWarning("[Radar] Локальное соединение пусто");
            return false;
        }

        RoleManager.Role role = RoleManager.Instance.GetPlayerRole(conn);
        Debug.Log($"[Radar] Роль локального игрока: {role}, ожидается: Prizrak");

        if (role == RoleManager.Role.None)
        {
            GameObject localPlayer = conn.FirstObject?.gameObject;
            if (localPlayer != null)
            {
                bool isPrizrak = localPlayer.CompareTag("Prizrak");
                Debug.Log($"[Radar] Определение по тегу: {isPrizrak}");
                return isPrizrak;
            }
        }

        return role == RoleManager.Role.Prizrak;
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