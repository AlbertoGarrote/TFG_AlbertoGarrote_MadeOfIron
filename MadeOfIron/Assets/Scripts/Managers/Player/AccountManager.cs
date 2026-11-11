using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }

    // Almacenará los datos del jugador que ha iniciado sesión actualmente
    public PlayerData CurrentPlayerData { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Registrar en el ServiceLocator para poder pedirlo desde cualquier escena
        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister<AccountManager>();
        }
    }

    /// <summary>
    /// Intenta registrar una cuenta nueva. Si el usuario ya existe, devuelve falso.
    /// </summary>
    public bool RegistrarCuenta(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[AccountManager] El usuario y la contraseña no pueden estar vacíos.");
            return false;
        }

        string filePath = GetUserFilePath(username);

        if (File.Exists(filePath))
        {
            Debug.LogWarning($"[AccountManager] El usuario '{username}' ya existe. No se puede registrar de nuevo.");
            return false; // Error: Ya existe la cuenta
        }

        // Creamos el perfil nuevo
        CurrentPlayerData = new PlayerData
        {
            username = username.Trim(),
            password = password, // Guardado en texto plano por ahora
            playerLevel = 1,
            currentExp = 0,
            recursos = 500, // Recursos iniciales ("Chatarra")
            hasCompletedTutorial = false,
            placedBuildings = new List<PlacedBuildingData>()
        };

        SaveCurrentPlayerData();
        Debug.Log($"[AccountManager] Cuenta nueva registrada con éxito para: {username}");
        return true;
    }

    /// <summary>
    /// Intenta iniciar sesión con una cuenta existente. Si no existe o la contraseña falla, devuelve falso.
    /// </summary>
    public bool IniciarSesion(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[AccountManager] El usuario y la contraseña no pueden estar vacíos.");
            return false;
        }

        string filePath = GetUserFilePath(username);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[AccountManager] El usuario '{username}' no existe.");
            return false; // Error: La cuenta no existe
        }

        string jsonContent = File.ReadAllText(filePath);
        PlayerData loadedData = JsonUtility.FromJson<PlayerData>(jsonContent);

        if (loadedData.password == password)
        {
            CurrentPlayerData = loadedData;
            Debug.Log($"[AccountManager] Sesión iniciada con éxito para: {username}");
            return true;
        }
        else
        {
            Debug.LogWarning("[AccountManager] Contraseña incorrecta.");
            return false; // Error: Contraseña incorrecta
        }
    }

    /// <summary>
    /// Guarda el progreso actual y el diseño de la base del usuario activo.
    /// </summary>
    public void SaveCurrentPlayerData()
    {
        if (CurrentPlayerData == null) return;

        // Si estamos en la escena de base, actualizamos los edificios actuales antes de guardar
        var gridPlacement = ServiceLocator.Get<GridPlacementManager>();
        if (gridPlacement != null)
        {
            GridSaveData currentGrid = gridPlacement.ExportGridState();
            CurrentPlayerData.placedBuildings = currentGrid.placedBuildings;
        }

        string filePath = GetUserFilePath(CurrentPlayerData.username);
        string jsonOutput = JsonUtility.ToJson(CurrentPlayerData, true);
        File.WriteAllText(filePath, jsonOutput);

        Debug.Log($"[AccountManager] Datos del usuario '{CurrentPlayerData.username}' guardados correctamente.");
    }

    /// <summary>
    /// Devuelve la ruta local del archivo JSON para un usuario específico.
    /// </summary>
    private string GetUserFilePath(string username)
    {
        string safeUsername = username.Trim().ToLower();
        return Path.Combine(Application.persistentDataPath, $"save_{safeUsername}.json");
    }
}