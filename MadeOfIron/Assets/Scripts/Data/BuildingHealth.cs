using System;
using UnityEngine;

public class BuildingHealth : MonoBehaviour
{
    [Header("Estadísticas de Salud")]
    public float maxHealth = 300f;
    public float CurrentHealth { get; private set; }

    // Evento que avisa a la barra de vida: (vidaActual, vidaMaxima)
    public event Action<float, float> OnHealthChanged;

    public event Action<BuildingHealth> OnBuildingDestroyed;

    private bool isDestroyed = false;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void InitializeHealth(float hp)
    {
        maxHealth = hp > 0f ? hp : 300f;
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        Debug.Log($"[{gameObject.name}] recibió {amount} de daño. Vida: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
        {
            DestruirEdificio();
        }
    }

    private void DestruirEdificio()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // Avisamos al AttackSceneManager que este edificio ha caído
        OnBuildingDestroyed?.Invoke(this);

        Destroy(gameObject);
    }
}