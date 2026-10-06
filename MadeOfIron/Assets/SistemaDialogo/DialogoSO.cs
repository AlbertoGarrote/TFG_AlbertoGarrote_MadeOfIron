using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LineaDialogo
{
    public string nombrePersonaje;
    [TextArea(3, 5)] public string texto;
    public Sprite retrato;
    public AudioClip sonidoVoz;
}

[System.Serializable]
public class OpcionDialogo
{
    public string textoOpcion;
    public DialogoSO siguienteDialogo;

    [Header("Consecuencia al seleccionar")]
    public string escenaACargar; // Si no está vacío, cambia de escena al elegir esta opción
}

[CreateAssetMenu(fileName = "NuevoDialogo", menuName = "Sistema Dialogo/Dialogo")]
public class DialogoSO : ScriptableObject
{
    public List<LineaDialogo> lineas = new List<LineaDialogo>();
    public List<OpcionDialogo> opciones = new List<OpcionDialogo>();
}