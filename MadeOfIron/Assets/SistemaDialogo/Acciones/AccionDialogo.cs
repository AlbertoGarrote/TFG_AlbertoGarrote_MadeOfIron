using System;
using System.Collections;
using UnityEngine;

public class AccionDialogo : AccionSmartObject
{
    [SerializeField] private DialogoSO dialogo;

    public override void EjecutarAccion(GameObject interactor, Action alTerminar)
    {
        if (DialogoManager.Instance != null && dialogo != null)
        {
            DialogoManager.Instance.IniciarDialogo(dialogo);
            StartCoroutine(EsperarFinDialogo(alTerminar));
        }
        else
        {
            alTerminar?.Invoke();
        }
    }

    private IEnumerator EsperarFinDialogo(Action alTerminar)
    {
        yield return new WaitUntil(() => !DialogoManager.Instance.EnDialogo);
        alTerminar?.Invoke();
    }
}