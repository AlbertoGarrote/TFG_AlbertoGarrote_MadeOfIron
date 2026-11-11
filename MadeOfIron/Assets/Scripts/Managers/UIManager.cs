using UnityEngine;
using UnityEngine.SceneManagement;

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

        // Cargar automáticamente la base del usuario nada más entrar a la escena de construcción
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
        {
            // Estábamos editando y vamos a pasar a Exploración -> ¡Es el momento de guardar!
            var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
            if (saveLoadManager != null)
            {
                saveLoadManager.GuardarBaseDelUsuario();
            }

            gameManager.ChangeState(BaseState.Exploration);
        }
        else
        {
            // Pasamos a modo Edición
            gameManager.ChangeState(BaseState.Editing);
        }
    }

    public void OnClickAttackButton()
    {
        GridPlacementManager pManager = GetPlacementManager();
        if (pManager == null) return;

        // Antes de atacar, nos aseguramos de guardar la base actual en el perfil
        var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
        if (saveLoadManager != null)
        {
            saveLoadManager.GuardarBaseDelUsuario();
        }

        // Exportamos la base actual reutilizando nuestra función
        GridSaveData saveData = pManager.ExportGridState();
        string jsonOutput = JsonUtility.ToJson(saveData);

        // Mandamos el JSON al GameManager e iniciamos el cambio de escena
        var gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager != null)
        {
            gameManager.StartAttackSequence(jsonOutput, "AttackScene");
        }
    }

    // ==========================================
    // NUEVO: BOTÓN DE CIERRE DE SESIÓN
    // ==========================================
    public void OnClickLogoutButton()
    {
        // 1. Guardar por última vez la base actual antes de salir (por si acaso estaba editando)
        var saveLoadManager = ServiceLocator.Get<GridSaveLoadManager>();
        if (saveLoadManager != null)
        {
            saveLoadManager.GuardarBaseDelUsuario();
        }

        // 2. Limpiar la sesión actual del AccountManager
        var accountManager = ServiceLocator.Get<AccountManager>();
        if (accountManager != null)
        {
            // Opcional: si crearas una función para limpiar el CurrentPlayerData, se llamaría aquí. 
            // Al cambiar de escena y volver al login, un nuevo login sobrescribirá CurrentPlayerData de todas formas.
            Debug.Log("[UIManager] Cerrando sesión del usuario actual...");
        }

        // 3. Volver a la escena de inicio de sesión
        SceneManager.LoadScene("LoginScene");
    }

    // --- MÉTODOS DE LA UI ---
    public void OnSelectBuilding1x1Pressed() => GetPlacementManager()?.SelectBuildingIndex(0);
    public void OnSelectBuilding3x1Pressed() => GetPlacementManager()?.SelectBuildingIndex(1);
    public void OnSelectBuilding2x2Pressed() => GetPlacementManager()?.SelectBuildingIndex(2);
}