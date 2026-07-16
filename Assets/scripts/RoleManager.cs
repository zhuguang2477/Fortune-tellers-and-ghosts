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

    [Header("角色预制体（需在 NetworkManager 中注册）")]
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
                TargetRoleSelectionResult(sender, false, "该职业已被其他玩家选择");
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

        TargetRoleSelectionResult(sender, true, $"选择成功，你成为了{(selectedRole == Role.Gadalka ? "占卜师" : "鬼魂")}");
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
            TargetRoleSelectionResult(sender, true, "已取消选择");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdStartGame(NetworkConnection sender = null)
    {
        if (_gameStarted.Value || sender == null) return;
        if (!_gadalkaOccupied.Value || !_prizrakOccupied.Value) return;

        _gameStarted.Value = true;

        // 打印当前场景信息（便于调试）
        Debug.Log($"[RoleManager] 服务器当前活动场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"[RoleManager] RoleManager 所在场景: {gameObject.scene.name}");

        foreach (var kv in playerRoles)
        {
            NetworkConnection conn = kv.Key;
            Role role = kv.Value;

            // 销毁旧玩家对象
            GameObject oldPlayer = conn.FirstObject?.gameObject;
            if (oldPlayer != null)
            {
                InstanceFinder.ServerManager.Despawn(oldPlayer);
                if (playerObjects.ContainsKey(conn))
                    playerObjects.Remove(conn);
            }
            else
            {
                Debug.LogWarning($"玩家 {conn.ClientId} 没有有效的 FirstObject，无法销毁");
            }

            // 选择预制体
            GameObject prefabToSpawn = (role == Role.Gadalka) ? gadalkaPrefab : prizrakPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogError($"玩家 {conn.ClientId} 的角色 {role} 对应的预制体未设置！");
                continue;
            }

            // 生成新对象
            Vector3 spawnPos = (role == Role.Gadalka) ? gadalkaSpawnPoint.position : prizrakSpawnPoint.position;
            GameObject newPlayer = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // ---- 关键修复：强制清除父物体，确保对象独立于任何父物体 ----
            // 如果对象被自动设为某个父物体，会导致移动方向错乱和位置同步异常
            newPlayer.transform.parent = null;
            // -----------------------------------------------------------------

            // 使用指定场景进行 Spawn，确保对象归属为 RoleManager 所在的场景
            InstanceFinder.ServerManager.Spawn(newPlayer, conn, gameObject.scene);

            newPlayer.tag = role.ToString();
            playerObjects[conn] = newPlayer;

            Debug.Log($"玩家 {conn.ClientId} 已替换为 {role} 角色，生成于 {spawnPos}，场景: {newPlayer.scene.name}，父物体: {newPlayer.transform.parent}");
        }

        // ---- 通知每个客户端刷新层并强制启用渲染器 ----
        foreach (var kv in playerRoles)
        {
            NetworkConnection conn = kv.Key;
            TargetRefreshClient(conn);
        }
    }

    [TargetRpc]
    private void TargetRefreshClient(NetworkConnection target)
    {
        Debug.Log("[TargetRefreshClient] 客户端执行刷新");

        RefreshAllPlayersLayers();
        EnableAllPlayerRenderers();
    }

    private void RefreshAllPlayersLayers()
    {
        NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
        Debug.Log($"[RefreshAllPlayersLayers] 找到 {allNetworkObjects.Length} 个 NetworkObject");

        foreach (var netObj in allNetworkObjects)
        {
            GameObject obj = netObj.gameObject;
            string tag = obj.tag;
            if (tag != "Gadalka" && tag != "Prizrak") continue;

            int targetLayer = (tag == "Gadalka") ? LayerMask.NameToLayer("Gadalka") : LayerMask.NameToLayer("Prizrak");
            if (targetLayer == -1)
            {
                Debug.LogError($"[RefreshAllPlayersLayers] 找不到层名称：{tag}，请检查 Project Settings 中的层名称！");
                continue;
            }

            SetLayerRecursively(obj.transform, targetLayer);
            Debug.Log($"[RefreshAllPlayersLayers] 设置玩家 {obj.name} (Tag={tag}) 的层为 {targetLayer} ({LayerMask.LayerToName(targetLayer)})");
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
                Debug.Log($"[EnableAllPlayerRenderers] 启用渲染器 {rend.name} 在 {obj.name}");
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
}