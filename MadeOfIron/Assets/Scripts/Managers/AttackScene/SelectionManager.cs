using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("Configuración de Capas")]
    public LayerMask groundLayer;
    public LayerMask unitLayer;     // Capa de las unidades (ej. "Units")
    public LayerMask buildingLayer; // Capa de los edificios (ej. "Building")

    [Header("Ajustes Visuales de la Caja de Selección")]
    public Color boxFillColor = new Color(0.8f, 0.8f, 0.95f, 0.25f);
    public Color boxBorderColor = new Color(0.8f, 0.8f, 0.95f, 0.8f);

    private bool isMouseDown = false;
    private bool isDragging = false;
    private Vector3 dragStartPosition;
    private Camera mainCamera;

    // Tolerancia en píxeles al cuadrado (100f = 10px de margen para considerar clic único)
    private const float DRAG_THRESHOLD_SQR = 100f;

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

        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<SelectionManager>();
        }
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

        // 1. PULSAR CLIC IZQUIERDO: Guardamos posición inicial
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isMouseDown = true;
            isDragging = false;
            dragStartPosition = mousePosition;
        }

        // 2. MANTENER CLIC IZQUIERDO: Comprobamos si el ratón se movió lo suficiente para ser caja de selección
        if (isMouseDown && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if ((mousePosition - (Vector2)dragStartPosition).sqrMagnitude >= DRAG_THRESHOLD_SQR)
            {
                isDragging = true;
            }
        }

        // 3. SOLTAR CLIC IZQUIERDO: Confirmar selección única o por caja
        if (isMouseDown && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (isDragging)
            {
                BoxSelect(mousePosition);
            }
            else
            {
                SingleSelect(mousePosition);
            }

            isMouseDown = false;
            isDragging = false;
        }

        // 4. CLIC DERECHO: Ordenar Ataque o Movimiento a las unidades seleccionadas
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CommandSelectedUnits(mousePosition);
        }
    }

    private void SingleSelect(Vector2 screenPosition)
    {
        DeselectAll();

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Usamos SphereCast pequeño (radio 0.3f) además de Raycast para que sea más fácil hacer clic en sprites/unidades delgadas
        if (Physics.SphereCast(ray, 0.3f, out RaycastHit hit, 500f, unitLayer))
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

        allUnits.RemoveAll(u => u == null);

        Bounds viewportBounds = UtilsUI.GetViewportBounds(mainCamera, dragStartPosition, screenPosition);

        foreach (UnitController unit in allUnits)
        {
            if (viewportBounds.Contains(mainCamera.WorldToViewportPoint(unit.transform.position)))
            {
                SelectUnit(unit);
            }
        }
    }

    private void CommandSelectedUnits(Vector2 screenPosition)
    {
        selectedUnits.RemoveAll(u => u == null);
        if (selectedUnits.Count == 0) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // PRIORIDAD 1: Clic derecho sobre un edificio atacable
        if (Physics.Raycast(ray, out RaycastHit buildingHit, 500f, buildingLayer))
        {
            BuildingHealth targetHealth = buildingHit.collider.GetComponentInParent<BuildingHealth>();
            if (targetHealth != null)
            {
                foreach (UnitController unit in selectedUnits)
                {
                    unit.AttackTarget(targetHealth.transform);
                }
                return;
            }
        }

        // PRIORIDAD 2: Clic derecho sobre el suelo
        if (Physics.Raycast(ray, out RaycastHit groundHit, 500f, groundLayer))
        {
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                Vector3 offset = GetFormationOffset(i, selectedUnits.Count);
                selectedUnits[i].MoveToDestination(groundHit.point + offset);
            }
        }
    }

    private Vector3 GetFormationOffset(int index, int totalCount)
    {
        if (totalCount <= 1) return Vector3.zero;

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
        // Solo dibujamos el recuadro si realmente se ha superado el umbral de arrastre
        if (isDragging && Pointer.current != null)
        {
            Vector2 currentMouse = Pointer.current.position.ReadValue();
            Rect rect = UtilsUI.GetScreenRect(dragStartPosition, currentMouse);

            UtilsUI.DrawScreenRect(rect, boxFillColor);
            UtilsUI.DrawScreenRectBorder(rect, 2f, boxBorderColor);
        }
    }
}