using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("Configuración de Capas")]
    public LayerMask groundLayer;
    public LayerMask unitLayer; // Asigna las unidades a una capa específica (ej. "Units")

    [Header("Ajustes Visuales de la Caja de Selección")]
    public Color boxFillColor = new Color(0.8f, 0.8f, 0.95f, 0.25f);
    public Color boxBorderColor = new Color(0.8f, 0.8f, 0.95f, 0.8f);

    private bool isDragging = false;
    private Vector3 dragStartPosition;
    private Camera mainCamera;

    // Lista de todas las unidades activas y seleccionadas
    public List<UnitController> allUnits = new List<UnitController>();
    public List<UnitController> selectedUnits = new List<UnitController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        RefreshUnitList();
    }

    public void RefreshUnitList()
    {
        allUnits.Clear();
        allUnits.AddRange(FindObjectsByType<UnitController>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (Pointer.current == null) return;

        Vector2 mousePosition = Pointer.current.position.ReadValue();

        // 1. CLIC IZQUIERDO: Iniciar arrastre o Selección Única
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            dragStartPosition = mousePosition;
        }

        // 2. SOLTAR CLIC IZQUIERDO: Confirmar selección
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isDragging)
            {
                // Si el arrastre fue insignificante, tratamos como Clic Único
                if ((mousePosition - (Vector2)dragStartPosition).sqrMagnitude < 50f)
                {
                    SingleSelect(mousePosition);
                }
                else
                {
                    BoxSelect(mousePosition);
                }
                isDragging = false;
            }
        }

        // 3. CLIC DERECHO: Ordenar Movimiento a las unidades seleccionadas
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CommandMoveSelectedUnits(mousePosition);
        }
    }

    private void SingleSelect(Vector2 screenPosition)
    {
        DeselectAll();

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, unitLayer))
        {
            UnitController unit = hit.collider.GetComponentInParent<UnitController>();
            if (unit != null)
            {
                SelectUnit(unit);
            }
        }
    }

    private void BoxSelect(Vector2 screenPosition)
    {
        DeselectAll();

        Bounds viewportBounds = UtilsUI.GetViewportBounds(mainCamera, dragStartPosition, screenPosition);

        foreach (UnitController unit in allUnits)
        {
            if (unit == null) continue;

            // Verificamos si la posición de la unidad en la pantalla entra dentro del marco
            if (viewportBounds.Contains(mainCamera.WorldToViewportPoint(unit.transform.position)))
            {
                SelectUnit(unit);
            }
        }
    }

    private void CommandMoveSelectedUnits(Vector2 screenPosition)
    {
        if (selectedUnits.Count == 0) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            // Distribuir las unidades en pequeñas formaciones si hay varias
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                Vector3 offset = GetFormationOffset(i, selectedUnits.Count);
                selectedUnits[i].MoveToDestination(hit.point + offset);
            }
        }
    }

    private Vector3 GetFormationOffset(int index, int totalCount)
    {
        if (totalCount <= 1) return Vector3.zero;

        // Distribución en círculo/cuadrícula pequeña alrededor del clic
        float spacing = 1.2f;
        int rows = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
        int row = index / rows;
        int col = index % rows;

        return new Vector3((col - rows / 2f) * spacing, 0f, (row - rows / 2f) * spacing);
    }

    private void SelectUnit(UnitController unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true);
        }
    }

    public void DeselectAll()
    {
        foreach (UnitController unit in selectedUnits)
        {
            if (unit != null) unit.SetSelected(false);
        }
        selectedUnits.Clear();
    }

    private void OnGUI()
    {
        // Dibuja la caja de selección en pantalla mientras el jugador arrastra el ratón
        if (isDragging && Pointer.current != null)
        {
            Vector2 currentMouse = Pointer.current.position.ReadValue();
            Rect rect = UtilsUI.GetScreenRect(dragStartPosition, currentMouse);

            UtilsUI.DrawScreenRect(rect, boxFillColor);
            UtilsUI.DrawScreenRectBorder(rect, 2f, boxBorderColor);
        }
    }
}