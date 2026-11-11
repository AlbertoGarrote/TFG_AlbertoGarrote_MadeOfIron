using System.Collections.Generic;
using UnityEngine;

public class GridSaveLoadManager : MonoBehaviour
{
    public static GridSaveLoadManager Instance { get; private set; }

    private GridPlacementManager placementManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Registrar en el ServiceLocator
        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<GridSaveLoadManager>();
        }
    }

    private GridPlacementManager GetPlacementManager()
    {
        if (placementManager != null) return placementManager;

        placementManager = ServiceLocator.Get<GridPlacementManager>();
        return placementManager;
    }

    // ==========================================
    // GUARDAR AUTOMÁTICO (Vinculado al AccountManager)
    // ==========================================
    public void GuardarBaseDelUsuario()
    {
        var accountManager = ServiceLocator.Get<AccountManager>();
        if (accountManager == null || accountManager.CurrentPlayerData == null)
        {
            Debug.LogWarning("[GridSaveLoadManager] No hay una sesión de usuario activa para guardar los datos.");
            return;
        }

        GridPlacementManager pManager = GetPlacementManager();
        if (pManager != null)
        {
            // Extraemos los edificios colocados en la cuadrícula
            GridSaveData currentGrid = pManager.ExportGridState();
            accountManager.CurrentPlayerData.placedBuildings = currentGrid.placedBuildings;
        }

        // Llamamos al AccountManager para que persista los cambios en el JSON del usuario
        accountManager.SaveCurrentPlayerData();
        Debug.Log("[GridSaveLoadManager] Base y recursos guardados automáticamente en el perfil del usuario.");
    }

    // ==========================================
    // CARGAR AUTOMÁTICO (Vinculado al AccountManager)
    // ==========================================
    public void CargarBaseDelUsuario()
    {
        var accountManager = ServiceLocator.Get<AccountManager>();
        if (accountManager == null || accountManager.CurrentPlayerData == null)
        {
            Debug.LogWarning("[GridSaveLoadManager] No se puede cargar la base: No hay usuario activo.");
            return;
        }

        GridPlacementManager pManager = GetPlacementManager();
        if (pManager == null)
        {
            Debug.LogError("[GridSaveLoadManager] No se encontró GridPlacementManager.");
            return;
        }

        // 1. Limpiar el mapa actual antes de cargar
        pManager.ClearGrid();

        List<PlacedBuildingData> savedBuildings = accountManager.CurrentPlayerData.placedBuildings;
        if (savedBuildings == null || savedBuildings.Count == 0)
        {
            Debug.Log("[GridSaveLoadManager] El usuario no tiene edificios guardados (base nueva).");
            return;
        }

        // 2. Mapear catálogo de ScriptableObjects para rápida búsqueda
        Dictionary<string, BuildingItemSO> catalogMap = new Dictionary<string, BuildingItemSO>();
        foreach (var item in pManager.buildingCatalog)
        {
            if (item == null) continue;
            string key = !string.IsNullOrEmpty(item.buildingId) ? item.buildingId : item.name;
            if (!catalogMap.ContainsKey(key))
            {
                catalogMap.Add(key, item);
            }
        }

        // 3. Instanciar los edificios según los datos del perfil
        int loadedCount = 0;
        foreach (PlacedBuildingData bData in savedBuildings)
        {
            if (catalogMap.TryGetValue(bData.buildingId, out BuildingItemSO buildingSO))
            {
                pManager.PlaceBuildingFromData(buildingSO, bData.gridX, bData.gridZ);
                loadedCount++;
            }
            else
            {
                Debug.LogWarning($"[GridSaveLoadManager] No se encontró el ScriptableObject con ID: '{bData.buildingId}' en el catálogo.");
            }
        }

        Debug.Log($"[GridSaveLoadManager] Base cargada con éxito para '{accountManager.CurrentPlayerData.username}'. Edificios generados: {loadedCount}");
    }
}