using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Velocidad y Suavizado de Movimiento")]
    public float moveSpeed = 20f;
    public float edgeScrollSensitivity = 20f;
    public float smoothTime = 0.15f;

    [Header("Configuración de Zoom (A 45 grados)")]
    public float zoomSpeed = 30f;
    public float minHeight = 8f;  // Altura mínima (Zoom In máximo)
    public float maxHeight = 35f; // Altura máxima (Zoom Out máximo)

    [Header("Límites del Mapa (50x50 Grid)")]
    public float minX = -10f;
    public float maxX = 60f;
    public float minZ = -10f;
    public float maxZ = 60f;

    [Header("Opciones")]
    public bool enableEdgeScrolling = true;

    private Vector3 targetPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        Vector2 inputDirection = Vector2.zero;

        // 1. Lectura de Entrada (Teclado)
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputDirection.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputDirection.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputDirection.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputDirection.x += 1f;
        }

        // 2. Lectura de Entrada (Bordes de Pantalla)
        if (enableEdgeScrolling && mouse != null)
        {
            Vector2 mousePos = mouse.position.ReadValue();

            if (mousePos.x >= Screen.width - edgeScrollSensitivity) inputDirection.x += 1f;
            if (mousePos.x <= edgeScrollSensitivity) inputDirection.x -= 1f;
            if (mousePos.y >= Screen.height - edgeScrollSensitivity) inputDirection.y += 1f;
            if (mousePos.y <= edgeScrollSensitivity) inputDirection.y -= 1f;
        }

        inputDirection.Normalize();

        // 3. CÁLCULO RELATIVO A LA ROTACIÓN DE LA CÁMARA (45°)
        Vector3 camForward = transform.forward;
        Vector3 camRight = transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredMove = (camForward * inputDirection.y) + (camRight * inputDirection.x);
        targetPosition += desiredMove * moveSpeed * Time.deltaTime;

        // 4. ZOOM HACIA DELANTE / ATRÁS (Siguiendo el vector de visión real de la cámara)
        // 4. ZOOM HACIA DELANTE / ATRÁS CONTROLADO
        if (mouse != null)
        {
            float scrollValue = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollValue) > 0.01f)
            {
                float zoomDelta = scrollValue * zoomSpeed * 0.01f;

                // Calculamos cuál sería la siguiente posición Y si hacemos zoom
                float nextY = targetPosition.y + (transform.forward.y * zoomDelta);

                // Solo permitimos el movimiento de zoom si estamos dentro del rango permitido
                if (nextY >= minHeight && nextY <= maxHeight)
                {
                    targetPosition += transform.forward * zoomDelta;
                }
            }
        }

        // 5. CLAMPING DE ALTURA (Y) 
        // Al moverse en diagonal a 45°, limitar la altura (Y) evita que la cámara atraviese el suelo o suba demasiado.
        targetPosition.y = Mathf.Clamp(targetPosition.y, minHeight, maxHeight);

        // 6. CLAMPING DE LÍMITES EN X y Z (Mapa)
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);

        // 7. APLICAR MOVIMIENTO SUAVIZADO (SmoothDamp)
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}