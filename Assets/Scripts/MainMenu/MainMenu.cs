using System.IO;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu; // Referencia al UI del men� principal
    public GameObject optionsMenu; // Referencia al UI del men� de opciones
    public GameObject playMenu; // Referencia al UI del men� de juego

    public TMP_InputField worldName; // Referencia al campo de entrada de texto para el nombre del mundo
    public TMP_InputField prompt; // Referencia al campo de entrada de texto para el prompt

    public GameObject alertPanel; // Referencia al panel de alerta
    public TMP_Text alertText;    // Referencia al texto de la alerta

    private GameObject menu; // Referencia al men� actual

    public void Start()
    {
        // Asegurarse de que solo el men� principal est� activo al inicio
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        playMenu.SetActive(false);
        alertPanel.SetActive(false);

        var root = Application.dataPath;
        var projectRoot = Directory.GetParent(root).FullName;
        var envFilePath = Path.Combine(projectRoot, ".env");
        DotEnv.Load(envFilePath);

        GenerationOrquestrator.Instance.Initialize("prueba", "Quiero un mundo de fantas�a medieval pero que tambi�n tenga cosas futuristas, como robots y magia al mismo tiempo. El mapa debe ser grande, pero tampoco tanto, y que haya muchos personajes distintos, pero no s� cu�ntos. Me gustar�a que haya ciudades flotantes y bosques m�gicos, pero que no sea igual que otros juegos famosos. No s� si prefiero que haya misiones principales o solo secundarias, lo que veas mejor. El clima deber�a cambiar, pero no todo el tiempo. Ah, y que haya dragones, pero que no sean malos siempre.\r\n");
        GenerationOrquestrator.Instance.StartGeneration();
    }

    public void OpenOptionsPanel()
    {
        // Desactivar el men� principal y activar el men� de opciones
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
        playMenu.SetActive(false);
    }

    public void OpenMainMenu()
    {
        // Desactivar el men� de opciones y activar el men� principal
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
        playMenu.SetActive(false);
    }

    public void OpenPlayMenu()
    {
        // Desactivar el men� principal y activar el men� de juego
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playMenu.SetActive(true);
    }

    public void Exit()
    {
        Application.Quit();
        // Si estamos en el editor de Unity, detener la reproducci�n
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void NewWorld()
    {
        menu = playMenu; // Guardar la referencia al men� de juego

        

        if (string.IsNullOrEmpty(worldName.text) || string.IsNullOrEmpty(prompt.text))
        {
            ShowAlert("El nombre del mundo y el prompt no pueden estar vac�os.");
            return;
        }
        
        
        return;

        
        

    }

    public void ShowAlert(string message)
    {
        // Ocultar el men� actual y mostrar el panel de alerta
        menu.SetActive(false);

        alertText.text = message;
        alertPanel.SetActive(true);
    }

    public void HideAlert()
    {
        alertPanel.SetActive(false);

        // Volver a mostrar el men� que estaba activo antes de la alerta
        if (menu != null)
        {
            menu.SetActive(true);
        }
    }
}
