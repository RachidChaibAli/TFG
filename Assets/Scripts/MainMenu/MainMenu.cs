using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu; // Referencia al UI del menú principal
    public GameObject optionsMenu; // Referencia al UI del menú de opciones
    public GameObject playMenu; // Referencia al UI del menú de juego

    public TMP_InputField worldName; // Referencia al campo de entrada de texto para el nombre del mundo
    public TMP_InputField promt; // Referencia al campo de entrada de texto para el prompt

    public GameObject alertPanel; // Referencia al panel de alerta
    public TMP_Text alertText;    // Referencia al texto de la alerta

    private GameObject menu; // Referencia al menú actual

    public void Start()
    {
        // Asegurarse de que solo el menú principal esté activo al inicio
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        alertPanel.SetActive(false);
    }

    public void OpenOptionsPanel()
    {
        // Desactivar el menú principal y activar el menú de opciones
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
        playMenu.SetActive(false);
    }

    public void OpenMainMenu()
    {
        // Desactivar el menú de opciones y activar el menú principal
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
        playMenu.SetActive(false);
    }

    public void OpenPlayMenu()
    {
        // Desactivar el menú principal y activar el menú de juego
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(true);
    }

    public void Exit()
    {
        Application.Quit();
        // Si estamos en el editor de Unity, detener la reproducción
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void NewWorld()
    {
        menu = playMenu; // Guardar la referencia al menú de juego

        if (string.IsNullOrEmpty(worldName.text) || string.IsNullOrEmpty(promt.text))
        {
            ShowAlert("El nombre del mundo y el prompt no pueden estar vacíos.");
            return;
        }

        ShowAlert("Creando nuevo mundo: " + worldName.text);
    }

    public void ShowAlert(string message)
    {
        // Ocultar el menú actual y mostrar el panel de alerta
        menu.SetActive(false);

        alertText.text = message;
        alertPanel.SetActive(true);
    }

    public void HideAlert()
    {
        alertPanel.SetActive(false);

        // Volver a mostrar el menú que estaba activo antes de la alerta
        if (menu != null)
        {
            menu.SetActive(true);
        }
    }
}
