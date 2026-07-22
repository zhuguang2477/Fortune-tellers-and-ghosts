using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using UnityEngine;

public class DayNightManager : NetworkBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Состояние дня и ночи")]
    private readonly SyncVar<bool> _isDay = new SyncVar<bool>(true);
    public bool IsDay => _isDay.Value;

    [Header("Компоненты визуализации сцены (опционально)")]
    public Light sunLight;
    public Material skyboxMaterial;
    public Material nightSkyboxMaterial;

    public static event System.Action<bool> OnDayNightChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _isDay.Value = true;
        ObserversRefreshVisuals();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateVisuals(_isDay.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdSetDayNight(bool isDay, NetworkConnection sender = null)
    {
        if (!IsServer) return;
        if (_isDay.Value == isDay) return;

        _isDay.Value = isDay;
        ObserversRefreshVisuals();
        OnDayNightChanged?.Invoke(isDay);
        Debug.Log($"Смена дня и ночи: {(isDay ? "День" : "Ночь")}");
    }

    private void ObserversRefreshVisuals()
    {
        if (!IsServer) return;
        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            TargetRefreshVisuals(conn, _isDay.Value);
        }
        UpdateVisuals(_isDay.Value);
    }

    [TargetRpc]
    private void TargetRefreshVisuals(NetworkConnection target, bool isDay)
    {
        UpdateVisuals(isDay);
    }

    private void UpdateVisuals(bool isDay)
    {
        if (sunLight != null)
        {
            sunLight.color = isDay ? Color.white : Color.blue;
            sunLight.intensity = isDay ? 1f : 0.3f;
        }
        if (skyboxMaterial != null && nightSkyboxMaterial != null)
        {
            RenderSettings.skybox = isDay ? skyboxMaterial : nightSkyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
    }
}