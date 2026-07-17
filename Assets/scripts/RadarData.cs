using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RadarData
{
    public byte[] gridData;
    public int halfSize;
    public List<RadarDynamicObject> dynamicObjects;
    public Vector2Int centerGrid;
}

[System.Serializable]
public class RadarDynamicObject
{
    public Vector2Int gridPos;
    public RadarObjectType type;
    public Color color;
}

public enum RadarObjectType
{
    Ghost,
    Key,
    Obstacle,
    Door,
    Default
}