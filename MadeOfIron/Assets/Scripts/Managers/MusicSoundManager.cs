using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicSoundManager : MonoBehaviour
{
    public static MusicSoundManager Instance { get; private set; }

    [System.Serializable]
    public struct SonidoAudio
    {
        public string nombreIdentificador;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumen;
        [Range(0.1f, 3f)] public float pitch;
        public bool esMusica; // True = Música global, False = SFX
        public bool loop;     // Solo aplica a música
    }

    [Header("Configuración de Audio")]
    [SerializeField] private AudioSource fuenteMusica;
    [SerializeField] private AudioSource fuenteSFXGlobal; // Para UI o efectos 2D generales
    [SerializeField] private AudioMixerGroup grupoMixerMusica; // Opcional para asignar en el AudioMixer
    [SerializeField] private AudioMixerGroup grupoMixerSFX;    // Opcional para asignar en el AudioMixer
    [SerializeField] private AudioMixer audioMixer;           // Para los Sliders del menú de opciones

    [Header("Base de Datos de Sonidos")]
    [SerializeField] private List<SonidoAudio> catalogoSonidos = new List<SonidoAudio>();

    [System.Serializable]
    public struct MusicaPorEscena
    {
        public string nombreEscena;
        public string nombreSonidoMusica;
    }
    [SerializeField] private List<MusicaPorEscena> musicaPorEscenaList = new List<MusicaPorEscena>();

    private Dictionary<string, SonidoAudio> diccionarioSonidos = new Dictionary<string, SonidoAudio>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Registrar en el ServiceLocator para acceso limpio
        ServiceLocator.Register(this);

        InicializarCatalogo();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<MusicSoundManager>();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InicializarCatalogo()
    {
        diccionarioSonidos.Clear();
        foreach (var sonido in catalogoSonidos)
        {
            if (!diccionarioSonidos.ContainsKey(sonido.nombreIdentificador))
            {
                diccionarioSonidos.Add(sonido.nombreIdentificador, sonido);
            }
            else
            {
                Debug.LogWarning($"[MusicSoundController] El sonido '{sonido.nombreIdentificador}' está repetido en el catálogo.");
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Música global automática según la escena cargada
        foreach (var item in musicaPorEscenaList)
        {
            if (item.nombreEscena == scene.name)
            {
                ReproducerMusica(item.nombreSonidoMusica);
                return;
            }
        }
    }

    // ==========================================
    // GESTIÓN DE MÚSICA (GLOBAL)
    // ==========================================

    public void ReproducerMusica(string nombreSonido)
    {
        if (diccionarioSonidos.TryGetValue(nombreSonido, out SonidoAudio sonido))
        {
            if (sonido.esMusica)
            {
                if (fuenteMusica.isPlaying && fuenteMusica.clip == sonido.clip) return;

                fuenteMusica.Stop();
                fuenteMusica.clip = sonido.clip;
                fuenteMusica.volume = sonido.volumen;
                fuenteMusica.pitch = sonido.pitch;
                fuenteMusica.loop = sonido.loop;

                if (grupoMixerMusica != null) fuenteMusica.outputAudioMixerGroup = grupoMixerMusica;

                fuenteMusica.Play();
            }
            else
            {
                Debug.LogWarning($"[MusicSoundController] '{nombreSonido}' es un SFX, no se puede reproducir como Música.");
            }
        }
        else
        {
            Debug.LogWarning($"[MusicSoundController] No se encontró el tema musical: '{nombreSonido}'");
        }
    }

    public void DetenerMusica()
    {
        if (fuenteMusica.isPlaying) fuenteMusica.Stop();
    }

    // ==========================================
    // GESTIÓN DE EFECTOS DE SONIDO (SFX)
    // ==========================================

    /// <summary>
    /// Reproduce un SFX global 2D (Ideal para UI, clics de botones, notificaciones).
    /// </summary>
    public void ReproducirSFXGlobal(string nombreSonido)
    {
        if (diccionarioSonidos.TryGetValue(nombreSonido, out SonidoAudio sonido))
        {
            if (!sonido.esMusica && fuenteSFXGlobal != null)
            {
                fuenteSFXGlobal.pitch = sonido.pitch;
                fuenteSFXGlobal.PlayOneShot(sonido.clip, sonido.volumen);
            }
        }
        else
        {
            Debug.LogWarning($"[MusicSoundController] No se encontró el SFX global: '{nombreSonido}'");
        }
    }

    /// <summary>
    /// Reproduce un SFX local en una posición 3D del mundo (Ideal para disparos, explosiones, golpes en batalla).
    /// Crea un objeto temporal que se autodestruye al terminar el audio, permitiendo escuchar múltiples sonidos a la vez en cualquier lugar del mapa.
    /// </summary>
    public void ReproducirSFX3D(string nombreSonido, Vector3 posicionMundo)
    {
        if (diccionarioSonidos.TryGetValue(nombreSonido, out SonidoAudio sonido))
        {
            if (!sonido.esMusica)
            {
                // Crear un GameObject temporal en la posición indicada
                GameObject tempAudioObj = new GameObject($"TempAudio3D_{nombreSonido}");
                tempAudioObj.transform.position = posicionMundo;

                AudioSource audioSource = tempAudioObj.AddComponent<AudioSource>();
                audioSource.clip = sonido.clip;
                audioSource.volume = sonido.volumen;
                audioSource.pitch = sonido.pitch;
                audioSource.spatialBlend = 1.0f; // 1.0 = Sonido 3D total en el espacio
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = 2f;
                audioSource.maxDistance = 35f;

                if (grupoMixerSFX != null) audioSource.outputAudioMixerGroup = grupoMixerSFX;

                audioSource.Play();

                // Destruir el objeto temporal exactamente cuando termine el clip de audio
                Destroy(tempAudioObj, sonido.clip.length / Mathf.Abs(sonido.pitch));
            }
            else
            {
                Debug.LogWarning($"[MusicSoundController] '{nombreSonido}' está marcado como Música, no como SFX 3D.");
            }
        }
        else
        {
            Debug.LogWarning($"[MusicSoundController] No se encontró el SFX 3D: '{nombreSonido}'");
        }
    }

    // ==========================================
    // CONTROL DE VOLUMEN (AUDIO MIXER)
    // ==========================================

    public void CambiarVolumenMusica(float valorSlider)
    {
        if (audioMixer != null)
        {
            float decibelios = (valorSlider <= 0.0001f) ? -80f : Mathf.Log10(valorSlider) * 20f;
            audioMixer.SetFloat("ParametroVolumenMusica", decibelios);
        }
    }

    public void CambiarVolumenSFX(float valorSlider)
    {
        if (audioMixer != null)
        {
            float decibelios = (valorSlider <= 0.0001f) ? -80f : Mathf.Log10(valorSlider) * 20f;
            audioMixer.SetFloat("ParametroVolumenSFX", decibelios);
        }
    }
}