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

    private int contadorSonido = 0;
    [SerializeField] private int frecuenciaSonido = 2;

    public bool EnDialogo { get; private set; }

    private DialogoSO dialogoActual;
    private int indiceLinea;

    private Coroutine corrutinaEscritura;
    private bool estaEscribiendo;
    private string textoCompletoLinea;
    private bool recienAbierto;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Registrar en el ServiceLocator
        ServiceLocator.Register(this);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<DialogoManager>();
        }
    }

    private void Update()
    {
        if (!EnDialogo) return;

        if (recienAbierto)
        {
            recienAbierto = false;
            return;
        }

        // Avanzar o autocompletar texto mediante la tecla E o Espacio
        if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            if (estaEscribiendo)
            {
                CompletarTextoInmediato();
            }
            else
            {
                ReproducirSonidoInteractuar();
                AvanzarDialogo();
            }
        }
    }

    private void ReproducirSonidoInteractuar()
    {
        var soundController = ServiceLocator.Get<MusicSoundManager>();
        if (soundController != null)
        {
            soundController.ReproducirSFXGlobal("Selected");
        }
    }

    public void IniciarDialogo(DialogoSO dialogo)
    {
        if (dialogo == null || dialogo.lineas.Count == 0) return;

        dialogoActual = dialogo;
        indiceLinea = 0;
        EnDialogo = true;
        if (panelDialogo != null) panelDialogo.SetActive(true);

        MostrarLinea();
        recienAbierto = true;
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
        if (textoContenido != null) textoContenido.text = "";
        contadorSonido = 0;

        foreach (char letra in linea.texto.ToCharArray())
        {
            if (textoContenido != null) textoContenido.text += letra;

            // Solo reproducimos sonido si hay letra, hay audio y toca según la frecuencia
            if (linea.sonidoVoz != null && audioSource != null && !char.IsWhiteSpace(letra))
            {
                contadorSonido++;
                if (contadorSonido >= frecuenciaSonido)
                {
                    audioSource.PlayOneShot(linea.sonidoVoz, 0.5f);
                    contadorSonido = 0;
                }
            }

            yield return new WaitForSeconds(velocidadEscritura);
        }

        estaEscribiendo = false;
    }

    private void CompletarTextoInmediato()
    {
        if (corrutinaEscritura != null)
            StopCoroutine(corrutinaEscritura);

        if (textoContenido != null) textoContenido.text = textoCompletoLinea;
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
            FinalizarDialogo();
        }
    }

    private void FinalizarDialogo()
    {
        if (corrutinaEscritura != null)
            StopCoroutine(corrutinaEscritura);

        estaEscribiendo = false;
        if (panelDialogo != null) panelDialogo.SetActive(false);

        StartCoroutine(ResetearEstadoDialogo());
    }

    private IEnumerator ResetearEstadoDialogo()
    {
        yield return null;
        EnDialogo = false;
    }
}