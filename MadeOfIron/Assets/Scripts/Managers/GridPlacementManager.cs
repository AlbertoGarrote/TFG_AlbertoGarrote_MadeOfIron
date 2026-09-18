using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GridPlacementManager : MonoBehaviour
{
    [Header("Configuración de Cuadrícula")]
    public float cellSize = 1.0f;
    public int gridWidth = 50;
    public int gridLength = 50;

    [Header("Capas y Selección")]
    public LayerMask groundLayer;

    [Header("Catálogo de Edificios (ScriptableObjects)")]
    public List<BuildingItemSO> buildingCatalog = new List<BuildingItemSO>();

    [Header("Materiales para la Vista Previa (Ghost)")]
    public Material validPlacementMaterial;   // Material Verde Transparente
    public Material invalidPlacementMaterial; // Material Rojo Transparente

    private BuildingItemSO selectedBuilding;
    private Transform gridContainer;
    private bool[,] occupiedCells;

    // Control del Ghost/Preview
    private GameObject ghostContainer;
    private List<Renderer> ghostRenderers = new List<Renderer>();

    void Start()
    {
        GameObject containerObj = new GameObject("PlacedObjectsContainer");
        gridContainer = containerObj.transform;
        occupiedCells = new bool[gridWidth, gridLength];

        // Crear materiales por defecto en tiempo de ejecución si no se han asignado en el Inspector
        CreateDefaultMaterialsIfNeeded();

        // Seleccionar el primer edificio si existe
        if (buildingCatalog.Count > 0 && buildingCatalog[0] != null)
        {
            SelectBuildingIndex(0);
        }
    }

    void Update()
    {
        // Si no estamos en modo Edición, destruir la vista previa si existe y salir
        if (GameManager.Instance != null && !GameManager.Instance.IsEditing())
        {
            DestroyGhost();
            return;
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame) ClearGrid();

        // Actualizar la posición y estado del Ghost en cada frame
        UpdateGhostPreview(mouse.position.ReadValue());

        // Clic izquierdo para colocar el objeto
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryPlaceObject(mouse.position.ReadValue());
        }
    }

    public void SelectBuildingIndex(int index)
    {
        if (index >= 0 && index < buildingCatalog.Count)
        {
            selectedBuilding = buildingCatalog[index];
            Debug.Log($"[PlacementManager] Cambiado a: {selectedBuilding.buildingName} ({selectedBuilding.width}x{selectedBuilding.length})");

            // Recrear la vista previa para ajustar las dimensiones del nuevo edificio seleccionado
            RebuildGhost();
        }
        else
        {
            Debug.LogWarning($"[PlacementManager] Índice {index} fuera de rango.");
        }
    }

    // ==========================================
    // SISTEMA DE VISTA PREVIA (GHOST/PREVIEW)
    // ==========================================

    private void RebuildGhost()
    {
        DestroyGhost();

        if (selectedBuilding == null) return;

        ghostContainer = new GameObject("GhostPreview");
        ghostRenderers.Clear();

        int width = selectedBuilding.width;
        int length = selectedBuilding.length;

        // Rellenar con bloques semitransparentes para representar la huella del edificio
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                Vector3 localPos = new Vector3(
                    (x * cellSize) + (cellSize / 2.0f),
                    0.5f,
                    (z * cellSize) + (cellSize / 2.0f)
                );

                GameObject ghostCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghostCube.transform.SetParent(ghostContainer.transform);
                ghostCube.transform.localPosition = localPos;
                ghostCube.transform.localScale = Vector3.one * (cellSize * 0.98f);

                // Desactivar el Collider para que los Raycasts de colocación no impacten contra el propio Ghost
                if (ghostCube.TryGetComponent<Collider>(out Collider col))
                {
                    Destroy(col);
                }

                Renderer rend = ghostCube.GetComponent<Renderer>();
                if (rend != null)
                {
                    ghostRenderers.Add(rend);
                }
            }
        }
    }

    private void UpdateGhostPreview(Vector2 mouseScreenPosition)
    {
        if (ghostContainer == null || selectedBuilding == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            if (!ghostContainer.activeSelf) ghostContainer.SetActive(true);

            int width = selectedBuilding.width;
            int length = selectedBuilding.length;

            Vector2Int gridCoord = GetGridCoordinates(hit.point, width, length);
            bool canPlace = CanPlaceObject(gridCoord.x, gridCoord.y, width, length);

            // Posicionar el contenedor Ghost en el origen de las coordenadas en la cuadrícula
            ghostContainer.transform.position = new Vector3(gridCoord.x * cellSize, 0f, gridCoord.y * cellSize);

            // Aplicar material Verde o Rojo según si el espacio está disponible o bloqueado
            Material targetMaterial = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
            foreach (Renderer rend in ghostRenderers)
            {
                if (rend != null) rend.material = targetMaterial;
            }
        }
        else
        {
            // Ocultar si el ratón está fuera del suelo
            if (ghostContainer.activeSelf) ghostContainer.SetActive(false);
        }
    }

    private void DestroyGhost()
    {
        if (ghostContainer != null)
        {
            Destroy(ghostContainer);
            ghostContainer = null;
            ghostRenderers.Clear();
        }
    }

    private void CreateDefaultMaterialsIfNeeded()
    {
        // Si no se asignan en el Inspector, creamos materiales estándar con transparencia en tiempo de ejecución
        if (validPlacementMaterial == null)
        {
            validPlacementMaterial = new Material(Shader.Find("Standard"));
            validPlacementMaterial.color = new Color(0f, 1f, 0f, 0.4f); // Verde semitransparente
            SetMaterialTransparent(validPlacementMaterial);
        }

        if (invalidPlacementMaterial == null)
        {
            invalidPlacementMaterial = new Material(Shader.Find("Standard"));
            invalidPlacementMaterial.color = new Color(1f, 0f, 0f, 0.4f); // Rojo semitransparente
            SetMaterialTransparent(invalidPlacementMaterial);
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // ==========================================
    // LÓGICA DE COLOCACIÓN Y MUNDOS
    // ==========================================

    void TryPlaceObject(Vector2 mouseScreenPosition)
    {
        if (selectedBuilding == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            int width = selectedBuilding.width;
            int length = selectedBuilding.length;

            Vector2Int gridCoord = GetGridCoordinates(hit.point, width, length);

            if (CanPlaceObject(gridCoord.x, gridCoord.y, width, length))
            {
                if (selectedBuilding.prefab != null)
                {
                    Vector3 worldPos = CalculateWorldPosition(gridCoord.x, gridCoord.y, width, length);
                    GameObject newObject = Instantiate(selectedBuilding.prefab, worldPos, Quaternion.identity);
                    newObject.transform.SetParent(gridContainer);

                    // Adjuntar y configurar el script de selección
                    BuildingObject buildingComp = newObject.AddComponent<BuildingObject>();
                    buildingComp.Initialize(selectedBuilding);
                }
                else
                {
                    SpawnTemporaryCubes(gridCoord.x, gridCoord.y, width, length);
                }

                MarkCellsOccupied(gridCoord.x, gridCoord.y, width, length, true);

                // Actualizar inmediatamente el estado del Ghost tras colocar para que se ponga rojo
                UpdateGhostPreview(mouseScreenPosition);
            }
            else
            {
                Debug.LogWarning("[Placement] Espacio ocupado o fuera de límites.");
            }
        }
    }

    void SpawnTemporaryCubes(int startX, int startZ, int width, int length)
    {
        GameObject buildingGroup = new GameObject($"BuildingPrototipo_{width}x{length}");
        buildingGroup.transform.SetParent(gridContainer);

        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + length; z++)
            {
                Vector3 cubePos = new Vector3(
                    (x * cellSize) + (cellSize / 2.0f),
                    0.5f,
                    (z * cellSize) + (cellSize / 2.0f)
                );

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = cubePos;
                cube.transform.localScale = Vector3.one * (cellSize * 0.95f);
                cube.transform.SetParent(buildingGroup.transform);
            }
        }

        // Adjuntar y configurar el script de selección al grupo prototipo
        BuildingObject buildingComp = buildingGroup.AddComponent<BuildingObject>();
        buildingComp.Initialize(selectedBuilding);
    }

    Vector2Int GetGridCoordinates(Vector3 hitPoint, int width, int length)
    {
        int x = Mathf.FloorToInt(hitPoint.x / cellSize) - (width - 1) / 2;
        int z = Mathf.FloorToInt(hitPoint.z / cellSize) - (length - 1) / 2;
        return new Vector2Int(x, z);
    }

    bool CanPlaceObject(int startX, int startZ, int width, int length)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + length; z++)
            {
                if (x < 0 || x >= gridWidth || z < 0 || z >= gridLength) return false;
                if (occupiedCells[x, z]) return false;
            }
        }
        return true;
    }

    void MarkCellsOccupied(int startX, int startZ, int width, int length, bool isOccupied)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + length; z++)
            {
                occupiedCells[x, z] = isOccupied;
            }
        }
    }

    Vector3 CalculateWorldPosition(int startX, int startZ, int width, int length)
    {
        float xPos = (startX * cellSize) + (width * cellSize / 2.0f);
        float zPos = (startZ * cellSize) + (length * cellSize / 2.0f);
        return new Vector3(xPos, 0.5f, zPos);
    }

    public void ClearGrid()
    {
        for (int i = gridContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(gridContainer.GetChild(i).gameObject);
        }
        occupiedCells = new bool[gridWidth, gridLength];

        // Refrescar el preview
        var mouse = Mouse.current;
        if (mouse != null) UpdateGhostPreview(mouse.position.ReadValue());
    }
}