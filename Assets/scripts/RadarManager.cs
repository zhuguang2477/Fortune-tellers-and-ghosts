using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using System.Collections.Generic;

public class RadarManager : NetworkBehaviour
{
    [Header("Параметры радара")]
    public int radarHalfSize = 10;
    public LayerMask detectableLayers;

    [ServerRpc(RequireOwnership = false)]
    public void RequestRadarData(NetworkConnection requester)
    {
        if (!IsServer) return;

        GameObject ghost = RoleManager.Instance?.GetPlayerByRole(RoleManager.Role.Gadalka);
        if (ghost == null)
        {
            Debug.LogError("[RadarManager] Не найден игрок-призрак! Радар будет использовать позицию запрашивающего как центр (это может быть ошибочно)");
            GameObject requesterObj = requester.FirstObject?.gameObject;
            if (requesterObj != null)
                ghost = requesterObj;
            else
                return;
        }

        Vector3 ghostPos = ghost.transform.position;
        Debug.Log($"[RadarManager] Позиция призрака: {ghostPos}, объект призрака: {ghost.name}");

        bool[,] grid = MapData.Instance.GetLocalGrid(ghostPos, radarHalfSize, out Vector2Int centerGrid);

        List<RadarDynamicObject> dynamicObjects = new List<RadarDynamicObject>();

        dynamicObjects.Add(new RadarDynamicObject
        {
            gridPos = Vector2Int.zero,
            type = RadarObjectType.Ghost,
            color = Color.green
        });

        float worldRadius = radarHalfSize * MapData.Instance.gridSize;
        Collider[] hits = Physics.OverlapSphere(ghostPos, worldRadius, detectableLayers);
        foreach (var col in hits)
        {
            GameObject obj = col.gameObject;
            if (obj == ghost) continue;

            RadarTag tag = obj.GetComponent<RadarTag>();
            if (tag != null)
            {
                Vector2Int objGrid = MapData.Instance.WorldToGrid(obj.transform.position);
                Vector2Int localOffset = objGrid - centerGrid;
                if (Mathf.Abs(localOffset.x) <= radarHalfSize && Mathf.Abs(localOffset.y) <= radarHalfSize)
                {
                    dynamicObjects.Add(new RadarDynamicObject
                    {
                        gridPos = localOffset,
                        type = tag.radarType,
                        color = tag.color
                    });
                }
            }
        }

        int totalSize = radarHalfSize * 2 + 1;
        byte[] gridBytes = new byte[totalSize * totalSize];
        for (int i = 0; i < totalSize; i++)
        {
            for (int j = 0; j < totalSize; j++)
            {
                gridBytes[i * totalSize + j] = (byte)(grid[i, j] ? 1 : 0);
            }
        }

        RadarData data = new RadarData
        {
            gridData = gridBytes,
            halfSize = radarHalfSize,
            dynamicObjects = dynamicObjects,
            centerGrid = centerGrid
        };

        TargetReceiveRadarData(requester, data);
    }

    [TargetRpc]
    private void TargetReceiveRadarData(NetworkConnection target, RadarData data)
    {
        RadarUI.Instance?.UpdateRadar(data);
    }
}