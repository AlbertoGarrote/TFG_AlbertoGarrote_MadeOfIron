using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Velocidad de Movimiento")]
    public float moveSpeed = 20f;
    public float edgeScrollSensitivity = 20f;

    [Header("Límites del Mapa (50x50 Grid)")]
    public float minX = -10f;
    public float maxX = 60f;
    public float minZ = -10f;
    public float maxZ = 60f;

    [Header("Opciones")]
    public bool enableEdgeScrolling = true;

    void Update()
    {
        Vector2 inputDirection = Vector2.zero;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

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

        // 3. CALCULO RELATIVO A LA ROTACIÓN DE LA CÁMARA (45°)
        // Obtenemos el vector 'adelante' y 'derecha' de la cámara
        Vector3 camForward = transform.forward;
        Vector3 camRight = transform.right;

        // Aplanamos los vectores para ignorar la inclinación vertical (Y = 0)
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculamos el movimiento real combinando la dirección de la cámara con el input
        Vector3 desiredMove = (camForward * inputDirection.y) + (camRight * inputDirection.x);

        // 4. Aplicar movimiento y Clamping
        Vector3 newPosition = transform.position + (desiredMove * moveSpeed * Time.deltaTime);

        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);

        transform.position = newPosition;
    }
}