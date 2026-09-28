using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class AttackSceneManager : MonoBehaviour
{
    [Header("Referencias Locales de la Escena de Ataque")]
    public GridPlacementManager placementManager;
    public NavMeshSurface navMeshSurface;

    private IEnumerator Start()
    {
        // Esperamos un frame para garantizar que el Singleton de GameManager y los Awake hayan terminado
        yield return null;

        string jsonToLoad = "";

        // 1. Intentamos obtener el JSON que traemos desde GameManager
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.BaseToAttackJson))
        {
            jsonToLoad = GameManager.Instance.BaseToAttackJson;
            Debug.Log("[AttackSceneManager] JSON recibido correctamente del GameManager.");
        }
        else
        {
            Debug.LogWarning("[AttackSceneManager] No se encontró GameManager o el JSON estaba vacío.");
        }

        // 2. Cargar los edificios en la grilla
        if (!string.IsNullOrEmpty(jsonToLoad))
        {
            LoadBaseFromJSON(jsonToLoad);
        }

        // Desactivar controles de edición en la escena de ataque
        if (placementManager != null)
        {
            placementManager.enabled = false;
        }

        // 3. Generar el NavMesh una vez que los edificios existen físicamente
        BakeNavMesh();
    }

    private void LoadBaseFromJSON(string jsonContent)
    {
        if (placementManager == null)
        {
            Debug.LogError("[AttackSceneManager] Falta asignar el GridPlacementManager en el Inspector.");
            return;
        }

        GridSaveData saveData = JsonUtility.FromJson<GridSaveData>(jsonContent);

        if (saveData == null || saveData.placedBuildings == null)
        {
            Debug.LogError("[AttackSceneManager] Error al deserializar el JSON.");
            return;
        }

        // Limpiar la grilla por si acaso
        placementManager.ClearGrid();

        // Mapear catálogo por ID/Nombre
        var catalogMap = new Dictionary<string, BuildingItemSO>();
        foreach (var item in placementManager.buildingCatalog)
        {
            if (item == null) continue;
            string key = !string.IsNullOrEmpty(item.buildingId) ? item.buildingId : item.name;
            if (!catalogMap.ContainsKey(key))
            {
                catalogMap.Add(key, item);
            }
        }

        // Instanciar cada edificio
        int spawnedCount = 0;
        foreach (var bData in saveData.placedBuildings)
        {
            if (catalogMap.TryGetValue(bData.buildingId, out BuildingItemSO buildingSO))
            {
                placementManager.PlaceBuildingFromData(buildingSO, bData.gridX, bData.gridZ);
                spawnedCount++;
            }
            else
            {
                Debug.LogWarning($"[AttackSceneManager] ID no encontrado en catálogo: {bData.buildingId}");
            }
        }

        Debug.Log($"[AttackSceneManager] Base cargada exitosamente. Edificios generados: {spawnedCount}");
    }

    private void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("[AttackSceneManager] ¡NavMesh horneado (Bake) con éxito!");
        }
        else
        {
            Debug.LogError("[AttackSceneManager] Asigna la NavMeshSurface en el Inspector.");
        }
    }
}