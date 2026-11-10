using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    public GameObject shopPanel;

    [Header("Referencias a Managers (Opcional, se buscan vía ServiceLocator)")]
    public GridPlacementManager placementManager;

    private GridPlacementManager GetPlacementManager()
    {
        if (placementManager != null) return placementManager;
        placementManager = ServiceLocator.Get<GridPlacementManager>();
        return placementManager;
    }

    private void Start()
    {
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnStateChanged += HandleStateChanged;
            HandleStateChanged(gameManager.CurrentState);
        }
    }

    private void OnDestroy()
    {
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnStateChanged -= HandleStateChanged;
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
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager == null) return;

        if (gameManager.IsEditing())
            gameManager.ChangeState(BaseState.Exploration);
        else
            gameManager.ChangeState(BaseState.Editing);
    }

    public void OnClickAttackButton()
    {
        GridPlacementManager pManager = GetPlacementManager();
        if (pManager == null) return;

        // Exportamos la base actual reutilizando nuestra función
        GridSaveData saveData = pManager.ExportGridState();
        string jsonOutput = JsonUtility.ToJson(saveData);

        // Mandamos el JSON al GameManager e iniciamos el cambio de escena
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null)
        {
            gameManager.StartAttackSequence(jsonOutput, "AttackScene");
            // Asegúrate de poner el nombre exacto de tu escena de ataque
        }
    }

    // --- MÉTODOS DE LA UI ---
    // La UI solo notifica qué opción se pulsó, delegando la responsabilidad al GridPlacementManager
    public void OnSelectBuilding1x1Pressed() => GetPlacementManager()?.SelectBuildingIndex(0);
    public void OnSelectBuilding3x1Pressed() => GetPlacementManager()?.SelectBuildingIndex(1);
    public void OnSelectBuilding2x2Pressed() => GetPlacementManager()?.SelectBuildingIndex(2);
}