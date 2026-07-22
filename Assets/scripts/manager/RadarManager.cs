using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RadarManager : NetworkBehaviour
{
    [Header("Параметры радара по умолчанию")]
    public int defaultHalfSize = 10;
    [Header("Слои для обнаружения")]
    public LayerMask detectableLayers;

    private int _currentHalfSize = 10;
    private NetworkConnection _boostUser = null;

    [ServerRpc(RequireOwnership = false)]
    public void RequestRadarData(NetworkConnection requester)
    {
        if (!IsServer) return;

        GameObject ghost = RoleManager.Instance?.GetPlayerByRole(RoleManager.Role.Gadalka);
        if (ghost == null)
        {
            GameObject requesterObj = requester.FirstObject?.gameObject;
            if (requesterObj != null)
                ghost = requesterObj;
            else
                return;
        }

        Vector3 ghostPos = ghost.transform.position;
        int halfSize = _currentHalfSize;

        bool[,] grid = MapData.Instance.GetLocalGrid(ghostPos, halfSize, out Vector2Int centerGrid);

        List<RadarDynamicObject> dynamicObjects = new List<RadarDynamicObject>();

        dynamicObjects.Add(new RadarDynamicObject
        {
            gridPos = Vector2Int.zero,
            type = RadarObjectType.Ghost,
            color = Color.green
        });

        float worldRadius = halfSize * MapData.Instance.gridSize;
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
                if (Mathf.Abs(localOffset.x) <= halfSize && Mathf.Abs(localOffset.y) <= halfSize)
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

        int totalSize = halfSize * 2 + 1;
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
            halfSize = halfSize,
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

    [ServerRpc(RequireOwnership = false)]
    public void CmdActivateRadarBoost(NetworkConnection sender = null)
    {
        if (sender == null) return;
        RoleManager.Role role = RoleManager.Instance.GetPlayerRole(sender);
        if (role != RoleManager.Role.Prizrak)
        {
            Debug.Log("[RadarManager] Не-призрак пытается активировать способность, отклонено");
            return;
        }

        if (_boostUser != null)
        {
            StopAllCoroutines();
            if (_boostUser != sender)
                TargetNotifyRadarBoost(_boostUser, false, 0);
        }

        _currentHalfSize = 15;
        _boostUser = sender;

        TargetNotifyRadarBoost(sender, true, 3f);

        StartCoroutine(RestoreRadarAfter(3f));
    }

    private IEnumerator RestoreRadarAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        _currentHalfSize = defaultHalfSize;
        if (_boostUser != null)
        {
            TargetNotifyRadarBoost(_boostUser, false, 0);
            _boostUser = null;
        }
    }

    [TargetRpc]
    private void TargetNotifyRadarBoost(NetworkConnection target, bool active, float duration)
    {
        RadarUI.Instance?.OnRadarBoost(active, duration);
    }
}