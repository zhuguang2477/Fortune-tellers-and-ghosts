using FishNet.Object;
using UnityEngine;

public class CrystalBall : NetworkBehaviour
{
    private bool radarActive = false;

    private void OnMouseDown()
    {
        if (!IsOwner) return;

        ToggleRadar();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleRadar();
        }
    }

    private void ToggleRadar()
    {
        radarActive = !radarActive;
        if (radarActive)
        {
            RadarUI radarUI = FindObjectOfType<RadarUI>(true);
            if (radarUI != null)
            {
                radarUI.ShowRadar();
                Debug.Log("Радар включен");
            }
            else
            {
                Debug.LogWarning("RadarUI не найден, убедитесь, что в сцене есть компонент RadarUI");
            }
        }
        else
        {
            RadarUI radarUI = FindObjectOfType<RadarUI>(true);
            if (radarUI != null)
            {
                radarUI.HideRadar();
                Debug.Log("Радар выключен");
            }
        }
    }
}