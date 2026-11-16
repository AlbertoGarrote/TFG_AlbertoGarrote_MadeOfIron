using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingItem", menuName = "Grid System/Building Item")]
public class BuildingItemSO : ScriptableObject
{
    [Header("Información Básica")]
    public string buildingId;
    public string buildingName = "Nuevo Edificio";
    public Sprite icon; // Para mostrar en la tienda UI

    [Header("Dimensiones en Cuadrícula")]
    public int width = 1;  // Eje X
    public int length = 1; // Eje Z

    [Header("Combate y Defensa")]
    public float maxHealth = 300f;

    [Header("Modelo 3D")]
    public GameObject prefab;

    [Header("Recursos y Costes (Futuro)")]
    public int cost = 100;
    public float buildTimeSeconds = 10f;
}