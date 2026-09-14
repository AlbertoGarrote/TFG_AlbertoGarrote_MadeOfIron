using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlacementManager : MonoBehaviour
{
    [Header("Configuración de Cuadrícula")]
    public float cellSize = 1.0f;

    [Header("Capas y Selección")]
    public LayerMask groundLayer;

    [Header("Objetos a colocar (Prefabs)")]
    public GameObject cubePrefab;
    public GameObject spherePrefab;
    public GameObject cylinderPrefab;

    private int selectedObjectType = 1;
    private Transform gridContainer; // Objeto padre para organizar la jerarquía y borrar fácilmente

    void Start()
    {
        // Crear un objeto contenedor en la jerarquía para guardar los elementos colocados
        GameObject containerObj = new GameObject("PlacedObjectsContainer");
        gridContainer = containerObj.transform;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        // Selección por teclado (1, 2, 3)
        if (keyboard.digit1Key.wasPressedThisFrame) selectedObjectType = 1;
        if (keyboard.digit2Key.wasPressedThisFrame) selectedObjectType = 2;
        if (keyboard.digit3Key.wasPressedThisFrame) selectedObjectType = 3;

        // Borrar todo con la barra espaciadora
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            ClearGrid();
        }

        // Clic izquierdo para colocar
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryPlaceObject(mouse.position.ReadValue());
        }
    }

    void TryPlaceObject(Vector2 mouseScreenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 gridPosition = SnapToGrid(hit.point);
            GameObject prefabToSpawn = GetSelectedPrefab();

            if (prefabToSpawn != null)
            {
                // Instanciar el objeto y asignarlo como hijo del contenedor
                GameObject newObject = Instantiate(prefabToSpawn, gridPosition, Quaternion.identity);
                newObject.transform.SetParent(gridContainer);
            }
        }
    }

    // Método para eliminar todos los objetos colocados en la cuadrícula
    public void ClearGrid()
    {
        // Recorrer todos los hijos del contenedor de forma inversa para destruirlos
        for (int i = gridContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(gridContainer.GetChild(i).gameObject);
        }

        Debug.Log("Cuadrícula limpiada por completo.");
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Floor(position.x / cellSize) * cellSize + (cellSize / 2.0f);
        float z = Mathf.Floor(position.z / cellSize) * cellSize + (cellSize / 2.0f);
        float y = 0.5f;

        return new Vector3(x, y, z);
    }

    GameObject GetSelectedPrefab()
    {
        switch (selectedObjectType)
        {
            case 1:
                return cubePrefab;
            case 2:
                return spherePrefab;
            case 3:
                return cylinderPrefab;
            default:
                return cubePrefab;
        }
    }
}