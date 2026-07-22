using UnityEngine;
using System.Collections.Generic;

public class MapData : MonoBehaviour
{
    public static MapData Instance { get; private set; }

    [Header("Размер карты")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float gridSize = 1f;

    private bool[,] walkableGrid;

    private void Awake()
    {
        Instance = this;
        GenerateMapData();
    }

    private void GenerateMapData()
    {
        walkableGrid = new bool[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                walkableGrid[x, y] = true;

        RadarTag[] tags = FindObjectsOfType<RadarTag>();
        foreach (var tag in tags)
        {
            if (tag.radarType == RadarObjectType.Obstacle)
            {
                Vector2Int gridPos = WorldToGrid(tag.transform.position);
                if (gridPos.x >= 0 && gridPos.x < mapWidth && gridPos.y >= 0 && gridPos.y < mapHeight)
                    walkableGrid[gridPos.x, gridPos.y] = false;
            }
        }
    }

    public bool[,] GetLocalGrid(Vector3 center, int halfSize, out Vector2Int centerGrid)
    {
        centerGrid = WorldToGrid(center);
        int total = halfSize * 2 + 1;
        bool[,] localGrid = new bool[total, total];

        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int z = -halfSize; z <= halfSize; z++)
            {
                int gridX = centerGrid.x + x;
                int gridZ = centerGrid.y + z;
                bool walkable = true;
                if (gridX >= 0 && gridX < mapWidth && gridZ >= 0 && gridZ < mapHeight)
                    walkable = walkableGrid[gridX, gridZ];
                localGrid[x + halfSize, z + halfSize] = walkable;
            }
        }
        return localGrid;
    }

    public Vector2Int WorldToGrid(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / gridSize);
        int z = Mathf.RoundToInt(pos.z / gridSize);
        return new Vector2Int(x, z);
    }
}