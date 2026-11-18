using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BaseState
{
    Exploration,
    Editing,
    Attacking
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado Actual")]
    public BaseState CurrentState { get; private set; } = BaseState.Exploration;

    public string BaseToAttackJson { get; set; }

    [Header("Tropas Seleccionadas para el Ataque")]
    public Dictionary<string, int> tropasSeleccionadasParaAtaque = new Dictionary<string, int>();

    public event Action<BaseState> OnStateChanged;
    public event Action<int> OnResourcesChanged; // Para refrescar la UI de recursos

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ServiceLocator.Register(this);
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeState(BaseState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameManager] Estado cambiado a: {CurrentState}");
        OnStateChanged?.Invoke(CurrentState);
    }

    public void StartAttackSequence(string jsonLayout, Dictionary<string, int> tropasElegidas, string attackSceneName = "AttackScene")
    {
        BaseToAttackJson = jsonLayout;
        tropasSeleccionadasParaAtaque = tropasElegidas != null ? new Dictionary<string, int>(tropasElegidas) : new Dictionary<string, int>();
        ChangeState(BaseState.Attacking);

        SceneManager.LoadScene(attackSceneName);
    }

    public void ReturnToBuildMode(string buildSceneName = "BaseScene")
    {
        ChangeState(BaseState.Exploration);
        SceneManager.LoadScene(buildSceneName);
    }

    // ==========================================
    // GESTIÓN DE RECURSOS Y DATOS VINCULADOS AL JUGADOR
    // ==========================================

    private AccountManager GetAccountManager()
    {
        return ServiceLocator.Get<AccountManager>();
    }

    public int GetPlayerResources()
    {
        var accManager = GetAccountManager();
        if (accManager != null && accManager.CurrentPlayerData != null)
        {
            return accManager.CurrentPlayerData.recursos;
        }
        return 500; // Valor por defecto si no hay sesión
    }

    public void AñadirRecursos(int cantidad)
    {
        if (cantidad <= 0) return;

        var accManager = GetAccountManager();
        if (accManager != null && accManager.CurrentPlayerData != null)
        {
            accManager.CurrentPlayerData.recursos += cantidad;

            // Guardamos automáticamente en el JSON del usuario
            accManager.SaveCurrentPlayerData();

            int totalActual = accManager.CurrentPlayerData.recursos;
            OnResourcesChanged?.Invoke(totalActual);
            Debug.Log($"[GameManager] Añadidos {cantidad} recursos a '{accManager.CurrentPlayerData.username}'. Total actual: {totalActual}");
        }
        else
        {
            Debug.LogWarning("[GameManager] No se pudieron añadir recursos: No hay AccountManager o PlayerData activo.");
        }
    }

    // Métodos útiles para consultar Nivel y Experiencia del jugador actual
    public int GetPlayerLevel()
    {
        var accManager = GetAccountManager();
        return (accManager != null && accManager.CurrentPlayerData != null) ? accManager.CurrentPlayerData.playerLevel : 1;
    }

    public int GetPlayerExp()
    {
        var accManager = GetAccountManager();
        return (accManager != null && accManager.CurrentPlayerData != null) ? accManager.CurrentPlayerData.currentExp : 0;
    }

    public void AñadirExp(int cantidadExp)
    {
        var accManager = GetAccountManager();
        if (accManager != null && accManager.CurrentPlayerData != null)
        {
            accManager.CurrentPlayerData.currentExp += cantidadExp;
            accManager.SaveCurrentPlayerData();
            Debug.Log($"[GameManager] Experiencia añadida. Total Exp: {accManager.CurrentPlayerData.currentExp}");
        }
    }

    public bool IsEditing() => CurrentState == BaseState.Editing;
    public bool IsExploring() => CurrentState == BaseState.Exploration;
    public bool IsAttacking() => CurrentState == BaseState.Attacking;
}