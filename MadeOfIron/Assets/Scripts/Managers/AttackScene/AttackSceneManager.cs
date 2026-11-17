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
    public string idTropa;
    public GameObject prefab;
}

public class AttackSceneManager : MonoBehaviour
{
    [Header("Catálogo de Prefabs de Tropas para la Batalla")]
    [SerializeField] private List<TropaPrefabData> catalogoPrefabsTropas = new List<TropaPrefabData>();

    [Header("UI de Batalla y Progreso")]
    [SerializeField] private TextMeshProUGUI textoTemporizador;
    [SerializeField] private TextMeshProUGUI textoPorcentajeDestruccion; // Texto UI para mostrar el % ej: "Destrucción: 45%"
    [SerializeField] private GameObject panelFinBatalla;
    [SerializeField] private TextMeshProUGUI textoResultadoFinBatalla;  // Opcional: Para mostrar "¡Victoria!" o "Derrota / Tiempo Agotado"
    [SerializeField] private float duracionBatallaSegundos = 60f;

    private float tiempoRestante;
    private bool batallaTerminada = false;

    // Control de destrucción
    private int totalEdificiosObjetivo = 0;
    private int edificiosDestruidos = 0;

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

        // 1. Horneamos el NavMesh PRIMERO con el suelo limpio
        BakeNavMesh();

        // 2. Cargar la base enemiga desde el JSON guardado en GameManager
        string jsonToLoad = "";
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null && !string.IsNullOrEmpty(gameManager.BaseToAttackJson))
        {
            jsonToLoad = gameManager.BaseToAttackJson;
        }

        if (!string.IsNullOrEmpty(jsonToLoad))
        {
            LoadBaseFromJSON(jsonToLoad);
        }

        // 3. Contar cuántos edificios válidos (excluyendo muros) hay en la base cargada
        CalcularEdificiosObjetivo();

        // 4. Desactivar controles de edición
        GridPlacementManager pManager = GetPlacementManager();
        if (pManager != null)
        {
            pManager.enabled = false;
        }

        // 5. Spawnear las tropas seleccionadas
        SpawnUnitsFromSelection();

        // 6. Iniciar el temporizador de la batalla
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

    private void CalcularEdificiosObjetivo()
    {
        totalEdificiosObjetivo = 0;
        edificiosDestruidos = 0;

        // Buscamos todos los edificios instanciados en la escena con BuildingHealth
        BuildingHealth[] todosLosEdificios = FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None);

        foreach (var edificio in todosLosEdificios)
        {
            BuildingObject bObj = edificio.GetComponent<BuildingObject>();
            if (bObj != null && bObj.buildingData != null)
            {
                // Si el edificio NO ignora el porcentaje (ej. no es un muro), cuenta para la victoria
                if (!bObj.buildingData.ignoraPorcentajeDestruccion)
                {
                    totalEdificiosObjetivo++;

                    // Nos suscribimos al evento de destrucción de cada edificio
                    edificio.OnBuildingDestroyed += RegistrarEdificioDestruido;
                }
            }
        }

        Debug.Log($"[AttackSceneManager] Edificios totales a destruir para el 100%: {totalEdificiosObjetivo}");
        ActualizarUITextoPorcentaje();
    }

    private void RegistrarEdificioDestruido(BuildingHealth edificioRoto)
    {
        // Desuscribir para evitar fugas de memoria
        edificioRoto.OnBuildingDestroyed -= RegistrarEdificioDestruido;

        edificiosDestruidos++;
        ActualizarUITextoPorcentaje();

        // Comprobar si se ha alcanzado el 100% de destrucción
        if (totalEdificiosObjetivo > 0 && edificiosDestruidos >= totalEdificiosObjetivo)
        {
            CompletarVictoria();
        }
    }

    private void ActualizarUITextoPorcentaje()
    {
        if (textoPorcentajeDestruccion != null)
        {
            float porcentaje = 0f;
            if (totalEdificiosObjetivo > 0)
            {
                porcentaje = ((float)edificiosDestruidos / totalEdificiosObjetivo) * 100f;
            }
            textoPorcentajeDestruccion.text = $"Destrucción: {Mathf.RoundToInt(porcentaje)}%";
        }
    }

    private void CompletarVictoria()
    {
        if (batallaTerminada) return;
        batallaTerminada = true;

        Debug.Log("[AttackSceneManager] ¡Base enemiga destruida al 100%! Victoria.");

        if (textoResultadoFinBatalla != null)
        {
            textoResultadoFinBatalla.text = "¡Victoria Total!";
        }

        FinalizarBatallaVisuales();
    }

    private void BakeNavMesh()
    {
        NavMeshSurface surface = GetNavMeshSurface();
        if (surface != null)
        {
            surface.overrideTileSize = true;
            surface.tileSize = 256;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.1f;
            surface.BuildNavMesh();
        }
    }

    private void SpawnUnitsFromSelection()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.tropasSeleccionadasParaAtaque.Count == 0) return;

        Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
        foreach (var item in catalogoPrefabsTropas)
        {
            if (!prefabMap.ContainsKey(item.idTropa)) prefabMap.Add(item.idTropa, item.prefab);
        }

        var pManager = GetPlacementManager();
        Vector3 spawnCenter = Vector3.zero;
        float spawnAreaSizeX = 3f;
        float spawnAreaSizeZ = 3f;

        if (pManager != null && pManager.restrictSpawnArea)
        {
            float minX = pManager.spawnBlockStartX * pManager.cellSize;
            float minZ = pManager.spawnBlockStartZ * pManager.cellSize;
            float maxX = (pManager.spawnBlockStartX + pManager.spawnBlockWidth) * pManager.cellSize;
            float maxZ = (pManager.spawnBlockStartZ + pManager.spawnBlockLength) * pManager.cellSize;

            spawnCenter = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
            spawnAreaSizeX = (maxX - minX) * 0.4f;
            spawnAreaSizeZ = (maxZ - minZ) * 0.4f;
        }

        foreach (KeyValuePair<string, int> pair in gameManager.tropasSeleccionadasParaAtaque)
        {
            string idTropa = pair.Key;
            int cantidad = pair.Value;

            if (prefabMap.TryGetValue(idTropa, out GameObject prefabTropa))
            {
                for (int i = 0; i < cantidad; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-spawnAreaSizeX, spawnAreaSizeX), 0f, Random.Range(-spawnAreaSizeZ, spawnAreaSizeZ));

                    if (NavMesh.SamplePosition(spawnCenter + randomOffset, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
                    {
                        Instantiate(prefabTropa, hit.position, Quaternion.identity);
                    }
                    else
                    {
                        Instantiate(prefabTropa, spawnCenter, Quaternion.identity);
                    }
                }
            }
        }

        var selectionManager = ServiceLocator.Get<SelectionManager>();
        if (selectionManager != null) selectionManager.RefreshUnitList();
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

        if (!batallaTerminada)
        {
            // Tiempo agotado sin llegar al 100%
            batallaTerminada = true;
            if (textoResultadoFinBatalla != null)
            {
                textoResultadoFinBatalla.text = "¡Tiempo Agotado!";
            }
            FinalizarBatallaVisuales();
        }
    }

    private void FinalizarBatallaVisuales()
    {
        if (panelFinBatalla != null)
        {
            panelFinBatalla.SetActive(true);
        }
    }

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