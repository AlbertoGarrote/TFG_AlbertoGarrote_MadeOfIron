using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Añadido para mostrar recursos en texto
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    public GameObject shopPanel;
    public GameObject armyPanel;

    [Header("Botones de UI")]
    public GameObject armyButton;
    public Button attackButton;

    [Header("Textos de UI (Recursos)")]
    [SerializeField] private TextMeshProUGUI textoRecursosGlobales; // Muestra los recursos del GameManager en pantalla

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

            // Suscribirse al cambio de recursos para actualizar la UI en tiempo real
            gameManager.OnResourcesChanged += ActualizarTextoRecursos;
            ActualizarTextoRecursos(gameManager.GetPlayerResources());
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
            gameManager.OnResourcesChanged -= ActualizarTextoRecursos;
        }
    }

    private void ActualizarTextoRecursos(int cantidadActual)
    {
        if (textoRecursosGlobales != null)
        {
            textoRecursosGlobales.text = $"Recursos: {cantidadActual}";
        }
    }

    private void HandleStateChanged(BaseState state)
    {
        bool isEditing = (state == BaseState.Editing);
        bool isExploring = (state == BaseState.Exploration);

        if (shopPanel != null) shopPanel.SetActive(isEditing);

        if (armyButton != null) armyButton.SetActive(isExploring);
        if (armyPanel != null && !isExploring) armyPanel.SetActive(false);

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

    public void ToggleArmyPanel()
    {
        if (armyPanel != null)
        {
            bool willBeOpen = !armyPanel.activeSelf;
            armyPanel.SetActive(willBeOpen);

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
        if (!TieneTropasSeleccionadas())
        {
            Debug.LogWarning("[UIManager] No se puede atacar: Se necesita al menos 1 unidad en el ejército.");
            return;
        }

        GridPlacementManager pManager = GetPlacementManager();
        if (pManager == null) return;

        var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
        if (saveLoadManager != null) saveLoadManager.GuardarBaseDelUsuario();

        var armyManager = ServiceLocator.Get<ArmyManager>();
        Dictionary<string, int> tropasElegidas = armyManager != null
            ? armyManager.ObtenerTropasSeleccionadas()
            : new Dictionary<string, int>();

        string enemyJson = SimulatedMatchmakingManager.ObtenerBaseEnemigaAleatoria();

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