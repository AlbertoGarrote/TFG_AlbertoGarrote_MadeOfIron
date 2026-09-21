using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridSaveLoadManager : MonoBehaviour
{
    [Header("Referencias")]
    public GridPlacementManager placementManager;

    // Ruta por defecto donde se guardará/leerá el JSON en build o editor
    private string DefaultFilePath => Path.Combine(Application.persistentDataPath, "grid_layout.json");

    // ==========================================
    // EXPORTAR A JSON
    // ==========================================
    public void ExportGridToJSON()
    {
        if (placementManager == null)
        {
            Debug.LogError("[SaveLoad] Falta la referencia a GridPlacementManager.");
            return;
        }

        GridSaveData saveData = placementManager.ExportGridState();
        string jsonOutput = JsonUtility.ToJson(saveData, true);

        // Opción 1: Guardar en Editor mediante diálogo de archivos
#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanel("Guardar Mapa JSON", "", "grid_layout.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, jsonOutput);
            Debug.Log($"[SaveLoad] Archivo JSON exportado con éxito en: {path}");
        }
#else
        // Opción 2: En Build guardamos en la ruta persistente por defecto
        File.WriteAllText(DefaultFilePath, jsonOutput);
        Debug.Log($"[SaveLoad] Archivo JSON guardado en: {DefaultFilePath}");
#endif

        Debug.Log("<b>Contenido del JSON generado:</b>\n" + jsonOutput);
    }

    // ==========================================
    // IMPORTAR Y REGENERAR DESDE JSON
    // ==========================================
    public void ImportGridFromJSON()
    {
        if (placementManager == null)
        {
            Debug.LogError("[SaveLoad] Falta la referencia a GridPlacementManager.");
            return;
        }

        string jsonContent = "";

#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Cargar Mapa JSON", "", "json");
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            jsonContent = File.ReadAllText(path);
        }
        else
        {
            return;
        }
#else
        if (File.Exists(DefaultFilePath))
        {
            jsonContent = File.ReadAllText(DefaultFilePath);
        }
        else
        {
            Debug.LogWarning("[SaveLoad] No se encontró el archivo JSON en: " + DefaultFilePath);
            return;
        }
#endif

        if (string.IsNullOrEmpty(jsonContent)) return;

        GridSaveData saveData = JsonUtility.FromJson<GridSaveData>(jsonContent);

        if (saveData == null || saveData.placedBuildings == null)
        {
            Debug.LogError("[SaveLoad] Formato de JSON inválido o corrupto.");
            return;
        }

        // 1. Limpiar el mapa actual antes de cargar
        placementManager.ClearGrid();

        // 2. Mapear catálogo de ScriptableObjects para rápida búsqueda por ID o nombre
        Dictionary<string, BuildingItemSO> catalogMap = new Dictionary<string, BuildingItemSO>();
        foreach (var item in placementManager.buildingCatalog)
        {
            if (item == null) continue;
            string key = !string.IsNullOrEmpty(item.buildingId) ? item.buildingId : item.name;
            if (!catalogMap.ContainsKey(key))
            {
                catalogMap.Add(key, item);
            }
        }

        // 3. Instanciar los edificios según los datos del JSON
        int loadedCount = 0;
        foreach (PlacedBuildingData bData in saveData.placedBuildings)
        {
            if (catalogMap.TryGetValue(bData.buildingId, out BuildingItemSO buildingSO))
            {
                placementManager.PlaceBuildingFromData(buildingSO, bData.gridX, bData.gridZ);
                loadedCount++;
            }
            else
            {
                Debug.LogWarning($"[SaveLoad] No se encontró el ScriptableObject con ID: '{bData.buildingId}' en el catálogo.");
            }
        }

        Debug.Log($"[SaveLoad] Carga completada. Se generaron {loadedCount} edificios.");
    }
}