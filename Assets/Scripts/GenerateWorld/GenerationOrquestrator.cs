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
    public string StableDiffusionPrompt { get; private set; }
    public string NegativeStableDiffusionPrompt { get; private set; }
    public string ScenePositionPrompt { get; private set; }
    public string GenerateDialogPrompt { get; private set; }
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
        StableDiffusionPrompt = File.ReadAllText(Path.Combine(configPath, "Sprite/stableDiffusionPrompt.txt"));
        NegativeStableDiffusionPrompt = File.ReadAllText(Path.Combine(configPath, "Sprite/negativeStableDiffusionPrompt.txt"));
        ScenePositionPrompt = File.ReadAllText(Path.Combine(configPath, "scenePositionsPrompt.txt"));
        GenerateDialogPrompt = File.ReadAllText(Path.Combine(configPath, "generateDialogPrompt.txt"));

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
            Debug.LogError("El nombre del mundo y el prompt no pueden estar vacíos.");
            return;
        }

        //await ParsePrompt();
        BasePrompt = File.ReadAllText(Path.Combine(WorldPath, "basePrompt.txt"));

        //generateScenes = new GenerateScenes(BasePrompt, geminiClient, WorldPath);

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
        
        
        /*
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

        var tasks = new List<Task>();

        foreach (var scene in Scenes)
        {
            tasks.Add( ScenePosition(scene));

        }
        await Task.WhenAll(tasks);
        */

        var tasks = new List<Task>();

        foreach ( var scene in Scenes)
        {
            tasks.Add(GenerateDialog(scene));
        }
        await Task.WhenAll(tasks);

    }

    private async Task ParsePrompt()
    {
        var responseText = await geminiClient.GenerateContentAsync(InitialPrompt + "\n" + UserPrompt);
        File.WriteAllText(Path.Combine(WorldPath, "basePrompt.txt"), responseText);
        BasePrompt = responseText;
        Debug.Log("Prompt base generado:\n" + BasePrompt);
        Debug.Log($"Generación completada. Contenido guardado en: {Path.Combine(WorldPath, "basePrompt.txt")}");
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
                Debug.Log($"Posición de la escena {scene.Id} deserializada correctamente.");
                Debug.Log($"POSICIONES: {JsonConvert.SerializeObject(scenePosition.positions, Formatting.Indented)}");
                File.WriteAllText(Path.Combine(scenePath, "scenePosition.json"), JsonConvert.SerializeObject(scenePosition, Formatting.Indented));
            }
            else
            {
                Debug.LogWarning("No se pudo deserializar la posición de la escena correctamente.");
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Error al deserializar la posición de la escena: {ex.Message}");
        }
    }

    private async Task GenerateDialog(Escena scene)
    {
        var allScenes = File.ReadAllText(Path.Combine(WorldPath, "generateScenes.json"));
        foreach ( var npc in scene.Elementos.NPCs)
        {
            if (npc.Id == "player")
                continue; // No generar diálogo para el jugador
            var npcPath = Path.Combine(WorldPath, scene.Id, $"{npc.Id}_dialog.json");
            if (!File.Exists(npcPath))
            {
                Debug.Log($"Generando diálogo para NPC: {npc.Name} ({npc.Id}) en {npcPath}");
                string mensaje = "\nThe JSON containing the complete information of the scene the ncp is in:\n" + JsonConvert.SerializeObject(scene) + "\nThe npc you must generate the dialog for:\n" + JsonConvert.SerializeObject(npc);
                var response = await geminiClient.GenerateContentAsync( GenerateDialogPrompt + mensaje);
                string rawText = response.Trim();
                if (rawText.StartsWith("```json"))
                    rawText = rawText[7..].TrimStart();
                if (rawText.EndsWith("```"))
                    rawText = rawText[..^3].TrimEnd();
                File.WriteAllText(npcPath, rawText);
                Debug.Log($"Diálogo generado y guardado para NPC: {npc.Name} ({npc.Id}) en {npcPath}");
            }
            else
            {
                Debug.Log($"Diálogo ya existe para NPC: {npc.Name} ({npc.Id}) en {npcPath}");
            }
        }
    }

}
