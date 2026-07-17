using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Server;
using FishNet.Transporting;
using System.Collections.Generic;
using UnityEngine;

public class RoleManager : NetworkBehaviour
{
    public enum Role { None, Gadalka, Prizrak }

    private readonly SyncVar<bool> _gadalkaOccupied = new SyncVar<bool>();
    private readonly SyncVar<bool> _prizrakOccupied = new SyncVar<bool>();
    private readonly SyncVar<bool> _gameStarted = new SyncVar<bool>();

    public bool GadalkaOccupied => _gadalkaOccupied.Value;
    public bool PrizrakOccupied => _prizrakOccupied.Value;
    public bool GameStarted => _gameStarted.Value;

    public static event System.Action<bool> OnGadalkaOccupiedChangedEvent;
    public static event System.Action<bool> OnPrizrakOccupiedChangedEvent;
    public static event System.Action<bool> OnGameStartedChangedEvent;
    public static event System.Action<bool, string> OnRoleSelectionResultEvent;

    public Transform gadalkaSpawnPoint;
    public Transform prizrakSpawnPoint;

    [Header("Префабы ролей (должны быть зарегистрированы в NetworkManager)")]
    public GameObject gadalkaPrefab;
    public GameObject prizrakPrefab;

    public static RoleManager Instance { get; private set; }

