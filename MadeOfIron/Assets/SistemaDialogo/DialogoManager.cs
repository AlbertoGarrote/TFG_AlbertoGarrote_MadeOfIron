using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DialogoManager : MonoBehaviour
{
    public static DialogoManager Instance { get; private set; }

    [Header("UI de Diálogo")]
    [SerializeField] private GameObject panelDialogo;
    [SerializeField] private TMP_Text textoNombre;
    [SerializeField] private TMP_Text textoContenido;
    [SerializeField] private RawImage imagenRetrato;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float velocidadEscritura = 0.03f;

    [Header("UI de Opciones (Max 2 Opciones)")]
    [SerializeField] private GameObject panelOpciones;
    [SerializeField] private TMP_Text textoOpcion1;
    [SerializeField] private TMP_Text textoOpcion2;
    [SerializeField] private Color colorSeleccionado = Color.yellow;
    [SerializeField] private Color colorNormal = Color.white;

    public bool EnDialogo { get; private set; }

    private DialogoSO dialogoActual;
    private int indiceLinea;

    private Coroutine corrutinaEscritura;
    private bool estaEscribiendo;
    private string textoCompletoLinea;

    private bool mostrandoOpciones;
    private int opcionSeleccionada = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!EnDialogo) return;

        // 1. Navegación en el menú de opciones
        if (mostrandoOpciones)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                if (dialogoActual.opciones.Count > 1)
                {
                    opcionSeleccionada = (opcionSeleccionada == 0) ? 1 : 0;
                    ActualizarVisualizacionOpciones();
                }
            }

            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ConfirmarOpcion();
            }

            return;
        }

        // 2. Avanzar o autocompletar texto
        if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            if (estaEscribiendo)
            {
                CompletarTextoInmediato();
            }
            else
            {
                AvanzarDialogo();
            }
        }
    }

    public void IniciarDialogo(DialogoSO dialogo)
    {
        if (dialogo == null || dialogo.lineas.Count == 0) return;

        dialogoActual = dialogo;
        indiceLinea = 0;
        EnDialogo = true;
        panelDialogo.SetActive(true);

        OcultarOpciones();
        MostrarLinea();
    }

    private void MostrarLinea()
    {
        LineaDialogo linea = dialogoActual.lineas[indiceLinea];

        if (textoNombre != null) textoNombre.text = linea.nombrePersonaje;

        if (imagenRetrato != null)
        {
            imagenRetrato.gameObject.SetActive(linea.retrato != null);
            if (linea.retrato != null)
                imagenRetrato.texture = linea.retrato.texture;
        }

        if (corrutinaEscritura != null)
            StopCoroutine(corrutinaEscritura);

        textoCompletoLinea = linea.texto;
        corrutinaEscritura = StartCoroutine(EscribirTexto(linea));
    }

    private IEnumerator EscribirTexto(LineaDialogo linea)
    {
        estaEscribiendo = true;
        textoContenido.text = "";

        foreach (char letra in linea.texto.ToCharArray())
        {
            textoContenido.text += letra;

            if (linea.sonidoVoz != null && audioSource != null && !char.IsWhiteSpace(letra))
            {
                audioSource.PlayOneShot(linea.sonidoVoz);
            }

            yield return new WaitForSeconds(velocidadEscritura);
        }

        estaEscribiendo = false;
    }

    private void CompletarTextoInmediato()
    {
        if (corrutinaEscritura != null)
            StopCoroutine(corrutinaEscritura);

        textoContenido.text = textoCompletoLinea;
        estaEscribiendo = false;
    }

    public void AvanzarDialogo()
    {
        indiceLinea++;

        if (indiceLinea < dialogoActual.lineas.Count)
        {
            MostrarLinea();
        }
        else
        {
            if (dialogoActual.opciones != null && dialogoActual.opciones.Count > 0)
            {
                MostrarOpciones();
            }
            else
            {
                FinalizarDialogo();
            }
        }
    }

    private void MostrarOpciones()
    {
        mostrandoOpciones = true;
        opcionSeleccionada = 0;

        if (panelOpciones != null) panelOpciones.SetActive(true);

        ActualizarVisualizacionOpciones();
    }

    private void ActualizarVisualizacionOpciones()
    {
        if (dialogoActual.opciones.Count > 0 && textoOpcion1 != null)
        {
            string prefijo = (opcionSeleccionada == 0) ? "> " : "  ";
            textoOpcion1.text = prefijo + dialogoActual.opciones[0].textoOpcion;
            textoOpcion1.color = (opcionSeleccionada == 0) ? colorSeleccionado : colorNormal;
        }

        if (textoOpcion2 != null)
        {
            if (dialogoActual.opciones.Count > 1)
            {
                textoOpcion2.gameObject.SetActive(true);
                string prefijo = (opcionSeleccionada == 1) ? "> " : "  ";
                textoOpcion2.text = prefijo + dialogoActual.opciones[1].textoOpcion;
                textoOpcion2.color = (opcionSeleccionada == 1) ? colorSeleccionado : colorNormal;
            }
            else
            {
                textoOpcion2.gameObject.SetActive(false);
            }
        }
    }

    private void ConfirmarOpcion()
    {
        OpcionDialogo opcion = dialogoActual.opciones[opcionSeleccionada];

        OcultarOpciones();

        // Si la opción tiene una escena asignada, cambiamos de escena inmediatamente
        if (!string.IsNullOrEmpty(opcion.escenaACargar))
        {
            CerrarPanelEInmediato();
            return;
        }

        // Si la opción continúa a otro diálogo (árbol)
        if (opcion.siguienteDialogo != null)
        {
            IniciarDialogo(opcion.siguienteDialogo);
        }
        else
        {
            FinalizarDialogo();
        }
    }

    private void OcultarOpciones()
    {
        mostrandoOpciones = false;
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    private void CerrarPanelEInmediato()
    {
        if (corrutinaEscritura != null)
            StopCoroutine(corrutinaEscritura);

        estaEscribiendo = false;
        panelDialogo.SetActive(false);
        OcultarOpciones();
        EnDialogo = true; // Previene que se vuelva a abrir durante la transición
    }

    private void FinalizarDialogo()
    {
        if (corrutinaEscritura != null)
            StopCoroutine(corrutinaEscritura);

        estaEscribiendo = false;
        panelDialogo.SetActive(false);
        OcultarOpciones();

        StartCoroutine(ResetearEstadoDialogo());
    }

    private IEnumerator ResetearEstadoDialogo()
    {
        yield return null;
        EnDialogo = false;
    }
}