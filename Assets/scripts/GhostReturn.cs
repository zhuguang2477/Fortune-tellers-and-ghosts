using FishNet;
using FishNet.Object;
using UnityEngine;
using FishNet.Connection;

public class TeleporterBack : NetworkBehaviour
{
    [Header("Целевая точка телепортации")]
    public Transform targetPoint;

    [Header("Настройка клавиши")]
    public KeyCode teleportKey = KeyCode.T;

    [Header("Радиус обнаружения")]
    public float detectionRadius = 3f;

    private void Awake()
    {
        Debug.Log("[TeleporterBack] Awake - ");
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
        else if (col == null)
        {
            
        }
    }

    private void Update()
    {
        if (!IsClient) return;

        if (IsLocalPlayerInRange() && Input.GetKeyDown(teleportKey))
        {
            RequestTeleportServer();
        }

        if (Time.frameCount % 300 == 0)
        {
            GameObject localPlayer = InstanceFinder.ClientManager.Connection?.FirstObject?.gameObject;
            if (localPlayer != null)
            {
                float dist = Vector3.Distance(localPlayer.transform.position, transform.position);
            }
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
    private void RequestTeleportServer(NetworkConnection sender = null)
    {
        Debug.Log($"[TeleporterBack] ServerRpc получен, отправитель={sender?.ClientId}");
        if (sender == null) return;

        GameObject playerObj = sender.FirstObject?.gameObject;
        if (playerObj == null)
        {
            Debug.Log("[TeleporterBack] Объект игрока пуст");
            return;
        }

        RoleManager.Role role = RoleManager.Instance?.GetPlayerRole(sender) ?? RoleManager.Role.None;
        if (role != RoleManager.Role.Gadalka)
        {
            Debug.Log($"[TeleporterBack] Не-призрак ({role}) пытается телепортироваться, отклонено");
            return;
        }

        float dist = Vector3.Distance(playerObj.transform.position, transform.position);
        if (dist > detectionRadius * 1.5f)
        {
            Debug.Log($"[TeleporterBack] Расстояние игрока до телепорта {dist} единиц, превышает порог, отклонено");
            return;
        }

        Transform playerTransform = playerObj.transform;
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerTransform.position = targetPoint.position;
            cc.enabled = true;
        }
        else
        {
            playerTransform.position = targetPoint.position;
        }

        Debug.Log($"[TeleporterBack] Игрок-призрак {sender.ClientId} телепортирован в {targetPoint.position}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}