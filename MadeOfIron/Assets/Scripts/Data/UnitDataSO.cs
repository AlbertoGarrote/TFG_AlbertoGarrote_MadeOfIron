using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "RTS/Unit Data")]
public class UnitDataSO : ScriptableObject
{
    [Header("Identificación")]
    public string unitId; // Ej: "Soldado", "Arquero" (útil para conectar con ArmyManager)
    public string unitName;

    [Header("Estadísticas Base")]
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f;

    [Header("Combate")]
    public float attackDamage = 15f;
    public float attackRange = 1.5f; // ~1.5f para Melee, ~6f-10f para Rango
    public float attackCooldown = 1.2f; // Tiempo entre ataques en segundos

    [Header("Ataque a Distancia")]
    [Tooltip("Si se asigna un prefab, la unidad disparará proyectiles. Si es null, atacará cuerpo a cuerpo (Melee).")]
    public GameObject projectilePrefab;
}