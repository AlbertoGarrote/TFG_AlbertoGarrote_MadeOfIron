using System;
using System.Collections.Generic;

[Serializable]
public class PlacedBuildingData
{
    public string buildingId; // ID o nombre único del ScriptableObject
    public int gridX;         // Coordenada X en la matriz
    public int gridZ;         // Coordenada Z en la matriz
}

[Serializable]
public class GridSaveData
{
    public List<PlacedBuildingData> placedBuildings = new List<PlacedBuildingData>();
}