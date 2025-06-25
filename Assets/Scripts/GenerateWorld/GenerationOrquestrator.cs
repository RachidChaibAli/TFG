using GenerativeAI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
    public string UserPrompt { get; private set; }
    public string BasePrompt { get; private set; }
    public string InitialPrompt { get; private set; }
    public string GenerateScenesPrompt { get; private set; }    
    public string WorldPath => Path.Combine(Application.persistentDataPath, WorldName);
    public string SpritesPath => Path.Combine(WorldPath, "sprites");
    public List<Escena> Scenes { get; private set; } = new List<Escena>();

    private GeminiClient geminiClient;

    private StableDiffusionClient stableDiffusionClient;

    private GenerateScenes generateScenes;

    private GenerationOrquestrator() { }

    public void Initialize(string worldName, string prompt)
    {
        WorldName = worldName;
        UserPrompt = prompt;

        string configPath = Path.Combine(Application.dataPath, "Scripts/GenerateWorld/ConfigPrompts");

        InitialPrompt = File.ReadAllText(Path.Combine(configPath, "initialPrompt.txt"));
        GenerateScenesPrompt = File.ReadAllText(Path.Combine(configPath, "generateScenesPrompt.txt"));

        geminiClient = new GeminiClient();

        if (!Directory.Exists(WorldPath))
        {
            Directory.CreateDirectory(WorldPath);
            Debug.Log($"Carpeta del mundo creada en: {WorldPath}");
        }

        if (!Directory.Exists(SpritesPath))
        {
            Directory.CreateDirectory(SpritesPath);
            Debug.Log($"Carpeta de sprites creada en: {SpritesPath}");
        }
    }

    public async void StartGeneration()
    {
        if (string.IsNullOrEmpty(WorldName) || string.IsNullOrEmpty(UserPrompt))
        {
            Debug.LogError("El nombre del mundo y el prompt no pueden estar vacíos.");
            return;
        }
        await ParsePrompt();

        generateScenes = new GenerateScenes(BasePrompt, geminiClient, WorldPath);

        await generateScenes.GenerateAllScenes();

    }

    private async Task ParsePrompt()
    {
        var responseText = await geminiClient.GenerateContentAsync(InitialPrompt + "\n" + UserPrompt);
        File.WriteAllText(Path.Combine(WorldPath, "basePrompt.txt"), responseText);
        BasePrompt = responseText;
        Debug.Log("Prompt base generado:\n" + BasePrompt);
        Debug.Log($"Generación completada. Contenido guardado en: {Path.Combine(WorldPath, "basePrompt.txt")}");
    }

    public async void PruebaImagenGenerada()
    {
        string imagePath = Path.Combine(WorldPath, "sprites/generatedImage.png");

        // Crear un GameObject temporal para usar StableDiffusionClient
        var tempObj = new GameObject("StableDiffusionClientTemp");
        var tempClient = tempObj.AddComponent<StableDiffusionClient>();

        await tempClient.GenerateImageAndSaveAsync("Dame un png sin fondo de un dragon occidental en estilo pixel art", imagePath);
        Debug.Log($"Imagen generada y guardada en: {imagePath}");

        Object.DestroyImmediate(tempObj);
    }
}
