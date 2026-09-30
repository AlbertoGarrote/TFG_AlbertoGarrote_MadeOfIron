using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem; // <-- Importante incluir el namespace

[RequireComponent(typeof(NavMeshAgent))]
public class UnitController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Camera mainCamera;

    public SpriteRenderer spriteRenderer;

    public bool lockYRotation = true;

    [Header("Configuración de Raycast")]
    [Tooltip("Capa que representa el suelo o superficie caminable.")]
    public LayerMask groundLayer;

    public float movementThreshold = 0.1f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Comprobamos si el puntero (Ratón o Pantalla Táctil) ha presionado en este frame
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            // Leemos la posición actual del cursor/puntero en la pantalla
            Vector2 screenPosition = Pointer.current.position.ReadValue();
            MoveToScreenPosition(screenPosition);
        }

        HandleSpriteFlip();
    }

    private void MoveToScreenPosition(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Si el Raycast impacta contra la capa del suelo
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            // Ordenamos al NavMeshAgent que calcule la ruta hasta el punto
            agent.SetDestination(hit.point);
        }
    }

    private void HandleSpriteFlip()
    {
        if (spriteRenderer == null) return;

        // Vector de velocidad horizontal en el mundo (ignoramos la Y/vertical)
        Vector3 worldVelocity = new Vector3(agent.velocity.x, 0f, agent.velocity.z);

        // Si la velocidad es suficiente para considerar que se está moviendo
        if (worldVelocity.sqrMagnitude > movementThreshold * movementThreshold)
        {
            // Transformamos el vector de velocidad al espacio local de la cámara
            // Esto nos da la velocidad relativa a lo que ve la pantalla/jugador
            Vector3 cameraRelativeVelocity = mainCamera.transform.InverseTransformDirection(worldVelocity);

            // Si cameraRelativeVelocity.x > 0 se mueve a la derecha de la pantalla, si es < 0 a la izquierda
            if (cameraRelativeVelocity.x > 0.05f)
            {
                spriteRenderer.flipX = false; // Mirando a la derecha
            }
            else if (cameraRelativeVelocity.x < -0.05f)
            {
                spriteRenderer.flipX = true;  // Mirando a la izquierda (flip)
            }
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Apuntamos en la misma dirección hacia la que mira la cámara
        Vector3 targetPosition = transform.position + mainCamera.transform.rotation * Vector3.forward;
        Vector3 targetUp = mainCamera.transform.rotation * Vector3.up;

        if (lockYRotation)
        {
            // Mantiene la unidad erguida verticalmente ignorando la inclinación X/Z de la cámara
            targetPosition.y = transform.position.y;
            targetUp = Vector3.up;
        }

        transform.LookAt(targetPosition, targetUp);
    }
}