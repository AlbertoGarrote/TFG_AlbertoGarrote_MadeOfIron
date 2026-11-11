using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [Header("Información de Cuenta")]
    public string username;
    public string password; // Nota: En un proyecto real se encriptaría, pero para guardado local va bien en texto plano.

    [Header("Progresión del Jugador")]
    public int playerLevel = 1;
    public int currentExp = 0;

    [Header("Economía y Recursos")]
    public int recursos = 500; 

    [Header("Banderas de Estado")]
    public bool hasCompletedTutorial = false; 

    [Header("Datos de la Base (Layout)")]
    public List<PlacedBuildingData> placedBuildings = new List<PlacedBuildingData>();
}