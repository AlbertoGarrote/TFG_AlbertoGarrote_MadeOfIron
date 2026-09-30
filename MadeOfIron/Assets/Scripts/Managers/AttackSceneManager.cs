using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class AttackSceneManager : MonoBehaviour
{
    [Header("Referencias Locales")]
    public GridPlacementManager placementManager;
    public NavMeshSurface navMeshSurface;

    [Header("Spawneo de Unidades")]
    public GameObject unitPrefab; // Asigna aquí el prefab de tu unidad de ataque

    private IEnumerator Start()
    {
        // Esperamos un frame para asegurar la inicialización de los componentes
        yield return null;

        string jsonToLoad = "";

        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.BaseToAttackJson))
        {
            jsonToLoad = GameManager.Instance.BaseToAttackJson;
            Debug.Log("[AttackSceneManager] JSON recibido del GameManager.");
        }
        else
        {
            Debug.LogWarning("[AttackSceneManager] No hay JSON guardado en GameManager.");
        }

        // 1. Cargar la base
        if (!string.IsNullOrEmpty(jsonToLoad))
        {
            LoadBaseFromJSON(jsonToLoad);
        }

        // 2. Desactivar controles de edición
        if (placementManager != null)
        {
            placementManager.enabled = false;
        }

        // 3. Bake del NavMesh
        BakeNavMesh();

        // 4. Spawnear la unidad en el centro sobre el NavMesh recién calculado
        SpawnUnitAtCenter();
    }

    private void LoadBaseFromJSON(string jsonContent)
    {
        if (placementManager == null) return;

        GridSaveData saveData = JsonUtility.FromJson<GridSaveData>(jsonContent);
        if (saveData == null || saveData.placedBuildings == null) return;

        placementManager.ClearGrid();

        var catalogMap = new Dictionary<string, BuildingItemSO>();
        foreach (var item in placementManager.buildingCatalog)
        {
            if (item == null) continue;
            string key = !string.IsNullOrEmpty(item.buildingId) ? item.buildingId : item.name;
            if (!catalogMap.ContainsKey(key)) catalogMap.Add(key, item);
        }

        foreach (var bData in saveData.placedBuildings)
        {
            if (catalogMap.TryGetValue(bData.buildingId, out BuildingItemSO buildingSO))
            {
                placementManager.PlaceBuildingFromData(buildingSO, bData.gridX, bData.gridZ);
            }
        }
    }

    private void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            // Opcional: Ajustes de precisión para pasillos de 1 celda
            navMeshSurface.overrideTileSize = true;
            navMeshSurface.tileSize = 256;
            navMeshSurface.overrideVoxelSize = true;
            navMeshSurface.voxelSize = 0.1f;

            navMeshSurface.BuildNavMesh();
            Debug.Log("[AttackSceneManager] NavMesh horneado.");
        }
    }

    private void SpawnUnitAtCenter()
    {
        if (unitPrefab == null)
        {
            Debug.LogWarning("[AttackSceneManager] No se ha asignado 'unitPrefab' en el Inspector.");
            return;
        }

        // Buscamos el centro aproximado de la matriz (asumiendo matriz de 50x50 o usando el origen)
        Vector3 approximateCenter = Vector3.zero;
        if (placementManager != null)
        {
            // Calculamos el centro según el tamaño del grid si está disponible
            float centerOffset = (50 * placementManager.cellSize) / 2f;
            approximateCenter = new Vector3(centerOffset, 0f, centerOffset);
        }

        // Buscamos el punto navegable más cercano al centro dentro de un radio razonable
        NavMeshHit hit;
        if (NavMesh.SamplePosition(approximateCenter, out hit, 10.0f, NavMesh.AllAreas))
        {
            GameObject spawnedUnit = Instantiate(unitPrefab, hit.position, Quaternion.identity);
            Debug.Log($"[AttackSceneManager] Unidad spawnada exitosamente en el NavMesh en: {hit.position}");
        }
        else
        {
            Debug.LogError("[AttackSceneManager] No se encontró ningún punto válido en el NavMesh cerca del centro.");
        }
    }
}