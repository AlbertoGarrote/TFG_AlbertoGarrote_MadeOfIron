using UnityEngine;
using TMPro;

public class BuildingObject : MonoBehaviour
{
    [Header("Información del Edificio")]
    public BuildingItemSO buildingData;

    private Canvas worldCanvas;
    private TextMeshProUGUI infoText;
    private Renderer[] renderers;
    private Color[] originalColors;

    public void Initialize(BuildingItemSO data)
    {
        buildingData = data;

        // Asignar capa "Building" automáticamente a todos los hijos
        int buildingLayerIndex = LayerMask.NameToLayer("Building");
        if (buildingLayerIndex != -1)
        {
            gameObject.layer = buildingLayerIndex;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = buildingLayerIndex;
            }
        }

        renderers = GetComponentsInChildren<Renderer>();

        // Guardar colores originales para poder restaurarlos
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else if (renderers[i].material.HasProperty("_BaseColor"))
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
        }

        CreateWorldTextUI();
    }

    private void CreateWorldTextUI()
    {
        // 1. Crear el objeto Canvas
        GameObject canvasObj = new GameObject("BuildingUI");
        canvasObj.transform.SetParent(transform);

        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;

        // Ajustar el tamaño y escala para World Space
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(5f, 2f);
        canvasObj.transform.localScale = Vector3.one;

        // 2. Posicionar el Canvas exactamente encima del centro geométrico del edificio
        PositionCanvasAboveBuilding();

        // 3. Crear el componente de Texto
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform, false);

        infoText = textObj.AddComponent<TextMeshProUGUI>();

        // Asignar fuente por defecto de TextMeshPro si falta
        if (infoText.font == null)
        {
            infoText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        infoText.alignment = TextAlignmentOptions.Center;
        infoText.fontSize = 0.5f;
        infoText.color = Color.yellow;
        infoText.rectTransform.sizeDelta = new Vector2(5f, 2f);

        string nameText = buildingData != null ? buildingData.buildingName : "Edificio";
        int w = buildingData != null ? buildingData.width : 1;
        int l = buildingData != null ? buildingData.length : 1;

        infoText.text = $"<b>{nameText}</b>\nOcupa: {w}x{l}";

        // Ocultar al inicio
        canvasObj.SetActive(false);
    }

    private void PositionCanvasAboveBuilding()
    {
        if (worldCanvas == null) return;

        // Calcular los límites 3D (Bounds) combinando todos los Renderers del edificio
        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer rend in renderers)
        {
            if (rend != null && rend != worldCanvas.GetComponent<Renderer>())
            {
                if (!hasBounds)
                {
                    combinedBounds = rend.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rend.bounds);
                }
            }
        }

        // Determinar la posición superior central
        Vector3 topCenter = hasBounds
            ? new Vector3(combinedBounds.center.x, combinedBounds.max.y + 0.8f, combinedBounds.center.z)
            : transform.position + new Vector3(0, 2.5f, 0);

        // Asignar la posición en coordenadas del mundo
        worldCanvas.transform.position = topCenter;
    }

    public void SetSelected(bool isSelected, Material highlightMaterial = null)
    {
        if (worldCanvas != null)
        {
            worldCanvas.gameObject.SetActive(isSelected);

            if (isSelected)
            {
                // Re-calcular la posición por si el edificio se ha movido
                PositionCanvasAboveBuilding();

                // Orientar el texto hacia la cámara activa para que no se vea del revés
                if (Camera.main != null)
                {
                    worldCanvas.transform.rotation = Quaternion.LookRotation(worldCanvas.transform.position - Camera.main.transform.position);
                }
            }
        }

        // Aplicar o quitar el color morado de selección
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            if (isSelected)
            {
                if (highlightMaterial != null)
                {
                    renderers[i].material = highlightMaterial;
                }
                else
                {
                    if (renderers[i].material.HasProperty("_Color"))
                        renderers[i].material.color = Color.magenta;
                    else if (renderers[i].material.HasProperty("_BaseColor"))
                        renderers[i].material.SetColor("_BaseColor", Color.magenta);
                }
            }
            else
            {
                if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = originalColors[i];
                else if (renderers[i].material.HasProperty("_BaseColor"))
                    renderers[i].material.SetColor("_BaseColor", originalColors[i]);
            }
        }
    }
}