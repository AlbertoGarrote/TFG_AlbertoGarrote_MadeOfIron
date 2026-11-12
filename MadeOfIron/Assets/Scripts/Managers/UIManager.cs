using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    public GameObject shopPanel;
    public GameObject armyPanel; // Panel para gestionar y reclutar tropas

    [Header("Botones de UI")]
    public GameObject armyButton; // Botón para abrir el panel de ejército (solo en Exploración)

    [Header("Referencias a Managers")]
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

        var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
        if (saveLoadManager != null)
        {
            saveLoadManager.CargarBaseDelUsuario();
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
        bool isExploring = (state == BaseState.Exploration);

        if (shopPanel != null) shopPanel.SetActive(isEditing);

        // El botón y el panel de ejército solo se gestionan en Exploración
        if (armyButton != null) armyButton.SetActive(isExploring);
        if (armyPanel != null && !isExploring) armyPanel.SetActive(false); // Ocultar si cambia de estado
    }

    public void ToggleEditMode()
    {
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager == null) return;

        if (gameManager.IsEditing())
        {
            var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
            if (saveLoadManager != null) saveLoadManager.GuardarBaseDelUsuario();

            gameManager.ChangeState(BaseState.Exploration);
        }
        else
        {
            gameManager.ChangeState(BaseState.Editing);
        }
    }

    // Método para abrir/cerrar el panel de ejército desde la UI
    public void ToggleArmyPanel()
    {
        if (armyPanel != null)
        {
            armyPanel.SetActive(!armyPanel.activeSelf);
        }
    }

    public void OnClickAttackButton()
    {
        GridPlacementManager pManager = GetPlacementManager();
        if (pManager == null) return;

        var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
        if (saveLoadManager != null) saveLoadManager.GuardarBaseDelUsuario();

        // 1. Obtener las tropas seleccionadas del ArmyManager
        var armyManager = ServiceLocator.Get<ArmyManager>();
        Dictionary<string, int> tropasElegidas = armyManager != null
            ? armyManager.ObtenerTropasSeleccionadas()
            : new Dictionary<string, int>();

        // 2. Generar o cargar base enemiga aleatoria
        string enemyJson = SimulatedMatchmakingManager.ObtenerBaseEnemigaAleatoria();

        // 3. Iniciar secuencia de ataque pasando el JSON y las tropas seleccionadas
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null)
        {
            gameManager.StartAttackSequence(enemyJson, tropasElegidas, "AttackScene");
        }
    }

    public void OnClickLogoutButton()
    {
        var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
        if (saveLoadManager != null) saveLoadManager.GuardarBaseDelUsuario();

        SceneManager.LoadScene("LoginScene");
    }

    public void OnSelectBuilding1x1Pressed() => GetPlacementManager()?.SelectBuildingIndex(0);
    public void OnSelectBuilding3x1Pressed() => GetPlacementManager()?.SelectBuildingIndex(1);
    public void OnSelectBuilding2x2Pressed() => GetPlacementManager()?.SelectBuildingIndex(2);
}