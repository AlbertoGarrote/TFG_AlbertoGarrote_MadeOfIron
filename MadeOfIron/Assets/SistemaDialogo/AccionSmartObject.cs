using System;
using UnityEngine;

public abstract class AccionSmartObject : MonoBehaviour
{
    public abstract void EjecutarAccion(GameObject interactor, Action alTerminar);
}