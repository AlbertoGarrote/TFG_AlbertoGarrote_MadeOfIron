using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    // Diccionario interno que almacena los servicios usando su tipo como clave
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    /// <summary>
    /// Registra un servicio en el localizador.
    /// </summary>
    public static void Register<T>(T service)
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] El servicio {type.Name} ya estaba registrado. Sobrescribiendo...");
            services[type] = service;
        }
        else
        {
            services.Add(type, service);
            Debug.Log($"[ServiceLocator] Registrado con éxito: {type.Name}");
        }
    }

    /// <summary>
    /// Obtiene un servicio registrado de tipo T.
    /// </summary>
    public static T Get<T>()
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            return (T)services[type];
        }

        Debug.LogError($"[ServiceLocator] ¡Error! No se encontró ningún servicio registrado de tipo: {type.Name}");
        return default;
    }

    /// <summary>
    /// Elimina un servicio del registro.
    /// </summary>
    public static void Unregister<T>()
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            services.Remove(type);
            Debug.Log($"[ServiceLocator] Desregistrado: {type.Name}");
        }
    }

    /// <summary>
    /// Limpia todos los servicios (útil al cambiar de escena global).
    /// </summary>
    public static void Clear()
    {
        services.Clear();
    }
}