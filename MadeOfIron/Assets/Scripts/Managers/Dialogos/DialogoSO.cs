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

[CreateAssetMenu(fileName = "NuevoDialogo", menuName = "Sistema Dialogo/Dialogo")]
public class DialogoSO : ScriptableObject
{
    public List<LineaDialogo> lineas = new List<LineaDialogo>();
}