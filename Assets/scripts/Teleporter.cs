using FishNet.Connection;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Teleporter : NetworkBehaviour
{
    [Header("Список точек телепортации")]
    public Transform[] targetPoints;

    [Header("Настройки клавиш")]
    public KeyCode teleportKey = KeyCode.T;

    [Header("Радиус обнаружения")]
    public float detectionRadius = 3f;

    private readonly SyncVar<int> _currentTargetIndex = new SyncVar<int>();

    public Transform CurrentTarget => (targetPoints != null && targetPoints.Length > 0 && _currentTargetIndex.Value < targetPoints.Length) 
        ? targetPoints[_currentTargetIndex.Value] : null;

    public int TargetCount => targetPoints?.Length ?? 0;

    public string GetTargetName(int index)
    {
        if (targetPoints == null || index < 0 || index >= targetPoints.Length) return "Без имени";
        return targetPoints[index] != null ? targetPoints[index].name : $"Цель {index}";
    }

    public int CurrentTargetIndex => _currentTargetIndex.Value;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger) col.isTrigger = true;

        if (targetPoints != null && targetPoints.Length > 0)
            _currentTargetIndex.Value = 0;
    }

    private void Update()
    {
        if (!IsClient) return;

        if (IsLocalPlayerInRange() && Input.GetKeyDown(teleportKey))
        {
            Debug.Log("[Teleporter] Нажата клавиша телепортации");
            RequestTeleportServer();
        }
    }

    private bool IsLocalPlayerInRange()
    {
        GameObject localPlayer = InstanceFinder.ClientManager.Connection?.FirstObject?.gameObject;
        if (localPlayer == null) return false;
        float dist = Vector3.Distance(localPlayer.transform.position, transform.position);
        return dist <= detectionRadius;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdChangeTarget(int newIndex, NetworkConnection sender = null)
    {
        if (sender == null) return;

        RoleManager.Role role = RoleManager.Instance.GetPlayerRole(sender);
        if (role != RoleManager.Role.Prizrak)
        {
            Debug.Log("[Teleporter] Только Призрак может переключать цели телепортации");
            return;
        }

        if (targetPoints == null || targetPoints.Length == 0)
        {
            Debug.LogWarning("[Teleporter] Нет доступных точек телепортации");
            return;
        }

        if (newIndex < 0 || newIndex >= targetPoints.Length)
        {
            Debug.LogWarning($"[Teleporter] Индекс цели {newIndex} вне диапазона");
            return;
        }

        _currentTargetIndex.Value = newIndex;
        Debug.Log($"[Teleporter] Цель телепортации переключена на {targetPoints[newIndex].name}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestTeleportServer(NetworkConnection sender = null)
    {
        if (sender == null) return;

        GameObject playerObj = sender.FirstObject?.gameObject;
        if (playerObj == null) return;

        RoleManager.Role role = RoleManager.Instance?.GetPlayerRole(sender) ?? RoleManager.Role.None;
        if (role != RoleManager.Role.Gadalka)
        {
            Debug.Log($"[Teleporter] Игрок не-призрак ({role}) пытается телепортироваться, отклонено");
            return;
        }

        float dist = Vector3.Distance(playerObj.transform.position, transform.position);
        if (dist > detectionRadius * 1.5f)
        {
            Debug.Log($"[Teleporter] Игрок на расстоянии {dist} от телепорта, превышает порог, отклонено");
            return;
        }

        Transform target = CurrentTarget;
        if (target == null)
        {
            Debug.LogError("[Teleporter] Текущая цель равна null, телепортация невозможна");
            return;
        }

        Transform playerTransform = playerObj.transform;
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerTransform.position = target.position;
            cc.enabled = true;
        }
        else
        {
            playerTransform.position = target.position;
        }

        Debug.Log($"[Teleporter] Игрок-призрак {sender.ClientId} телепортирован в {target.name}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (targetPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var t in targetPoints)
            {
                if (t != null) Gizmos.DrawSphere(t.position, 0.5f);
            }
        }
    }
}