using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingSelectionManager : MonoBehaviour
{
    [Header("Configuración de Selección")]
    public LayerMask buildingLayer;
    public Material selectedMaterial; // Opcional: Material Morado para aplicar

    private BuildingObject currentSelectedBuilding;

    private void Start()
    {
        // Suscribirse a cambios de estado para deseleccionar al cambiar a modo Edición
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(BaseState state)
    {
        // Si salimos de Exploración, quitamos cualquier selección activa
        if (state != BaseState.Exploration)
        {
            DeselectCurrent();
        }
    }

    private void Update()
    {
        // Solo permitir selección si estamos estrictamente en MODO EXPLORACIÓN
        if (GameManager.Instance != null && GameManager.Instance.IsEditing()) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Ignorar clics sobre elementos de la interfaz de usuario (UI)
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TrySelectBuilding(mouse.position.ReadValue());
        }
    }

    private void TrySelectBuilding(Vector2 mouseScreenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // Buscar si el objeto impactado o alguno de sus padres tiene el componente BuildingObject
            BuildingObject clickedBuilding = hit.collider.GetComponentInParent<BuildingObject>();

            if (clickedBuilding != null)
            {
                // Si hacemos clic en el mismo edificio seleccionado, no hacemos nada
                if (currentSelectedBuilding == clickedBuilding) return;

                // Deseleccionar el anterior
                DeselectCurrent();

                // Seleccionar el nuevo
                currentSelectedBuilding = clickedBuilding;
                currentSelectedBuilding.SetSelected(true, selectedMaterial);
            }
            else
            {
                // Si hacemos clic en el terreno o suelo sin edificio, deseleccionamos
                DeselectCurrent();
            }
        }
        else
        {
            DeselectCurrent();
        }
    }

    public void DeselectCurrent()
    {
        if (currentSelectedBuilding != null)
        {
            currentSelectedBuilding.SetSelected(false);
            currentSelectedBuilding = null;
        }
    }
}