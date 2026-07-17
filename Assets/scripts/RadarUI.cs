using FishNet;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadarUI : MonoBehaviour
{
    public static RadarUI Instance { get; private set; }

    [Header("Настройки UI")]
    public RectTransform radarContainer;
    public GameObject gridCellPrefab;
    public GameObject dynamicIconPrefab;
    public int gridCellSize = 20;

    [Header("Частота обновления")]
    public float updateInterval = 0.3f;

    private GameObject[,] cellObjects;
    private List<GameObject> iconObjects = new List<GameObject>();
    private int currentHalfSize;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        gameObject.SetActive(false);
    }

    public void ShowRadar()
    {
        gameObject.SetActive(true);
        CancelInvoke(nameof(RequestUpdate));
        InvokeRepeating(nameof(RequestUpdate), 0f, updateInterval);
        RequestUpdate();
    }

    public void HideRadar()
    {
        gameObject.SetActive(false);
        CancelInvoke(nameof(RequestUpdate));
        ClearDynamicIcons();
    }

    private void RequestUpdate()
    {
        RadarManager radarManager = FindObjectOfType<RadarManager>();
        if (radarManager != null)
            radarManager.RequestRadarData(InstanceFinder.ClientManager.Connection);
        else
            Debug.LogWarning("RadarManager не найден");
    }

    public void UpdateRadar(RadarData data)
    {
        if (data == null || data.gridData == null || data.gridData.Length == 0)
        {
            Debug.LogWarning("Данные радара недействительны");
            return;
        }

        int halfSize = data.halfSize;
        int total = halfSize * 2 + 1;

        bool[,] grid = new bool[total, total];
        for (int i = 0; i < total; i++)
        {
            for (int j = 0; j < total; j++)
            {
                int index = i * total + j;
                grid[i, j] = (index < data.gridData.Length && data.gridData[index] == 1);
            }
        }

        if (cellObjects == null || halfSize != currentHalfSize)
        {
            currentHalfSize = halfSize;
            RebuildGrid(halfSize);
        }

        for (int x = 0; x < total; x++)
        {
            for (int y = 0; y < total; y++)
            {
                bool walkable = grid[x, y];
                int uiX = x - halfSize;
                int uiY = y - halfSize;
                if (uiX >= -halfSize && uiX <= halfSize && uiY >= -halfSize && uiY <= halfSize)
                {
                    Image img = cellObjects[uiX + halfSize, uiY + halfSize].GetComponent<Image>();
                    if (img != null)
                        img.color = walkable ? new Color(0.2f, 0.2f, 0.2f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }

        ClearDynamicIcons();
        if (data.dynamicObjects != null)
        {
            foreach (var dyn in data.dynamicObjects)
            {
                Vector2Int gridPos = dyn.gridPos;
                if (Mathf.Abs(gridPos.x) > halfSize || Mathf.Abs(gridPos.y) > halfSize) continue;

                GameObject icon = Instantiate(dynamicIconPrefab, radarContainer);
                icon.transform.localPosition = new Vector3(gridPos.x * gridCellSize, gridPos.y * gridCellSize, 0);
                Image iconImg = icon.GetComponent<Image>();
                if (iconImg != null) iconImg.color = dyn.color;
                iconObjects.Add(icon);
            }
        }
    }

    private void RebuildGrid(int halfSize)
    {
        if (cellObjects != null)
        {
            foreach (var go in cellObjects)
                if (go != null) Destroy(go);
        }

        int total = halfSize * 2 + 1;
        cellObjects = new GameObject[total, total];
        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                GameObject cell = Instantiate(gridCellPrefab, radarContainer);
                cell.transform.localPosition = new Vector3(x * gridCellSize, y * gridCellSize, 0);
                cellObjects[x + halfSize, y + halfSize] = cell;
            }
        }
    }

    private void ClearDynamicIcons()
    {
        foreach (var go in iconObjects)
            if (go != null) Destroy(go);
        iconObjects.Clear();
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(RequestUpdate));
        ClearDynamicIcons();
        if (cellObjects != null)
        {
            foreach (var go in cellObjects)
                if (go != null) Destroy(go);
        }
    }
}