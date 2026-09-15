using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Definición de los estados de la escena de la base
public enum BaseState
{
    Exploration, // Modo normal: ver recursos, interactuar con edificios existentes, abrir menús
    Editing      // Modo construcción/edición: colocar nuevos objetos, mover o borrar estructuras
}

public class GameManager : MonoBehaviour
{
    // Instancia Singleton estática
    public static GameManager Instance { get; private set; }

    [Header("Estado Actual")]
    public BaseState CurrentState { get; private set; } = BaseState.Exploration;

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

    // Métodos helper rápidos
    public bool IsEditing() => CurrentState == BaseState.Editing;
    public bool IsExploring() => CurrentState == BaseState.Exploration;

    void Update()
    {
        // DEBUG TEMPORAL
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Al pulsar la tecla 'E', alternamos el estado
        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (CurrentState == BaseState.Exploration)
            {
                ChangeState(BaseState.Editing);
            }
            else
            {
                ChangeState(BaseState.Editing); // Pasa a Exploración
                ChangeState(BaseState.Exploration);
            }
        }
    }
}