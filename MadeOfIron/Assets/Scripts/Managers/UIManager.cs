using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Asegúrate de incluir esto para usar Button
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    public GameObject shopPanel;
    public GameObject armyPanel; // Panel para gestionar y reclutar tropas

    [Header("Botones de UI")]
    public GameObject armyButton; // Botón para abrir el panel de ejército (solo en Exploración)
    public Button attackButton;   // Referencia al botón de atacar para bloquearlo visualmente

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

        ActualizarEstadoBotonAtaque();
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

        // Cada vez que cambiamos de estado (por ejemplo, volvemos a exploración), actualizamos el botón
        if (isExploring)
        {
            ActualizarEstadoBotonAtaque();
        }
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
            bool willBeOpen = !armyPanel.activeSelf;
            armyPanel.SetActive(willBeOpen);

            // Si cerramos el panel de ejército, actualizamos el botón de ataque por si han cambiado las tropas
            if (!willBeOpen)
            {
                ActualizarEstadoBotonAtaque();
            }
        }
    }

    private bool TieneTropasSeleccionadas()
    {
        var armyManager = ServiceLocator.Get<ArmyManager>();
        if (armyManager == null) return false;

        Dictionary<string, int> tropasElegidas = armyManager.ObtenerTropasSeleccionadas();
        int totalUnidades = 0;

        foreach (var par in tropasElegidas)
        {
            totalUnidades += par.Value;
        }

        return totalUnidades > 0;
    }

    public void ActualizarEstadoBotonAtaque()
    {
        if (attackButton != null)
        {
            attackButton.interactable = TieneTropasSeleccionadas();
        }
    }

    public void OnClickAttackButton()
    {
        // Doble comprobación por seguridad por si se invoca por código
        if (!TieneTropasSeleccionadas())
        {
            Debug.LogWarning("[UIManager] No se puede atacar: Se necesita al menos 1 unidad en el ejército.");
            return;
        }

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