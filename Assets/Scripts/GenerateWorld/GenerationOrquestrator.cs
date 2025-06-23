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
    public string CompleteScenesPrompt { get; private set; }
    
    public string WorldPath => Path.Combine(Application.persistentDataPath, WorldName);
    public string SpritesPath => Path.Combine(WorldPath, "sprites");
    public List<Escena> Scenes { get; private set; } = new List<Escena>();

    private GeminiClient geminiClient;

    private StableDiffusionClient stableDiffusionClient;

    private GenerationOrquestrator() { }

    public void Initialize(string worldName, string prompt)
    {
        WorldName = worldName;
        UserPrompt = prompt;

        string configPath = Path.Combine(Application.dataPath, "Scripts/GenerateWorld/ConfigPrompts");

        InitialPrompt = File.ReadAllText(Path.Combine(configPath, "initialPrompt.txt"));
        CompleteScenesPrompt = File.ReadAllText(Path.Combine(configPath, "completeScenesPrompt.txt"));
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
        await GenerateScenes();
        await CompleteScenes();
    }

    private async Task ParsePrompt()
    {
        var responseText = await geminiClient.GenerateContentAsync(InitialPrompt + "\n" + UserPrompt);
        File.WriteAllText(Path.Combine(WorldPath, "basePrompt.txt"), responseText);
        BasePrompt = responseText;
        Debug.Log("Prompt base generado:\n" + BasePrompt);
        Debug.Log($"Generación completada. Contenido guardado en: {Path.Combine(WorldPath, "basePrompt.txt")}");
    }

    private async Task GenerateScenes()
    {
        var responseText = await geminiClient.GenerateContentAsync(GenerateScenesPrompt + "\n" + BasePrompt);

        string rawText = responseText.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();


        try
        {
            var escenas = JsonConvert.DeserializeObject<List<Escena>>(rawText);
            Debug.Log($"Se han parseado {escenas.Count} escenas.");
            File.WriteAllText(Path.Combine(WorldPath, "generateScenes.json"), rawText);
            Scenes = escenas;
        }
        catch (JsonReaderException ex)
        {
            Debug.LogError("Error al deserializar el JSON: " + ex.Message);
        }

        
        foreach (var escena in Scenes)
        {
            var scenePath = Path.Combine(WorldPath, escena.Id);
            if (!Directory.Exists(scenePath))
            {
                Directory.CreateDirectory(scenePath);
                Debug.Log($"Carpeta de escena creada en: {scenePath}");

                // Serializar el objeto 'escena' a JSON antes de escribirlo en el archivo
                string escenaJson = JsonConvert.SerializeObject(escena, Formatting.Indented);
                File.WriteAllText(Path.Combine(scenePath, "sceneInfo.json"), escenaJson);
            }
        }
    }

    private async Task CompleteScenes()
    {
        Debug.Log("Iniciando la generación de escenas completas...");
        Debug.Log($"Total de escenas a completar: {Scenes.Count}");
        for (int i=0; i < Scenes.Count; i++)
        {
            var escena = Scenes[i];

            var responseText = await geminiClient.GenerateContentAsync(CompleteScenesPrompt + "\n" + JsonConvert.SerializeObject(escena, Formatting.Indented));

            string rawText = responseText.Trim();
            if (rawText.StartsWith("```json"))
                rawText = rawText[7..].TrimStart();
            if (rawText.EndsWith("```"))
                rawText = rawText[..^3].TrimEnd();

            try
            {
                var escenasCompletas = JsonConvert.DeserializeObject<List<Escena>>(rawText);
                var escenaCompleta = escenasCompletas[0];
                Debug.Log($"Escena {escenaCompleta.Id} completada.");

                Scenes[i] = escenaCompleta; // Actualizar la escena en la lista

                // Guardar la escena completa en su carpeta
                string scenePath = Path.Combine(WorldPath, escenaCompleta.Id, "completeScene.json");
                File.WriteAllText(scenePath, JsonConvert.SerializeObject(escenaCompleta, Formatting.Indented));
            }
            catch (JsonReaderException ex)
            {
                Debug.LogError($"Error al deserializar la escena {escena.Id}: " + ex.Message);
            }

        }
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
