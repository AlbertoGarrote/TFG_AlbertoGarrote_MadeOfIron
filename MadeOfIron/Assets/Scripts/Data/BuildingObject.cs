using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using TMPro;

public class BuildingObject : MonoBehaviour
{
    [Header("Información del Edificio")]
    public BuildingItemSO buildingData;

    // Guardamos las coordenadas exactas de la grilla
    public int gridX { get; private set; }
    public int gridZ { get; private set; }

    private Canvas worldCanvas;
    private TextMeshProUGUI infoText;
    private Renderer[] renderers;
    private Color[] originalColors;

    public void Initialize(BuildingItemSO data, int startX, int startZ)
    {
        buildingData = data;
        gridX = startX;
        gridZ = startZ;

        // 1. Asignar capa "Building" automáticamente a todos los hijos
        int buildingLayerIndex = LayerMask.NameToLayer("Building");
        if (buildingLayerIndex != -1)
        {
            gameObject.layer = buildingLayerIndex;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = buildingLayerIndex;
            }
        }

        // 2. COMPROBAR SI ESTAMOS EN LA ESCENA DE ATAQUE
        string escenaActual = SceneManager.GetActiveScene().name;

        // CAMBIA "Attack" aquí abajo por el nombre exacto de tu escena de batalla si es diferente
        bool esEscenaDeAtaque = escenaActual.Contains("Attack") || escenaActual.Contains("Battle");

        if (esEscenaDeAtaque)
        {
            ConfigurarFisicasYCombateParaBatalla();
        }

        // 3. Guardar materiales originales para la selección
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else if (renderers[i].material.HasProperty("_BaseColor"))
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
        }

        CreateWorldTextUI();
    }

    private void ConfigurarFisicasYCombateParaBatalla()
    {
        if (buildingData == null) return;

        float cellSize = GridPlacementManager.Instance != null ? GridPlacementManager.Instance.cellSize : 1f;
        float totalWidth = buildingData.width * cellSize;
        float totalLength = buildingData.length * cellSize;

        // A) Asegurar que el edificio tiene un BoxCollider en el padre que cubre toda su huella
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol == null)
        {
            boxCol = gameObject.AddComponent<BoxCollider>();
        }

        if (buildingData.prefab == null)
        {
            float centerX = (gridX * cellSize) + (totalWidth / 2f);
            float centerZ = (gridZ * cellSize) + (totalLength / 2f);
            boxCol.center = new Vector3(centerX, 0.5f, centerZ);
        }
        else
        {
            boxCol.center = new Vector3(0f, 1f, 0f);
        }
        boxCol.size = new Vector3(totalWidth * 0.98f, 2f, totalLength * 0.98f);

        // B) Configurar NavMeshObstacle con Carve = true para que las tropas lo rodeen
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = boxCol.center;
        obstacle.size = boxCol.size;
        obstacle.carving = true;

        // C) Añadir componente de salud
        BuildingHealth health = GetComponent<BuildingHealth>();
        if (health == null)
        {
            health = gameObject.AddComponent<BuildingHealth>();
        }
        health.InitializeHealth(buildingData.maxHealth);

        // D) Añadir la barra de vida visual automática
        BuildingHealthBar healthBar = GetComponent<BuildingHealthBar>();
        if (healthBar == null)
        {
            gameObject.AddComponent<BuildingHealthBar>();
        }
    }

    private void CreateWorldTextUI()
    {
        // 1. Crear el objeto Canvas
        GameObject canvasObj = new GameObject("BuildingUI");
        canvasObj.transform.SetParent(transform);

        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;

        // Ajustar el tamaño y escala para World Space
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(5f, 2f);
        canvasObj.transform.localScale = Vector3.one;

        // 2. Posicionar el Canvas exactamente encima del centro geométrico del edificio
        PositionCanvasAboveBuilding();

        // 3. Crear el componente de Texto
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform, false);

        infoText = textObj.AddComponent<TextMeshProUGUI>();

        if (infoText.font == null)
        {
            infoText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        infoText.alignment = TextAlignmentOptions.Center;
        infoText.fontSize = 0.5f;
        infoText.color = Color.yellow;
        infoText.rectTransform.sizeDelta = new Vector2(5f, 2f);

        string nameText = buildingData != null ? buildingData.buildingName : "Edificio";
        int w = buildingData != null ? buildingData.width : 1;
        int l = buildingData != null ? buildingData.length : 1;

        infoText.text = $"<b>{nameText}</b>\nOcupa: {w}x{l}";

        // Ocultar al inicio
        canvasObj.SetActive(false);
    }

    private void PositionCanvasAboveBuilding()
    {
        if (worldCanvas == null) return;

        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer rend in renderers)
        {
            if (rend != null && rend != worldCanvas.GetComponent<Renderer>())
            {
                if (!hasBounds)
                {
                    combinedBounds = rend.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rend.bounds);
                }
            }
        }

        Vector3 topCenter = hasBounds
            ? new Vector3(combinedBounds.center.x, combinedBounds.max.y + 0.8f, combinedBounds.center.z)
            : transform.position + new Vector3(0, 2.5f, 0);

        worldCanvas.transform.position = topCenter;
    }

    public void SetSelected(bool isSelected, Material highlightMaterial = null)
    {
        if (worldCanvas != null)
        {
            worldCanvas.gameObject.SetActive(isSelected);

            if (isSelected)
            {
                PositionCanvasAboveBuilding();

                if (Camera.main != null)
                {
                    worldCanvas.transform.rotation = Quaternion.LookRotation(worldCanvas.transform.position - Camera.main.transform.position);
                }
            }
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            if (isSelected)
            {
                if (highlightMaterial != null)
                {
                    renderers[i].material = highlightMaterial;
                }
                else
                {
                    if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.color = Color.magenta;
                    else if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", Color.magenta);
                }
            }
            else
            {
                if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = originalColors[i];
                else if (renderers[i].material.HasProperty("_BaseColor"))
                    renderers[i].material.SetColor("_BaseColor", originalColors[i]);
            }
        }
    }
}