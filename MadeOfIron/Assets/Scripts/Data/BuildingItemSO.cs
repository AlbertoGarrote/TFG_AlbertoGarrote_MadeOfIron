using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingItem", menuName = "RTS/Building Item")]
public class BuildingItemSO : ScriptableObject
{
    [Header("Información Básica")]
    public string buildingName = "Nuevo Edificio";
    public Sprite icon; // Para mostrar en la tienda UI

    [Header("Dimensiones en Cuadrícula")]
    public int width = 1;  // Eje X
    public int length = 1; // Eje Z

    [Header("Modelo 3D")]
    public GameObject prefab;

    [Header("Recursos y Costes (Futuro)")]
    public int goldCost = 100;
    public int elixirCost = 0;
    public float buildTimeSeconds = 10f;
}