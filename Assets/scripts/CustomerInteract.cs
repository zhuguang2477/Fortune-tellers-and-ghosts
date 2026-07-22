using FishNet.Object;
using FishNet;
using UnityEngine;
using FishNet.Connection;

public class CustomerInteract : NetworkBehaviour
{
    [Header("Настройки взаимодействия")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 2f;

    [Header("Опционально: анимация ухода клиента")]
    public float destroyDelay = 0.5f;

    private bool _playerInRange = false;
    private GameObject _nearbyPlayer;

    private void Update()
    {
        if (!IsClient) return;

        GameObject localPlayer = InstanceFinder.ClientManager.Connection?.FirstObject?.gameObject;
        if (localPlayer == null) return;

        float dist = Vector3.Distance(localPlayer.transform.position, transform.position);
        bool currentlyInRange = dist <= interactRange;

        if (currentlyInRange && !_playerInRange)
        {
            _playerInRange = true;
            _nearbyPlayer = localPlayer;
            Debug.Log("[Customer] Игрок вошел в зону взаимодействия, нажмите " + interactKey + " для взаимодействия");
        }
        else if (!currentlyInRange && _playerInRange)
        {
            _playerInRange = false;
            _nearbyPlayer = null;
        }

        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log("[Customer] Игрок нажал клавишу взаимодействия, отправка запроса на уход");
            RequestLeaveServer();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLeaveServer(NetworkConnection sender = null)
    {
        Debug.Log("[Customer] Сервер получил запрос на уход от " + sender?.ClientId);
        if (!IsServer) return;

        CustomerSpawnManager.Instance?.OnCustomerLeft();

        StartCoroutine(DestroyWithDelay());
    }

    private System.Collections.IEnumerator DestroyWithDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        InstanceFinder.ServerManager.Despawn(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}