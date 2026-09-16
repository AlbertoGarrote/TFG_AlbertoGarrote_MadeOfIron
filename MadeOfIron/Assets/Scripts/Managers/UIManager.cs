using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    public GameObject shopPanel;

    [Header("Referencias a Managers")]
    public GridPlacementManager placementManager;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
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

    private void HandleStateChanged(BaseState state)
    {
        bool isEditing = (state == BaseState.Editing);

        if (shopPanel != null)
        {
            shopPanel.SetActive(isEditing);
        }
    }

    public void ToggleEditMode()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.IsEditing())
            GameManager.Instance.ChangeState(BaseState.Exploration);
        else
            GameManager.Instance.ChangeState(BaseState.Editing);
    }

    // --- MÉTODOS DE LA UI ---
    // La UI solo notifica qué opción se pulsó, delegando la responsabilidad al GridPlacementManager
    public void OnSelectBuilding1x1Pressed() => placementManager?.SelectBuildingIndex(0);
    public void OnSelectBuilding3x1Pressed() => placementManager?.SelectBuildingIndex(1);
    public void OnSelectBuilding2x2Pressed() => placementManager?.SelectBuildingIndex(2);
}