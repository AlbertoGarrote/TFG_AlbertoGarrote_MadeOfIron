using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    public GameObject shopPanel; // Panel que contiene los botones de selección de prefabs

    [Header("Referencias a Managers")]
    public GridPlacementManager placementManager;

    private void Start()
    {
        // Suscribirse al evento de cambio de estado del GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            // Configuración inicial de la UI
            HandleStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    // Activa o desactiva el panel de la tienda según el estado del juego
    private void HandleStateChanged(BaseState state)
    {
        bool isEditing = (state == BaseState.Editing);

        if (shopPanel != null)
        {
            shopPanel.SetActive(isEditing);
        }
    }

    // Métodos vinculados a los botones de la interfaz
    public void ToggleEditMode()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.IsEditing())
        {
            GameManager.Instance.ChangeState(BaseState.Exploration);
        }
        else
        {
            GameManager.Instance.ChangeState(BaseState.Editing);
        }
    }

    public void SelectCube() => placementManager?.SelectPrefab(1);
    public void SelectSphere() => placementManager?.SelectPrefab(2);
    public void SelectCylinder() => placementManager?.SelectPrefab(3);
}