using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BuildingHealth))]
public class BuildingHealthBar : MonoBehaviour
{
    private BuildingHealth buildingHealth;
    private GameObject canvasObj;
    private Slider healthSlider;
    private Camera mainCamera;

    private void Awake()
    {
        buildingHealth = GetComponent<BuildingHealth>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        CrearBarraDeVidaUI();

        // Nos suscribimos al evento de cambio de vida
        buildingHealth.OnHealthChanged += ActualizarBarra;

        // Ocultar la barra al inicio (solo se muestra al recibir daño)
        if (canvasObj != null) canvasObj.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buildingHealth != null)
        {
            buildingHealth.OnHealthChanged -= ActualizarBarra;
        }
    }

    private void LateUpdate()
    {
        // Forzar que la barra mire siempre hacia la cámara pero orientada exclusivamente en el plano horizontal (Eje Y)
        if (canvasObj != null && canvasObj.activeSelf && mainCamera != null)
        {
            // Copiamos la rotación horizontal de la cámara para que el texto/barra nunca se incline ni se ponga boca abajo
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f; // Bloqueamos la inclinación vertical

            if (camForward.sqrMagnitude > 0.001f)
            {
                // Hacemos que la barra mire exactamente en la dirección opuesta a la cámara (hacia el frente del jugador)
                canvasObj.transform.rotation = Quaternion.LookRotation(-camForward);
            }
        }
    }

    private void CrearBarraDeVidaUI()
    {
        // 1. Crear Canvas en World Space
        canvasObj = new GameObject("HealthBarCanvas", typeof(RectTransform));
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2f, 0.4f); // Tamaño físico en el mundo

        // Posicionar un poco por encima del edificio
        canvasRect.localPosition = new Vector3(0f, 2.5f, 0f);
        canvasObj.transform.localScale = Vector3.one;

        // 2. Crear el Slider de fondo
        GameObject sliderObj = new GameObject("SliderHealth", typeof(RectTransform));
        sliderObj.transform.SetParent(canvasObj.transform, false);

        healthSlider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(1.8f, 0.25f);
        sliderRect.anchoredPosition = Vector2.zero;

        // 3. Crear el fondo del Slider (Background)
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        ConfigureRectTransform(bgObj.GetComponent<RectTransform>());

        // 4. Crear el área de relleno (Fill Area)
        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        ConfigureRectTransform(fillAreaObj.GetComponent<RectTransform>());

        // 5. Crear la barra de relleno (Fill)
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.GetComponent<Image>();
        fillImage.color = Color.green; // Color verde para la salud
        ConfigureRectTransform(fillObj.GetComponent<RectTransform>());

        // Asignar referencias al componente Slider
        healthSlider.targetGraphic = fillImage;
        healthSlider.fillRect = fillObj.GetComponent<RectTransform>();
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;
    }

    private void ConfigureRectTransform(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private void ActualizarBarra(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;

            // Mostrar la barra de vida en cuanto reciba el primer golpe
            if (canvasObj != null && !canvasObj.activeSelf)
            {
                canvasObj.SetActive(true);
            }
        }
    }
}