using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Definición de los estados de la escena de la base
public enum BaseState
{
    Exploration, // Modo normal: ver recursos, interactuar con edificios existentes, abrir menús
    Editing,      // Modo construcción/edición: colocar nuevos objetos, mover o borrar estructuras
    Attacking    // Modo ataque: escena de combate/simulación
}

public class GameManager : MonoBehaviour
{
    // Instancia Singleton estática
    public static GameManager Instance { get; private set; }

    [Header("Estado Actual")]
    public BaseState CurrentState { get; private set; } = BaseState.Exploration;

    public string BaseToAttackJson { get; set; }

    // Evento C# para notificar a otros scripts cuando el estado cambie
    public event Action<BaseState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persiste entre escenas si es necesario
    }

    // Método para cambiar de estado desde cualquier lugar (UI, botones, accesos directos)
    public void ChangeState(BaseState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameManager] Estado cambiado a: {CurrentState}");

        // Notificar a todos los sistemas suscritos (UI, GridPlacementManager, etc.)
        OnStateChanged?.Invoke(CurrentState);
    }

    // Método que llama el botón "Atacar" desde la base
    public void StartAttackSequence(string jsonLayout, string attackSceneName = "AttackScene")
    {
        BaseToAttackJson = jsonLayout;
        ChangeState(BaseState.Attacking);

        // Cambiamos a la escena de ataque
        SceneManager.LoadScene(attackSceneName);
    }

    // Método para volver a la aldea desde la escena de ataque
    public void ReturnToBuildMode(string buildSceneName = "BuildScene")
    {
        ChangeState(BaseState.Exploration);
        SceneManager.LoadScene(buildSceneName);
    }

    // Métodos helper rápidos
    public bool IsEditing() => CurrentState == BaseState.Editing;
    public bool IsExploring() => CurrentState == BaseState.Exploration;
    public bool IsAttacking() => CurrentState == BaseState.Attacking;

}