    private Dictionary<NetworkConnection, Role> playerRoles = new Dictionary<NetworkConnection, Role>();
    private Dictionary<NetworkConnection, GameObject> playerObjects = new Dictionary<NetworkConnection, GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        _gadalkaOccupied.OnChange += OnGadalkaOccupiedChanged;
        _prizrakOccupied.OnChange += OnPrizrakOccupiedChanged;
        _gameStarted.OnChange += OnGameStartedChanged;
    }

    private void OnGadalkaOccupiedChanged(bool prev, bool next, bool asServer)
        => OnGadalkaOccupiedChangedEvent?.Invoke(next);
    private void OnPrizrakOccupiedChanged(bool prev, bool next, bool asServer)
        => OnPrizrakOccupiedChangedEvent?.Invoke(next);
    private void OnGameStartedChanged(bool prev, bool next, bool asServer)
        => OnGameStartedChangedEvent?.Invoke(next);

    public override void OnStartServer()
    {
        base.OnStartServer();
        InstanceFinder.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        InstanceFinder.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
    }

    private void ServerManager_OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            if (playerRoles.ContainsKey(connection))
            {
                Role oldRole = playerRoles[connection];
                if (oldRole == Role.Gadalka) _gadalkaOccupied.Value = false;
                else if (oldRole == Role.Prizrak) _prizrakOccupied.Value = false;
                playerRoles.Remove(connection);
            }
            if (playerObjects.ContainsKey(connection))
                playerObjects.Remove(connection);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdSelectRole(Role selectedRole, NetworkConnection sender = null)
    {
        if (_gameStarted.Value || sender == null) return;

        foreach (var kv in playerRoles)
        {
            if (kv.Key != sender && kv.Value == selectedRole)
            {
                TargetRoleSelectionResult(sender, false, "Эта профессия уже выбрана другим игроком");
                return;
            }
        }

        if (playerRoles.TryGetValue(sender, out Role currentRole))
        {
            if (currentRole == selectedRole)
            {
                CmdDeselectRole(sender);
                return;
            }
            if (currentRole == Role.Gadalka) _gadalkaOccupied.Value = false;
            else if (currentRole == Role.Prizrak) _prizrakOccupied.Value = false;
        }

        playerRoles[sender] = selectedRole;
        if (selectedRole == Role.Gadalka) _gadalkaOccupied.Value = true;
        else if (selectedRole == Role.Prizrak) _prizrakOccupied.Value = true;

        TargetRoleSelectionResult(sender, true, $"Выбор успешен, вы стали {(selectedRole == Role.Gadalka ? "Гадалкой" : "Призраком")}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdDeselectRole(NetworkConnection sender = null)
    {
        if (_gameStarted.Value || sender == null) return;
        if (playerRoles.TryGetValue(sender, out Role oldRole))
        {
            if (oldRole == Role.Gadalka) _gadalkaOccupied.Value = false;
            else if (oldRole == Role.Prizrak) _prizrakOccupied.Value = false;
            playerRoles.Remove(sender);
            if (playerObjects.ContainsKey(sender)) playerObjects.Remove(sender);
            TargetRoleSelectionResult(sender, true, "Выбор отменён");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdStartGame(NetworkConnection sender = null)
    {
        if (_gameStarted.Value || sender == null) return;
        if (!_gadalkaOccupied.Value || !_prizrakOccupied.Value) return;

        _gameStarted.Value = true;

        Debug.Log($"[RoleManager] Текущая активная сцена на сервере: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"[RoleManager] Сцена RoleManager: {gameObject.scene.name}");

        foreach (var kv in playerRoles)
        {
            NetworkConnection conn = kv.Key;
            Role role = kv.Value;

            GameObject oldPlayer = conn.FirstObject?.gameObject;
            if (oldPlayer != null)
            {
                InstanceFinder.ServerManager.Despawn(oldPlayer);
                if (playerObjects.ContainsKey(conn))
                    playerObjects.Remove(conn);
            }
            else
            {
                Debug.LogWarning($"У игрока {conn.ClientId} нет действительного FirstObject, невозможно уничтожить");
            }

            GameObject prefabToSpawn = (role == Role.Gadalka) ? gadalkaPrefab : prizrakPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogError($"Префаб для роли {role} игрока {conn.ClientId} не установлен!");
                continue;
            }

            Vector3 spawnPos = (role == Role.Gadalka) ? gadalkaSpawnPoint.position : prizrakSpawnPoint.position;
            GameObject newPlayer = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            newPlayer.transform.parent = null;

            InstanceFinder.ServerManager.Spawn(newPlayer, conn, gameObject.scene);

            newPlayer.tag = role.ToString();
            playerObjects[conn] = newPlayer;

            Debug.Log($"Игрок {conn.ClientId} заменён на роль {role}, создан в {spawnPos}, сцена: {newPlayer.scene.name}, родитель: {newPlayer.transform.parent}");
        }

        foreach (var kv in playerRoles)
        {
            NetworkConnection conn = kv.Key;
            TargetRefreshClient(conn);
        }
    }

    [TargetRpc]
    private void TargetRefreshClient(NetworkConnection target)
    {
        Debug.Log("[TargetRefreshClient] Клиент выполняет обновление");

        RefreshAllPlayersLayers();
        EnableAllPlayerRenderers();
    }

    private void RefreshAllPlayersLayers()
    {
        NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
        Debug.Log($"[RefreshAllPlayersLayers] Найдено {allNetworkObjects.Length} NetworkObject");

        foreach (var netObj in allNetworkObjects)
        {
            GameObject obj = netObj.gameObject;
            string tag = obj.tag;
            if (tag != "Gadalka" && tag != "Prizrak") continue;

            int targetLayer = (tag == "Gadalka") ? LayerMask.NameToLayer("Gadalka") : LayerMask.NameToLayer("Prizrak");
            if (targetLayer == -1)
            {
                Debug.LogError($"[RefreshAllPlayersLayers] Не удалось найти слой с именем: {tag}, проверьте имена слоёв в Project Settings!");
                continue;
            }

            SetLayerRecursively(obj.transform, targetLayer);
            Debug.Log($"[RefreshAllPlayersLayers] Установка слоя игрока {obj.name} (Tag={tag}) на {targetLayer} ({LayerMask.LayerToName(targetLayer)})");
        }
    }

    private void EnableAllPlayerRenderers()
    {
        foreach (var netObj in FindObjectsOfType<NetworkObject>())
        {
            GameObject obj = netObj.gameObject;
            string tag = obj.tag;
            if (tag != "Gadalka" && tag != "Prizrak") continue;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                rend.enabled = true;
                Debug.Log($"[EnableAllPlayerRenderers] Включение рендерера {rend.name} на {obj.name}");
            }
        }
    }

    private void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform child in t)
            SetLayerRecursively(child, layer);
    }

    [TargetRpc]
    private void TargetRoleSelectionResult(NetworkConnection target, bool success, string message)
    {
        OnRoleSelectionResultEvent?.Invoke(success, message);
    }

    public string GetRoleName(NetworkConnection conn)
    {
        if (playerRoles.TryGetValue(conn, out Role role))
            return role.ToString();
        return "Unknown";
    }

    public GameObject GetPlayerByRole(Role role)
    {
        foreach (var kv in playerObjects)
        {
            if (playerRoles.TryGetValue(kv.Key, out Role r) && r == role)
                return kv.Value;
        }
        return null;
    }
}