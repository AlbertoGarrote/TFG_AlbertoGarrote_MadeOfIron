using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct TropaPrefabData
{
    public string idTropa;      // Debe coincidir con el idTropa que pusiste en el ArmyManager
    public GameObject prefab;   // Prefab correspondiente para la batalla
}

public class AttackSceneManager : MonoBehaviour
{
    [Header("Catálogo de Prefabs de Tropas para la Batalla")]
    [SerializeField] private List<TropaPrefabData> catalogoPrefabsTropas = new List<TropaPrefabData>();

    [Header("UI de Batalla y Temporizador")]
    [SerializeField] private TextMeshProUGUI textoTemporizador;
    [SerializeField] private GameObject panelFinBatalla;
    [SerializeField] private float duracionBatallaSegundos = 60f; 

    private float tiempoRestante;
    private bool batallaTerminada = false;

    private GridPlacementManager GetPlacementManager()
    {
        return ServiceLocator.Get<GridPlacementManager>();
    }

    private NavMeshSurface GetNavMeshSurface()
    {
        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = FindFirstObjectByType<NavMeshSurface>();
        }
        return surface;
    }

    private IEnumerator Start()
    {
        if (panelFinBatalla != null) panelFinBatalla.SetActive(false);
        tiempoRestante = duracionBatallaSegundos;

        // Esperamos un frame para asegurar la inicialización de los componentes y el ServiceLocator
        yield return null;

        string jsonToLoad = "";

        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null && !string.IsNullOrEmpty(gameManager.BaseToAttackJson))
        {
            jsonToLoad = gameManager.BaseToAttackJson;
            Debug.Log("[AttackSceneManager] JSON recibido del GameManager.");
        }
        else
        {
            Debug.LogWarning("[AttackSceneManager] No hay JSON guardado en GameManager.");
        }

        // 1. Cargar la base enemiga
        if (!string.IsNullOrEmpty(jsonToLoad))
        {
            LoadBaseFromJSON(jsonToLoad);
        }

        // 2. Desactivar controles de edición
        GridPlacementManager pManager = GetPlacementManager();
        if (pManager != null)
        {
            pManager.enabled = false;
        }

        // 3. Bake del NavMesh
        BakeNavMesh();

        // 4. Spawnear las tropas exactas seleccionadas por el jugador
        SpawnUnitsFromSelection();

        // 5. Iniciar el temporizador de la batalla
        StartCoroutine(BucleTemporizador());
    }

    private void LoadBaseFromJSON(string jsonContent)
    {
        GridPlacementManager pManager = GetPlacementManager();
        if (pManager == null) return;

        GridSaveData saveData = JsonUtility.FromJson<GridSaveData>(jsonContent);
        if (saveData == null || saveData.placedBuildings == null) return;

        pManager.ClearGrid();

        var catalogMap = new Dictionary<string, BuildingItemSO>();
        foreach (var item in pManager.buildingCatalog)
        {
            if (item == null) continue;
            string key = !string.IsNullOrEmpty(item.buildingId) ? item.buildingId : item.name;
            if (!catalogMap.ContainsKey(key)) catalogMap.Add(key, item);
        }

        foreach (var bData in saveData.placedBuildings)
        {
            if (catalogMap.TryGetValue(bData.buildingId, out BuildingItemSO buildingSO))
            {
                pManager.PlaceBuildingFromData(buildingSO, bData.gridX, bData.gridZ);
            }
        }
    }

    private void BakeNavMesh()
    {
        NavMeshSurface surface = GetNavMeshSurface();
        if (surface != null)
        {
            // Ajustes de precisión para pasillos
            surface.overrideTileSize = true;
            surface.tileSize = 256;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.1f;

            surface.BuildNavMesh();
            Debug.Log("[AttackSceneManager] NavMesh horneado.");
        }
        else
        {
            Debug.LogWarning("[AttackSceneManager] No se encontró ningún componente NavMeshSurface en la escena de ataque.");
        }
    }

    private void SpawnUnitsFromSelection()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.tropasSeleccionadasParaAtaque.Count == 0)
        {
            Debug.LogWarning("[AttackSceneManager] No hay tropas seleccionadas para la batalla.");
            return;
        }

        // Crear un mapa rápido de ID -> Prefab usando el catálogo configurado en el inspector
        Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
        foreach (var item in catalogoPrefabsTropas)
        {
            if (!prefabMap.ContainsKey(item.idTropa)) prefabMap.Add(item.idTropa, item.prefab);
        }

        Vector3 approximateCenter = Vector3.zero;
        var pManager = GetPlacementManager();
        if (pManager != null)
        {
            float centerOffset = (50 * pManager.cellSize) / 2f;
            approximateCenter = new Vector3(centerOffset, 0f, centerOffset);
        }

        // Recorrer las tropas elegidas en el diccionario y spawnear la cantidad exacta
        foreach (KeyValuePair<string, int> pair in gameManager.tropasSeleccionadasParaAtaque)
        {
            string idTropa = pair.Key;
            int cantidad = pair.Value;

            if (prefabMap.TryGetValue(idTropa, out GameObject prefabTropa))
            {
                for (int i = 0; i < cantidad; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));

                    if (NavMesh.SamplePosition(approximateCenter + randomOffset, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
                    {
                        Instantiate(prefabTropa, hit.position, Quaternion.identity);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[AttackSceneManager] No se encontró el prefab en el catálogo para la tropa ID: '{idTropa}'");
            }
        }

        // Refrescar el SelectionManager para que reconozca a las unidades recién spawneadas
        var selectionManager = ServiceLocator.Get<SelectionManager>();
        if (selectionManager != null)
        {
            selectionManager.RefreshUnitList();
        }
    }

    private IEnumerator BucleTemporizador()
    {
        while (tiempoRestante > 0 && !batallaTerminada)
        {
            yield return new WaitForSeconds(1f);
            tiempoRestante -= 1f;

            if (textoTemporizador != null)
            {
                textoTemporizador.text = $"Tiempo: {Mathf.CeilToInt(tiempoRestante)}s";
            }
        }

        FinalizarBatalla();
    }

    private void FinalizarBatalla()
    {
        if (batallaTerminada) return;
        batallaTerminada = true;

        Debug.Log("[AttackSceneManager] ¡Tiempo agotado! Fin de la batalla.");

        if (panelFinBatalla != null)
        {
            panelFinBatalla.SetActive(true);
        }
    }

    /// <summary>
    /// Método público para asignar al botón "Volver a la Base" en el panel de fin de batalla.
    /// </summary>
    public void OnClickVolverABase()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToBuildMode("BaseScene");
        }
        else
        {
            SceneManager.LoadScene("BaseScene");
        }
    }
}