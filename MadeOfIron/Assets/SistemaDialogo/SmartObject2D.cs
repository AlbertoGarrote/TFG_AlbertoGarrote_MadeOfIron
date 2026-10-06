using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class SmartObject2D : MonoBehaviour
{
    [Header("UI Feedback")]
    [SerializeField] private GameObject iconoInteraccion;

    [Header("Acciones a Ejecutar")]
    [SerializeField] private List<AccionSmartObject> acciones;

    [Header("Opciones")]
    [SerializeField] private bool deUnSoloUso = false;

    private bool jugadorEnRango = false;
    private bool yaFueUsado = false;
    private bool ejecutandoAcciones = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (yaFueUsado) return;

        if (collision.CompareTag("Player"))
        {

            if (iconoInteraccion != null)
                iconoInteraccion.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorEnRango = false;

            if (iconoInteraccion != null)
                iconoInteraccion.SetActive(false);
        }
    }

    private void Update()
    {
        if (yaFueUsado || !jugadorEnRango) return;

        if (DialogoManager.Instance != null && DialogoManager.Instance.EnDialogo) return;

        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
        {
            
            StartCoroutine(EjecutarSecuenciaAcciones());
            
        }
    }

    private IEnumerator EjecutarSecuenciaAcciones()
    {
        if (acciones == null || acciones.Count == 0) yield break;

        ejecutandoAcciones = true;

        if (iconoInteraccion != null)
            iconoInteraccion.SetActive(false);

        if (deUnSoloUso)
            yaFueUsado = true;

        // Ejecuta cada acción de la lista esperando a que la previa termine
        foreach (var accion in acciones)
        {
            if (accion != null)
            {
                bool accionFinalizada = false;

                // Esperamos aquí mientras la acción esté en proceso (por ej. mientras dure el diálogo)
                yield return new WaitUntil(() => accionFinalizada);
            }
        }

        ejecutandoAcciones = false;
    }
}