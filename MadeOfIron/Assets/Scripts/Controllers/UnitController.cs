using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{
    [Header("Datos de la Tropa")]
    public UnitDataSO unitData;

    private NavMeshAgent agent;
    private Camera mainCamera;

    [Header("Referencias Visuales y Audio")]
    public SpriteRenderer spriteRenderer;
    public GameObject selectionIndicator;
    public Transform firePoint; // Opcional: punto desde donde salen los proyectiles
    public bool lockYRotation = true;

    private MusicSoundManager soundController;

    [Header("Configuración de Movimiento")]
    public LayerMask groundLayer;
    public float movementThreshold = 0.1f;

    public bool IsSelected { get; private set; }

    // Estado interno de combate y salud
    private float currentHealth;
    private float attackTimer;
    private Transform targetBuilding;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        soundController = ServiceLocator.Get<MusicSoundManager>();

        InicializarEstadisticas();
        SetSelected(false);
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void InicializarEstadisticas()
    {
        if (unitData != null)
        {
            currentHealth = unitData.maxHealth;

            if (agent != null)
            {
                agent.speed = unitData.moveSpeed;
            }
        }
        else
        {
            Debug.LogWarning($"[UnitController] La unidad {gameObject.name} no tiene un UnitDataSO asignado.");
        }
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        // Si el jugador le ordenó atacar un edificio, gestionamos persecución y ataque
        if (targetBuilding != null)
        {
            GestionarOrdenAtaque();
        }

        HandleSpriteFlip();
    }

    // =========================================================================
    // ÓRDENES DEL JUGADOR (Llamadas desde el SelectionManager / Input)
    // =========================================================================

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selected && soundController != null)
        {
            soundController.ReproducirSFX3D("Selected", transform.position);
        }

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }
    }

    /// <summary>
    /// Orden de clic derecho en el suelo: cancela el ataque actual y se mueve al punto.
    /// </summary>
    public void MoveToDestination(Vector3 destination)
    {
        targetBuilding = null; // Limpiamos el objetivo de ataque

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.stoppingDistance = 0.1f; // Frena exacto en el punto clicado
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    /// <summary>
    /// Orden de clic derecho sobre un edificio: se acerca hasta su rango y lo ataca.
    /// </summary>
    public void AttackTarget(Transform building)
    {
        if (building == null || unitData == null) return;

        targetBuilding = building;

        if (agent != null && agent.isActiveAndEnabled)
        {
            // Ajustamos la distancia de frenado al rango de ataque de esta tropa
            agent.stoppingDistance = Mathf.Max(0.5f, unitData.attackRange * 0.85f);
            agent.isStopped = false;
            agent.SetDestination(targetBuilding.position);
        }
    }

    // =========================================================================
    // LÓGICA INTERNA DE ATAQUE Y DAÑO
    // =========================================================================

    private void GestionarOrdenAtaque()
    {
        if (unitData == null) return;

        // Calculamos la distancia horizontal (ignorando altura Y) al centro o punto más cercano del edificio
        Vector3 posUnidad = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 posEdificio = new Vector3(targetBuilding.position.x, 0f, targetBuilding.position.z);

        // Si el edificio tiene Collider, medimos hasta su borde exterior (muy importante para edificios grandes 2x2 o 3x1)
        Collider buildingCollider = targetBuilding.GetComponentInChildren<Collider>();
        if (buildingCollider != null)
        {
            Vector3 closestPoint = buildingCollider.ClosestPoint(transform.position);
            posEdificio = new Vector3(closestPoint.x, 0f, closestPoint.z);
        }

        float distance = Vector3.Distance(posUnidad, posEdificio);

        if (distance <= unitData.attackRange)
        {
            // Ya estamos a rango: frenamos y atacamos
            if (agent != null && agent.isActiveAndEnabled && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            if (attackTimer <= 0f)
            {
                EjecutarAtaque();
                attackTimer = unitData.attackCooldown;
            }
        }
        else
        {
            // Aún estamos lejos (o el edificio se movió): seguimos acercándonos
            if (agent != null && agent.isActiveAndEnabled && agent.isStopped)
            {
                agent.isStopped = false;
                agent.SetDestination(posEdificio);
            }
        }
    }

    private void EjecutarAtaque()
    {
        if (targetBuilding == null) return;

        if (unitData.projectilePrefab != null)
        {
            // ATAQUE A DISTANCIA: Crea el proyectil y le pasa el objetivo y el daño
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.5f;
            GameObject projObj = Instantiate(unitData.projectilePrefab, spawnPos, Quaternion.identity);

            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Initialize(targetBuilding, unitData.attackDamage);
            }
            else
            {
                // Fallback por si el prefab de proyectil aún no tiene el script Projectile.cs
                AplicarDanoDirectoAlEdificio();
            }
        }
        else
        {
            // ATAQUE MELEE: Golpe directo al BuildingHealth del edificio
            AplicarDanoDirectoAlEdificio();
        }
    }

    private void AplicarDanoDirectoAlEdificio()
    {
        if (targetBuilding == null) return;

        // Buscamos BuildingHealth en el objeto o en su padre (por si el target era un collider hijo)
        BuildingHealth health = targetBuilding.GetComponentInParent<BuildingHealth>();
        if (health != null)
        {
            health.TakeDamage(unitData.attackDamage);
        }
        else
        {
            Debug.LogWarning($"[UnitController] El edificio '{targetBuilding.name}' no tiene componente BuildingHealth.");
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    // =========================================================================
    // VISUALES (FLIP Y BILLBOARDING)
    // =========================================================================

    private void HandleSpriteFlip()
    {
        if (spriteRenderer == null || agent == null) return;

        Vector3 worldVelocity = new Vector3(agent.velocity.x, 0f, agent.velocity.z);

        // Si se está moviendo, mira hacia donde camina; si está parado atacando, mira hacia el edificio
        if (worldVelocity.sqrMagnitude > movementThreshold * movementThreshold)
        {
            Vector3 cameraRelativeVelocity = mainCamera.transform.InverseTransformDirection(worldVelocity);
            if (cameraRelativeVelocity.x > 0.05f) spriteRenderer.flipX = false;
            else if (cameraRelativeVelocity.x < -0.05f) spriteRenderer.flipX = true;
        }
        else if (targetBuilding != null)
        {
            Vector3 dirToTarget = targetBuilding.position - transform.position;
            Vector3 cameraRelativeDir = mainCamera.transform.InverseTransformDirection(dirToTarget);
            if (cameraRelativeDir.x > 0.05f) spriteRenderer.flipX = false;
            else if (cameraRelativeDir.x < -0.05f) spriteRenderer.flipX = true;
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 targetPosition = transform.position + mainCamera.transform.rotation * Vector3.forward;
        Vector3 targetUp = mainCamera.transform.rotation * Vector3.up;

        if (lockYRotation)
        {
            targetPosition.y = transform.position.y;
            targetUp = Vector3.up;
        }

        transform.LookAt(targetPosition, targetUp);
    }
}