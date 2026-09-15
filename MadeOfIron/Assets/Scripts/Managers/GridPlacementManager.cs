using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlacementManager : MonoBehaviour
{
    [Header("Configuración de Cuadrícula")]
    public float cellSize = 1.0f;
    public int gridWidth = 50;
    public int gridLength = 50;

    [Header("Capas y Selección")]
    public LayerMask groundLayer;

    [Header("Objetos a colocar (Prefabs)")]
    public GameObject cubePrefab;
    public GameObject spherePrefab;
    public GameObject cylinderPrefab;

    private int selectedObjectType = 1; // 1 = Cubo, 2 = Esfera, 3 = Cilindro
    private Transform gridContainer;

    void Start()
    {
        GameObject containerObj = new GameObject("PlacedObjectsContainer");
        gridContainer = containerObj.transform;
    }

    void Update()
    {
        // Solo colocar objetos si estamos en modo Edición
        if (GameManager.Instance != null && !GameManager.Instance.IsEditing()) return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        // Tecla Espacio para vaciar el mapa
        if (keyboard.spaceKey.wasPressedThisFrame) ClearGrid();

        // Clic izquierdo para instanciar la estructura seleccionada
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Evitar colocar objetos si el clic fue sobre un elemento de la UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryPlaceObject(mouse.position.ReadValue());
        }
    }

    // Método público invocado por los botones de la UI
    public void SelectPrefab(int type)
    {
        selectedObjectType = type;
        Debug.Log($"[Placement] Prefab seleccionado: {type}");
    }

    void TryPlaceObject(Vector2 mouseScreenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 hitPoint = hit.point;

            if (IsWithinBounds(hitPoint))
            {
                Vector3 gridPosition = SnapToGrid(hitPoint);
                GameObject prefabToSpawn = GetSelectedPrefab();

                if (prefabToSpawn != null)
                {
                    GameObject newObject = Instantiate(prefabToSpawn, gridPosition, Quaternion.identity);
                    newObject.transform.SetParent(gridContainer);
                }
            }
        }
    }

    bool IsWithinBounds(Vector3 position)
    {
        return position.x >= 0 && position.x < (gridWidth * cellSize) &&
               position.z >= 0 && position.z < (gridLength * cellSize);
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Floor(position.x / cellSize) * cellSize + (cellSize / 2.0f);
        float z = Mathf.Floor(position.z / cellSize) * cellSize + (cellSize / 2.0f);
        return new Vector3(x, 0.5f, z);
    }

    public void ClearGrid()
    {
        for (int i = gridContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(gridContainer.GetChild(i).gameObject);
        }
    }

    GameObject GetSelectedPrefab()
    {
        switch (selectedObjectType)
        {
            case 1: return cubePrefab;
            case 2: return spherePrefab;
            case 3: return cylinderPrefab;
            default: return cubePrefab;
        }
    }
}