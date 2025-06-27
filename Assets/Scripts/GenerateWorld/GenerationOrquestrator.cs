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
    public string StableDiffusionPrompt { get; private set; }
    public string NegativeStableDiffusionPrompt { get; private set; }
    public string ScenePositionPrompt { get; private set; }
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
        StableDiffusionPrompt = File.ReadAllText(Path.Combine(configPath, "stableDiffusionPrompt.txt"));
        NegativeStableDiffusionPrompt = File.ReadAllText(Path.Combine(configPath, "negativeStableDiffusionPrompt.txt"));
        ScenePositionPrompt = File.ReadAllText(Path.Combine(configPath, "scenePositionsPrompt.txt"));

        geminiClient = new GeminiClient();

        stableDiffusionClient = new StableDiffusionClient();

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
            Debug.LogError("El nombre del mundo y el prompt no pueden estar vac�os.");
            return;
        }

        //await ParsePrompt();
        BasePrompt = File.ReadAllText(Path.Combine(WorldPath, "basePrompt.txt"));

        generateScenes = new GenerateScenes(BasePrompt, geminiClient, WorldPath);

        //Scenes = await generateScenes.GenerateAllScenes();
        
        Scenes.Clear();
        int i = 1;
        while (true)
        {
            string sceneFolder = Path.Combine(WorldPath, $"scene_{i}");
            string sceneInfoPath = Path.Combine(sceneFolder, "sceneInfo.json");
            if (!File.Exists(sceneInfoPath))
                break;

            try
            {
                string json = File.ReadAllText(sceneInfoPath);
                var escena = JsonConvert.DeserializeObject<Escena>(json);
                if (escena != null)
                {
                    Scenes.Add(escena);
                    Debug.Log($"Escena cargada: {escena.Id} ({sceneInfoPath})");
                }
                else
                {
                    Debug.LogWarning($"No se pudo deserializar la escena en: {sceneInfoPath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error al cargar la escena {sceneInfoPath}: {ex.Message}");
            }
            i++;
        }
        
        var tasks = new List<Task>();

        await GenerateSpritesAsync();
        foreach ( var scene in Scenes)
        {
            var scenePath = Path.Combine(WorldPath, scene.Id);
            if (!Directory.Exists(scenePath))
            {
                Directory.CreateDirectory(scenePath);
                Debug.Log($"Carpeta de escena creada en: {scenePath}");
            }
            File.WriteAllText(Path.Combine(scenePath, "sceneInfo.json"), JsonConvert.SerializeObject(scene, Formatting.Indented));
        }
        return;
        foreach (var scene in Scenes)
        {
            tasks.Add( ScenePosition(scene));

        }
        await Task.WhenAll(tasks);
    }

    private async Task ParsePrompt()
    {
        var responseText = await geminiClient.GenerateContentAsync(InitialPrompt + "\n" + UserPrompt);
        File.WriteAllText(Path.Combine(WorldPath, "basePrompt.txt"), responseText);
        BasePrompt = responseText;
        Debug.Log("Prompt base generado:\n" + BasePrompt);
        Debug.Log($"Generaci�n completada. Contenido guardado en: {Path.Combine(WorldPath, "basePrompt.txt")}");
    }

    public async Task GenerateSpritesAsync()
    {
        
        foreach ( var scene in Scenes)
        {
            var scenePath = Path.Combine(SpritesPath, scene.Id);
            if (!Directory.Exists(scenePath))
            {
                Directory.CreateDirectory(scenePath);
                Debug.Log($"Carpeta de escena creada en: {scenePath}");
            }

            foreach (var sprite in scene.Elementos.MainSprites)
            {
                var spritePath = Path.Combine(scenePath, $"{sprite.Id}.png");
                if (!File.Exists(spritePath))
                {
                    Debug.Log($"Generando sprite: {sprite.Name} en {spritePath}");
                    var image = await stableDiffusionClient.GenerateImageAndSaveAsync(sprite.Description, spritePath, NegativeStableDiffusionPrompt);
                    if (image)
                    {
                        Debug.Log($"Sprite generado y guardado: {sprite.Name}");
                        sprite.Ruta = spritePath;
                    }
                    else
                    {
                        Debug.LogError($"Error al generar el sprite: {sprite.Name}");
                    }
                }
                else
                {
                    Debug.Log($"Sprite ya existe: {sprite.Name} en {spritePath}");
                    sprite.Ruta = spritePath;
                }
                
            }

            
        }
    }

    private async Task ScenePosition(Escena scene)
    {
        var scenePath = Path.Combine(WorldPath, scene.Id);

        var response = await geminiClient.GenerateContentAsync(ScenePositionPrompt + "\n" + JsonConvert.SerializeObject(scene, Formatting.Indented));
        string rawText = response.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();

        try
            {
            ScenePosition scenePosition = JsonConvert.DeserializeObject<ScenePosition>(rawText);
            if (scenePosition != null && scenePosition.positions != null)
            {
                Debug.Log($"Posici�n de la escena {scene.Id} deserializada correctamente.");
                Debug.Log($"POSICIONES: {JsonConvert.SerializeObject(scenePosition.positions, Formatting.Indented)}");
                File.WriteAllText(Path.Combine(scenePath, "scenePosition.json"), JsonConvert.SerializeObject(scenePosition, Formatting.Indented));
            }
            else
            {
                Debug.LogWarning("No se pudo deserializar la posici�n de la escena correctamente.");
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Error al deserializar la posici�n de la escena: {ex.Message}");
        }
    }

}
