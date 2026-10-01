using UnityEngine;

public static class UtilsUI
{
    private static Texture2D whiteTexture;

    public static Texture2D WhiteTexture
    {
        get
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }
            return whiteTexture;
        }
    }

    // Dibuja el borde y el relleno translúcido de la caja de selección RTS
    public static void DrawScreenRect(Rect position, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(position, WhiteTexture);
        GUI.color = Color.white;
    }

    public static void DrawScreenRectBorder(Rect position, float thickness, Color color)
    {
        // Arriba, Abajo, Izquierda, Derecha
        DrawScreenRect(new Rect(position.xMin, position.yMin, position.width, thickness), color);
        DrawScreenRect(new Rect(position.xMin, position.yMax - thickness, position.width, thickness), color);
        DrawScreenRect(new Rect(position.xMin, position.yMin, thickness, position.height), color);
        DrawScreenRect(new Rect(position.xMax - thickness, position.yMin, thickness, position.height), color);
    }

    public static Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
    {
        // Convierte las coordenadas del mouse (con origen abajo-izquierda) a GUI (origen arriba-izquierda)
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;

        Vector3 topLeft = Vector3.Min(screenPosition1, screenPosition2);
        Vector3 bottomRight = Vector3.Max(screenPosition1, screenPosition2);

        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    public static Bounds GetViewportBounds(Camera camera, Vector3 screenPosition1, Vector3 screenPosition2)
    {
        Vector3 v1 = camera.ScreenToViewportPoint(screenPosition1);
        Vector3 v2 = camera.ScreenToViewportPoint(screenPosition2);

        Vector3 min = Vector3.Min(v1, v2);
        Vector3 max = Vector3.Max(v1, v2);
        min.z = camera.nearClipPlane;
        max.z = camera.farClipPlane;

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }
}