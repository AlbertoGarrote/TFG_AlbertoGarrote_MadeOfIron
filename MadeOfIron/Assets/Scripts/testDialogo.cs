using UnityEngine;
using UnityEngine.InputSystem;

public class testDialogo : MonoBehaviour
{
    [Header("Configuración de Prueba")]
    [SerializeField] private DialogoSO dialogoPrueba;

    private void Update()
    {
        // Comprobamos si el teclado está activo y se pulsa la tecla 'P'
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            var dialogoManager = ServiceLocator.Get<DialogoManager>();

            if (dialogoManager != null)
            {
                // Solo iniciamos si no hay otro diálogo ya en marcha y tenemos el SO asignado
                if (!dialogoManager.EnDialogo && dialogoPrueba != null)
                {
                    dialogoManager.IniciarDialogo(dialogoPrueba);
                    Debug.Log("[DialogueTestTrigger] ¡Diálogo de prueba lanzado!");
                }
                else if (dialogoPrueba == null)
                {
                    Debug.LogWarning("[DialogueTestTrigger] Falta asignar un ScriptableObject de Diálogo en el Inspector.");
                }
            }
            else
            {
                Debug.LogError("[DialogueTestTrigger] No se encontró el DialogoManager registrado en el ServiceLocator.");
            }
        }
    }
}