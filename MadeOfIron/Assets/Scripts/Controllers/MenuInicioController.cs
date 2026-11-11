using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuInicioController : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private TMP_InputField inputUsuario;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private TextMeshProUGUI textoError; // Para mostrar feedback en pantalla

    /// <summary>
    /// Se llama desde el botón de "Iniciar Sesión"
    /// </summary>
    public void OnClickLoginButton()
    {
        string user = inputUsuario.text.Trim();
        string pass = inputPassword.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            MostrarError("Introduce el usuario y la contraseña.");
            return;
        }

        var accountManager = ServiceLocator.Get<AccountManager>();
        if (accountManager != null)
        {
            bool success = accountManager.IniciarSesion(user, pass);

            if (success)
            {
                if (textoError != null) textoError.text = "";
                Debug.Log("[MenuInicioController] Login correcto. Cargando BuildScene...");
                SceneManager.LoadScene("BaseScene");
            }
            else
            {
                MostrarError("Usuario no encontrado o contraseña incorrecta.");
            }
        }
        else
        {
            MostrarError("Error: AccountManager no encontrado.");
        }
    }

    /// <summary>
    /// Se llama desde el botón de "Registrarse"
    /// </summary>
    public void OnClickRegisterButton()
    {
        string user = inputUsuario.text.Trim();
        string pass = inputPassword.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            MostrarError("Introduce un usuario y contraseña para registrarte.");
            return;
        }

        var accountManager = ServiceLocator.Get<AccountManager>();
        if (accountManager != null)
        {
            bool success = accountManager.RegistrarCuenta(user, pass);

            if (success)
            {
                if (textoError != null) textoError.text = "";
                Debug.Log("[MenuInicioController] Registro correcto. Cargando BuildScene...");
                SceneManager.LoadScene("BaseScene");
            }
            else
            {
                MostrarError("El nombre de usuario ya está en uso. Prueba con otro.");
            }
        }
        else
        {
            MostrarError("Error: AccountManager no encontrado.");
        }
    }

    private void MostrarError(string mensaje)
    {
        Debug.LogWarning($"[MenuInicioController] {mensaje}");
        if (textoError != null)
        {
            textoError.text = mensaje;
        }
    }
}