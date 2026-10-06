using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AccionCargarEscena : AccionSmartObject
{
    [SerializeField] private string nombreEscena;

    public override void EjecutarAccion(GameObject interactor, Action alTerminar)
    {
        if (!string.IsNullOrEmpty(nombreEscena))
        {
            SceneManager.LoadScene(nombreEscena);
        }

        alTerminar?.Invoke();
    }
}