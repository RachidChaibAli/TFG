using GenerativeAI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SplitScenesClasses;

public class GenerationOrquestrator
{
    private static GenerationOrquestrator _instance;
    public static GenerationOrquestrator Instance
    {
        get
        {
            _instance ??= new GenerationOrquestrator();
            return _instance;
        }
    }

    public string WorldName { get; private set; }
    public string Prompt { get; private set; }
    public string InitialPrompt { get; private set; }
    public string SplitScenesPrompt { get; private set; }
    public string BasePrompt { get; private set; }
    public string WorldPath => Path.Combine(Application.persistentDataPath, WorldName);
    public List<Escena> Scenes { get; private set; } = new List<Escena>();
    public GoogleAi GoogleAi { get; private set; }
    public GenerativeModel GenerativeModel { get; private set; }

    private GenerationOrquestrator() { }

    public void Initialize(string worldName, string prompt)
    {
        WorldName = worldName;
        Prompt = prompt;

        string configPath = Path.Combine(Application.dataPath, "Scripts/GenerateWorld/ConfigPrompts");

        InitialPrompt = File.ReadAllText(Path.Combine(configPath, "initialPrompt.txt"));
        SplitScenesPrompt = File.ReadAllText(Path.Combine(configPath, "splitScenesPrompt.txt"));

        GoogleAi = new GoogleAi();
        GenerativeModel = GoogleAi.CreateGenerativeModel(GoogleAIModels.Gemini15Flash);

        // Crear una carpeta para el mundo si no existe
        if (!Directory.Exists(WorldPath))
        {
            Directory.CreateDirectory(WorldPath);
            Debug.Log($"Carpeta del mundo creada en: {WorldPath}");
        }
    }

    public void StartGeneration()
    {
        if (string.IsNullOrEmpty(WorldName) || string.IsNullOrEmpty(Prompt))
        {
            Debug.LogError("El nombre del mundo y el prompt no pueden estar vacíos.");
            return;
        }
        ParsePrompt();
        SplitPromptIntoScenes();
    }

    private async void ParsePrompt()
    {
        var response = await GenerativeModel.GenerateContentAsync(InitialPrompt + "\n" + Prompt);
        File.WriteAllText(Path.Combine(WorldPath, "basePrompt.txt"), response.Text);
        BasePrompt = response.Text;
        Debug.Log($"Generación completada. Contenido guardado en: {Path.Combine(WorldPath, "basePrompt.txt")}");
    }
    private async void SplitPromptIntoScenes()
    {
        var response = await GenerativeModel.GenerateContentAsync(SplitScenesPrompt + "\n" + BasePrompt);

        string rawText = response.Text.Trim();

        // Elimina los delimitadores de bloque de código si existen
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();

        Debug.Log("Respuesta limpia:\n" + rawText);

        try
        {
            var escenas = JsonConvert.DeserializeObject<List<Escena>>(rawText);
            Debug.Log($"Se han parseado {escenas.Count} escenas.");
            File.WriteAllText(Path.Combine(WorldPath, "scenes.json"), rawText);
            Scenes = escenas;
        }
        catch (JsonReaderException ex)
        {
            Debug.LogError("Error al deserializar el JSON: " + ex.Message);
        }
    }


}
