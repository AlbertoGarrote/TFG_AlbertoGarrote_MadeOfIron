using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class SimulatedMatchmakingManager
{
    public static string ObtenerBaseEnemigaAleatoria()
    {
        string folderPath = Application.persistentDataPath;

        // Comprobamos si la carpeta de guardados existe
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning("[Matchmaking] No existe la carpeta de guardados. Generando base enemiga vacía.");
            return "{\"placedBuildings\":[]}";
        }

        string[] saveFiles = Directory.GetFiles(folderPath, "save_*.json");

        // Obtener el nombre del usuario actual para excluirlo del matchmaking
        var accountManager = ServiceLocator.Get<AccountManager>();
        string currentPlayer = "";
        if (accountManager != null && accountManager.CurrentPlayerData != null)
        {
            currentPlayer = accountManager.CurrentPlayerData.username.ToLower();
        }

        // Filtrar ficheros para no elegir la base del propio jugador
        List<string> validFiles = new List<string>();
        foreach (var file in saveFiles)
        {
            // Extraer el nombre del fichero (ej: "save_pepe.json")
            string fileName = Path.GetFileName(file).ToLower();

            // Si el archivo no pertenece al usuario actual, es un enemigo válido
            if (string.IsNullOrEmpty(currentPlayer) || !fileName.Contains($"save_{currentPlayer}.json"))
            {
                validFiles.Add(file);
            }
        }

        // Si hay bases de otros jugadores disponibles, elegimos una al azar
        if (validFiles.Count > 0)
        {
            int randomIndex = Random.Range(0, validFiles.Count);
            string enemyJsonPath = validFiles[randomIndex];
            Debug.Log($"[Matchmaking] Base enemiga encontrada y seleccionada: {enemyJsonPath}");
            return File.ReadAllText(enemyJsonPath);
        }

        // Si estás tú solo en el sistema, devolvemos una estructura vacía por defecto
        Debug.LogWarning("[Matchmaking] No se han encontrado bases de otros jugadores. Generando base enemiga vacía por defecto.");
        return "{\"placedBuildings\":[]}";
    }
}