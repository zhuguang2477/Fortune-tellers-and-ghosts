using FishNet;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerSpawnManager : NetworkBehaviour
{
    public static CustomerSpawnManager Instance { get; private set; }

    [Header("Префабы посетителей")]
    public GameObject[] dayPrefabs;
    public GameObject[] nightPrefabs;

    [Header("Настройки спавна")]
    public Transform spawnPoint;
    public KeyCode startKey = KeyCode.K;

    [Header("Параметры времени")]
    public float dayNightTransitionDelay = 10f;
    public float minSpawnDelay = 10f;
    public float maxSpawnDelay = 20f;

    private bool _isSpawningActive = false;
    private bool _isWaitingToSpawn = false;
    private Coroutine _spawnCoroutine;
    private GameObject _currentCustomer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Debug.Log($"[CustomerSpawnManager] Текущая сцена: {gameObject.scene.name}");
    }

    private void Update()
    {
        if (!IsClient) return;
        if (!Input.GetKeyDown(startKey)) return;
        CmdStartSpawning();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdStartSpawning(NetworkConnection sender = null)
    {
        if (_isSpawningActive) return;
        Debug.Log("[CustomerSpawnManager] Запуск процесса спавна");
        _isSpawningActive = true;
        SpawnCustomer();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public void OnCustomerLeft()
    {
        if (!IsServer) return;
        if (!_isSpawningActive) return;
        if (_isWaitingToSpawn) return;

        _currentCustomer = null;

        if (_spawnCoroutine != null)
            StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = StartCoroutine(CustomerCycle());
    }

    private IEnumerator CustomerCycle()
    {
        _isWaitingToSpawn = true;

        yield return new WaitForSeconds(dayNightTransitionDelay);

        bool currentIsDay = DayNightManager.Instance.IsDay;
        bool newIsDay = !currentIsDay;
        DayNightManager.Instance.CmdSetDayNight(newIsDay);

        float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
        yield return new WaitForSeconds(delay);

        SpawnCustomer();

        _isWaitingToSpawn = false;
        _spawnCoroutine = null;
    }

    private void SpawnCustomer()
    {
        if (!IsServer) return;
        if (!_isSpawningActive) return;

        bool isDay = DayNightManager.Instance.IsDay;
        GameObject[] prefabs = isDay ? dayPrefabs : nightPrefabs;

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[CustomerSpawnManager] Нет доступных префабов посетителей");
            return;
        }

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        GameObject newCustomer = Instantiate(prefab, spawnPos, Quaternion.identity);
        InstanceFinder.ServerManager.Spawn(newCustomer, null, gameObject.scene);

        Rigidbody rb = newCustomer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("[CustomerSpawnManager] У префаба посетителя отсутствует Rigidbody, гравитация не будет действовать");
        }

        _currentCustomer = newCustomer;
        Debug.Log($"[CustomerSpawnManager] Создан посетитель: {prefab.name}, сцена: {newCustomer.scene.name}");
    }

    private Vector3 GetSpawnPosition()
    {
        return spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }
}