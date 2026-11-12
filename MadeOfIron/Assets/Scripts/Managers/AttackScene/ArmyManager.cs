using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TropaConfig
{
    public string idTropa;              // Identificador único (ej: "Soldado", "Tanque")
    public GameObject prefabTropa;      // Prefab que se instanciará en la batalla
    public int cantidadActual = 0;      // Cuántas ha seleccionado el jugador
    public TextMeshProUGUI textoContador; // Referencia al texto de la UI que muestra el número
}

public class ArmyManager : MonoBehaviour
{
    [Header("Tipos de Tropas Disponibles")]
    public List<TropaConfig> listaTropas = new List<TropaConfig>();

    private void Awake()
    {
        // Registrar en el ServiceLocator para que el UIManager pueda acceder fácilmente
        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.Get<ArmyManager>() == this)
        {
            ServiceLocator.Unregister<ArmyManager>();
        }
    }

    // Método para aumentar la cantidad de un tipo de tropa (Asociar al botón +)
    public void IncrementarTropa(int index)
    {
        if (index >= 0 && index < listaTropas.Count)
        {
            listaTropas[index].cantidadActual++;
            ActualizarUI(index);
        }
    }

    // Método para disminuir la cantidad de un tipo de tropa (Asociar al botón -)
    public void DecrementarTropa(int index)
    {
        if (index >= 0 && index < listaTropas.Count)
        {
            if (listaTropas[index].cantidadActual > 0)
            {
                listaTropas[index].cantidadActual--;
                ActualizarUI(index);
            }
        }
    }

    private void ActualizarUI(int index)
    {
        var tropa = listaTropas[index];
        if (tropa.textoContador != null)
        {
            tropa.textoContador.text = tropa.cantidadActual.ToString();
        }
    }

    /// <summary>
    /// Empaqueta las tropas seleccionadas (cantidad mayor a 0) en un diccionario para enviarlas al GameManager.
    /// </summary>
    public Dictionary<string, int> ObtenerTropasSeleccionadas()
    {
        Dictionary<string, int> resultado = new Dictionary<string, int>();
        foreach (var tropa in listaTropas)
        {
            if (tropa.cantidadActual > 0)
            {
                resultado.Add(tropa.idTropa, tropa.cantidadActual);
            }
        }
        return resultado;
    }
}