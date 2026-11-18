using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [SerializeField] private TextMeshProUGUI textoPorcentajeDestruccion;
    [SerializeField] private TextMeshProUGUI textoBotinActual;     // Cuánto llevas robado en tiempo real
    [SerializeField] private TextMeshProUGUI textoBotinMaximo;      // Máximo posible a robar (30%)

    [SerializeField] private GameObject panelFinBatalla;
    [SerializeField] private TextMeshProUGUI textoResultadoFinBatalla;
    [SerializeField] private TextMeshProUGUI textoBotiginRobadoFinal; // Total robado al terminar
    [SerializeField] private float duracionBatallaSegundos = 60f;

    [Header("Economía de Ataque Real")]
    private int recursosTotalesBaseEnemiga = 0; // Se extraerá del JSON del defensor
    private string usernameDefensor = "";     // Nombre del defensor para descontarle los recursos

    private float tiempoRestante;
    private bool batallaTerminada = false;

    private int totalEdificiosObjetivo = 0;
    private int edificiosDestruidos = 0;

    private int botinMaximoPosible = 0;

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

        yield return null;

        BakeNavMesh();

        string jsonToLoad = "";
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null && !string.IsNullOrEmpty(gameManager.BaseToAttackJson))
        {
            jsonToLoad = gameManager.BaseToAttackJson;
        }

        if (!string.IsNullOrEmpty(jsonToLoad))
        {
            LoadBaseAndEconomyFromJSON(jsonToLoad);
        }
        else
        {
            recursosTotalesBaseEnemiga = 2000; // Respaldo por defecto si no hay JSON
        }

        // Calculamos el botín máximo posible (30% de los recursos reales de la base enemiga)
        botinMaximoPosible = Mathf.RoundToInt(recursosTotalesBaseEnemiga * 0.30f);

        CalcularEdificiosObjetivo();

        GridPlacementManager pManager = GetPlacementManager();
        if (pManager != null)
        {
            pManager.enabled = false;
        }

        SpawnUnitsFromSelection();
        StartCoroutine(BucleTemporizador());
    }

    private void LoadBaseAndEconomyFromJSON(string jsonContent)
    {
        // 1. Extraer la economía y el nombre del defensor desde el JSON completo (PlayerData)
        try
        {
            PlayerData playerDataEnemigo = JsonUtility.FromJson<PlayerData>(jsonContent);
            if (playerDataEnemigo != null)
            {
                recursosTotalesBaseEnemiga = playerDataEnemigo.recursos;
                usernameDefensor = playerDataEnemigo.username;
                Debug.Log($"[AttackSceneManager] Base enemiga cargada de '{usernameDefensor}'. Recursos totales del rival: {recursosTotalesBaseEnemiga}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AttackSceneManager] Error al leer los datos de economía del enemigo: {e.Message}");
            recursosTotalesBaseEnemiga = 2000;
        }

        // 2. Instanciar los edificios en la grilla usando la estructura de guardado
        GridPlacementManager pManager = GetPlacementManager();
        if (pManager == null) return;

        GridSaveData saveDataGrid = JsonUtility.FromJson<GridSaveData>(jsonContent);
        if (saveDataGrid == null || saveDataGrid.placedBuildings == null) return;

        pManager.ClearGrid();

        var catalogMap = new Dictionary<string, BuildingItemSO>();
        foreach (var item in pManager.buildingCatalog)
        {
            if (item == null) continue;
            string key = !string.IsNullOrEmpty(item.buildingId) ? item.buildingId : item.name;
            if (!catalogMap.ContainsKey(key)) catalogMap.Add(key, item);
        }

        foreach (var bData in saveDataGrid.placedBuildings)
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

        BuildingHealth[] todosLosEdificios = FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None);

        foreach (var edificio in todosLosEdificios)
        {
            BuildingObject bObj = edificio.GetComponent<BuildingObject>();
            if (bObj != null && bObj.buildingData != null)
            {
                if (!bObj.buildingData.ignoraPorcentajeDestruccion)
                {
                    totalEdificiosObjetivo++;
                    edificio.OnBuildingDestroyed += RegistrarEdificioDestruido;
                }
            }
        }

        ActualizarUIProgresoYBotin();
    }

    private void RegistrarEdificioDestruido(BuildingHealth edificioRoto)
    {
        edificioRoto.OnBuildingDestroyed -= RegistrarEdificioDestruido;
        edificiosDestruidos++;
        ActualizarUIProgresoYBotin();

        if (totalEdificiosObjetivo > 0 && edificiosDestruidos >= totalEdificiosObjetivo)
        {
            CompletarVictoria();
        }
    }

    private float ObtenerPorcentajeActual()
    {
        if (totalEdificiosObjetivo <= 0) return 0f;
        return ((float)edificiosDestruidos / totalEdificiosObjetivo) * 100f;
    }

    private int CalcularBotinActual()
    {
        float porcentaje = ObtenerPorcentajeActual();
        // Escala proporcionalmente al % de destrucción respecto al máximo del 30% de los recursos del enemigo
        float proporcionRobo = (porcentaje / 100f) * 0.30f;
        return Mathf.RoundToInt(recursosTotalesBaseEnemiga * proporcionRobo);
    }

    private void ActualizarUIProgresoYBotin()
    {
        float porcentaje = ObtenerPorcentajeActual();
        int botinActual = CalcularBotinActual();

        if (textoPorcentajeDestruccion != null)
        {
            textoPorcentajeDestruccion.text = $"Destrucción: {Mathf.RoundToInt(porcentaje)}%";
        }

        if (textoBotinActual != null)
        {
            textoBotinActual.text = $"Robado: {botinActual}";
        }

        if (textoBotinMaximo != null)
        {
            textoBotinMaximo.text = $"Máximo a robar: {botinMaximoPosible}";
        }
    }

    private void CompletarVictoria()
    {
        if (batallaTerminada) return;
        batallaTerminada = true;

        if (textoResultadoFinBatalla != null)
        {
            textoResultadoFinBatalla.text = "¡Victoria Total!";
        }

        FinalizarBatallaConBotin();
    }

    private void FinalizarBatallaConBotin()
    {
        int recursosRobados = CalcularBotinActual();

        // 1. Sumar los recursos al jugador atacante actual a través del GameManager
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.AñadirRecursos(recursosRobados);
        }

        // 2. Restar los recursos robados al archivo JSON individual del jugador defensor
        if (!string.IsNullOrEmpty(usernameDefensor) && recursosRobados > 0)
        {
            RestarRecursosAJugadorDefensor(usernameDefensor, recursosRobados);
        }

        // 3. Mostrar el resultado en el panel de fin de batalla
        if (textoBotiginRobadoFinal != null)
        {
            textoBotiginRobadoFinal.text = $"¡Has conseguido {recursosRobados} recursos robados!";
        }

        if (panelFinBatalla != null)
        {
            panelFinBatalla.SetActive(true);
        }
    }

    private void RestarRecursosAJugadorDefensor(string username, int cantidadRestar)
    {
        string safeUsername = username.Trim().ToLower();
        string filePath = Path.Combine(Application.persistentDataPath, $"save_{safeUsername}.json");

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            PlayerData defensorData = JsonUtility.FromJson<PlayerData>(jsonContent);

            if (defensorData != null)
            {
                // Restamos asegurando que los recursos del defensor nunca bajen de 0
                defensorData.recursos = Mathf.Max(0, defensorData.recursos - cantidadRestar);

                // Guardamos los cambios en el archivo JSON del defensor
                string jsonOutput = JsonUtility.ToJson(defensorData, true);
                File.WriteAllText(filePath, jsonOutput);

                Debug.Log($"[AttackSceneManager] Se han descontado {cantidadRestar} recursos al defensor '{username}'. Le quedan: {defensorData.recursos}");
            }
        }
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
            batallaTerminada = true;
            if (textoResultadoFinBatalla != null)
            {
                textoResultadoFinBatalla.text = "¡Tiempo Agotado!";
            }
            FinalizarBatallaConBotin();
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