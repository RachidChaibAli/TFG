using GenerativeAI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public string ArquitectureDescriptionPrompt { get; private set; }
    public string ArchitecturePrompt { get; private set; }
    public string CommunicationPrompt { get; private set; }
    public string ScriptFinalPrompt { get; private set; }
    public string WorldPath => Path.Combine(Application.persistentDataPath, WorldName);
    public string SpritesPath => Path.Combine(WorldPath, "sprites");
    public string ScriptsPath => Path.Combine(WorldPath, "scripts");
    public List<Escena> Scenes { get; private set; } = new List<Escena>();

    private GeminiClient geminiClient;

    private StableDiffusionClient stableDiffusionClient;

    private GenerateScenes generateScenes;
    private PixianAIClient pixianAIClient;

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
        ArquitectureDescriptionPrompt = File.ReadAllText(Path.Combine(configPath, "arquitectureDescriptionPrompt.txt"));
        ArchitecturePrompt = File.ReadAllText(Path.Combine(configPath, "architecturePrompt.txt"));
        CommunicationPrompt = File.ReadAllText(Path.Combine(configPath, "communicationPrompt.txt"));
        ScriptFinalPrompt = File.ReadAllText(Path.Combine(configPath, "scriptFinalPrompt.txt"));

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
        if (!Directory.Exists(ScriptsPath))
        {
            Directory.CreateDirectory(ScriptsPath);
            Debug.Log($"Carpeta de scripts creada en: {ScriptsPath}");
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
        /*
        foreach ( var scene in Scenes)
        {
            tasks.Add(GenerateDialog(scene));
        }
        await Task.WhenAll(tasks);
        

        foreach (var scene in Scenes)
        {
            if (!Directory.Exists(Path.Combine(SpritesPath, scene.Id, "png")))
            {
                Directory.CreateDirectory(Path.Combine(SpritesPath, scene.Id, "png"));
                Debug.Log($"Carpeta de escena creada en: {Path.Combine(SpritesPath, scene.Id, "png")}");
            }
        }
        
        pixianAIClient = new PixianAIClient();

        tasks.Clear();
        foreach (var scene in Scenes)
        {
            tasks.Add(RemoveBackgroundAsync(scene));
        }
        await Task.WhenAll(tasks);
        tasks.Clear();
        

        
        WriteScencesToFile();
        

        foreach (var scene in Scenes)
        {
            tasks.Add(GenerateArquitectureDescription(scene));
        }
        await Task.WhenAll(tasks);

        tasks.Clear();
        foreach (var scene in Scenes)
        {
            tasks.Add(GenerateArquitecture(scene));
        }
        await Task.WhenAll(tasks);
        
        tasks.Clear();
        foreach (var scene in Scenes)
        {
            tasks.Add(GenerateCommunication(scene));
        }
        await Task.WhenAll(tasks);
        



        foreach (var scene in Scenes)
        {
            var sceneScriptPath = Path.Combine(ScriptsPath, scene.Id);
            if (!Directory.Exists(sceneScriptPath))
            {
                Directory.CreateDirectory(sceneScriptPath);
                Debug.Log($"Carpeta de escena creada en: {sceneScriptPath}");
            }
        }

        tasks.Clear();
        foreach (var scene in Scenes)
        {
            tasks.Add(GenerateScript(scene));
        }
        await Task.WhenAll(tasks);
        Debug.Log("Generación de scripts completada.");
        */
        GameOrquestrator.Instance.InstantiateSceneObjects(Scenes.First().Id);
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

    private async Task RemoveBackgroundAsync(Escena scene)
    {
        foreach (var sprite in scene.Elementos.MainSprites)
        {
            if (string.IsNullOrEmpty(sprite.Ruta) || !File.Exists(sprite.Ruta))
            {
                Debug.LogWarning($"Ruta del sprite no válida o no existe: {sprite.Ruta}");
                continue;
            }
            try
            {
                var pngPath = Path.Combine(SpritesPath, $"{scene.Id}/png/{sprite.Id}.png");
                if (File.Exists(pngPath))
                {
                    sprite.Ruta = pngPath; // Actualizar la ruta del sprite
                }
                else
                {
                    byte[] imageBytes = File.ReadAllBytes(sprite.Ruta);
                    byte[] resultBytes;
                    if (sprite.AssociatedObject != null)
                    {
                        resultBytes = await pixianAIClient.RemoveBackgroundAsync(imageBytes, true);
                    }
                    else
                    {
                        resultBytes = imageBytes; // Si no hay objeto asociado, no se elimina el fondo
                    }
                    
                    
                    if (resultBytes != null && resultBytes.Length > 0)
                    {
                        File.WriteAllBytes(sprite.Ruta, resultBytes);
                        Debug.Log($"Fondo eliminado para el sprite: {sprite.Name} ({sprite.Id})");
                        File.WriteAllBytes(pngPath, resultBytes);
                        sprite.Ruta = pngPath;
                    }
                    else
                    {
                        Debug.LogError($"No se pudo eliminar el fondo para el sprite: {sprite.Name} ({sprite.Id})");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error al eliminar el fondo del sprite {sprite.Name} ({sprite.Id}): {ex.Message}");
            }
        }
    }

    private async Task GenerateArquitectureDescription(Escena scene)
    {
        var scenePath = Path.Combine(WorldPath, scene.Id);
        var arquitectureDescriptionPath = Path.Combine(scenePath, $"arquitectureDescription_{scene.Id}.txt");
        if (File.Exists(arquitectureDescriptionPath))
        {
            Debug.Log($"Descripción de la arquitectura ya existe para la escena {scene.Id} en {arquitectureDescriptionPath}");
            return; // No generar de nuevo si ya existe
        }
        var response = await geminiClient.GenerateContentAsync(ArquitectureDescriptionPrompt + "\n" + JsonConvert.SerializeObject(scene, Formatting.Indented));
        Debug.Log($"Response: {response}");
        string rawText = response.Trim();
        
        try
        {
            if (!string.IsNullOrEmpty(rawText))
            {
                File.WriteAllText(Path.Combine(scenePath, $"arquitectureDescription_{scene.Id}.txt"), rawText);
                Debug.Log($"Descripción de la arquitectura de la escena {scene.Id} generada y guardada correctamente.");
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Error al deserializar la descripción de la arquitectura de la escena: {ex.Message}");
        }
    }

    private async Task GenerateArquitecture(Escena scene)
    {
        var scenePath = Path.Combine(WorldPath, scene.Id);
        var arquitectureDescriptionPath = Path.Combine(scenePath, $"arquitectureDescription_{scene.Id}.txt");
        string arquitectureDescription = string.Empty;
        if (File.Exists(arquitectureDescriptionPath))
        {
            arquitectureDescription = File.ReadAllText(arquitectureDescriptionPath);
        }
        else
        {
            Debug.LogWarning($"No se encontró la descripción de la arquitectura para la escena {scene.Id}. Generando sin descripción.");
        }
        var arquitecturePath = Path.Combine(scenePath, $"architecture_{scene.Id}.json");
        if (File.Exists(arquitecturePath))
        {
            Debug.Log($"Arquitectura ya existe para la escena {scene.Id} en {arquitecturePath}");
            return; // No generar de nuevo si ya existe
        }
        var response = await geminiClient.GenerateContentAsync(ArchitecturePrompt + "\n" + JsonConvert.SerializeObject(scene, Formatting.Indented) + "\nArquitecture description:\n" + arquitectureDescription);
        Debug.Log($"Response: {response}");
        string rawText = response.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();
        try
        {
            if (!string.IsNullOrEmpty(rawText))
            {
                File.WriteAllText(Path.Combine(scenePath, $"architecture_{scene.Id}.json"), rawText);
                Debug.Log($"Arquitectura de la escena {scene.Id} generada y guardada correctamente.");
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Error al deserializar la arquitectura de la escena: {ex.Message}");
        }
    }

    private async Task GenerateCommunication(Escena scene)
    {
        var scenePath = Path.Combine(WorldPath, scene.Id);
        var arquitecturePath = Path.Combine(scenePath, $"architecture_{scene.Id}.json");
        var arquitectureDescriptionPath = Path.Combine(scenePath, $"arquitectureDescription_{scene.Id}.txt");
        if (!File.Exists(arquitecturePath) || !File.Exists(arquitectureDescriptionPath))
        {
            Debug.LogWarning($"No se encontró la arquitectura o descripción de la arquitectura para la escena {scene.Id}. Generando sin estos datos.");
        }
        string arquitectureContent = File.Exists(arquitecturePath) ? File.ReadAllText(arquitecturePath) : string.Empty;
        string arquitectureDescriptionContent = File.Exists(arquitectureDescriptionPath) ? File.ReadAllText(arquitectureDescriptionPath) : string.Empty;
        var generatedCommunicationPath = Path.Combine(scenePath, $"communication_{scene.Id}.json");
        if (File.Exists(generatedCommunicationPath))
        {
            Debug.Log($"Comunicación ya existe para la escena {scene.Id} en {generatedCommunicationPath}");
            return; // No generar de nuevo si ya existe
        }
        var response = await geminiClient.GenerateContentAsync(CommunicationPrompt + "\n" + JsonConvert.SerializeObject(scene, Formatting.Indented) + "\nArquitecture:\n" + arquitectureContent + "\nArquitecture description:\n" + arquitectureDescriptionContent);

        Debug.Log($"Response: {response}");
        string rawText = response.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();
        try
        {
            if (!string.IsNullOrEmpty(rawText))
            {
                File.WriteAllText(Path.Combine(scenePath, $"communication_{scene.Id}.json"), rawText);
                Debug.Log($"Comunicación de la escena {scene.Id} generada y guardada correctamente.");
            }
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Error al deserializar la comunicación de la escena: {ex.Message}");
        }
    }

    private void WriteScencesToFile()
    {
        foreach (var scene in Scenes)
        {
            var scenePath = Path.Combine(WorldPath, scene.Id);
            if (!Directory.Exists(scenePath))
            {
                Directory.CreateDirectory(scenePath);
                Debug.Log($"Carpeta de escena creada en: {scenePath}");
            }
            File.WriteAllText(Path.Combine(scenePath, "sceneInfo.json"), JsonConvert.SerializeObject(scene, Formatting.Indented));
        }
    }

    private async Task GenerateScript(Escena scene)
    {
        var sceneScriptPath = Path.Combine(ScriptsPath, scene.Id);
        var scenePath = Path.Combine(WorldPath, scene.Id);
        string arquitecturePath = Path.Combine(scenePath, $"architecture_{scene.Id}.json");
        string communicationPath = Path.Combine(scenePath, $"communication_{scene.Id}.json");
        string arquitectureDescriptionPath = Path.Combine(scenePath, $"arquitectureDescription_{scene.Id}.txt");
        string arquitectureContent = File.Exists(arquitecturePath) ? File.ReadAllText(arquitecturePath) : string.Empty;
        string communicationContent = File.Exists(communicationPath) ? File.ReadAllText(communicationPath) : string.Empty;
        string arquitectureDescriptionContent = File.Exists(arquitectureDescriptionPath) ? File.ReadAllText(arquitectureDescriptionPath) : string.Empty;
        if (!Directory.Exists(sceneScriptPath))
        {
            Directory.CreateDirectory(sceneScriptPath);
            Debug.Log($"Carpeta de escena creada en: {sceneScriptPath}");
        }
        foreach (var script in scene.Elementos.EventScripts)
        {
            var scriptPath = Path.Combine(sceneScriptPath, $"{script.Id}.lua");
            if (!File.Exists(scriptPath))
            {
                Debug.Log($"Generando script: {script.Name} ({script.Id}) en {scriptPath}");
                
                string mensaje = "\nThe JSON containing the complete information of the scene:\n" + JsonConvert.SerializeObject(scene, Formatting.Indented) +
                    "\nThe description of the architecture:\n" + arquitectureDescriptionContent +
                    "\nThe architecture of the scene:\n" + JsonConvert.SerializeObject(arquitectureContent, Formatting.Indented) +
                    "\nThe communication of the scene:\n" + JsonConvert.SerializeObject(communicationContent, Formatting.Indented) +
                    "\nThe script you must generate:\n" + JsonConvert.SerializeObject(script, Formatting.Indented);


                var response = await geminiClient.GenerateContentAsync(ScriptFinalPrompt + mensaje);
                Debug.Log($"Response: {response}");
                string rawText = response.Trim();
                if (rawText.StartsWith("```lua"))
                    rawText = rawText[7..].TrimStart();
                if (rawText.EndsWith("```"))
                    rawText = rawText[..^3].TrimEnd();
                File.WriteAllText(scriptPath, rawText);
                Debug.Log($"Script generado y guardado: {script.Name} ({script.Id}) en {scriptPath}");
            }
            else
            {
                Debug.Log($"Script ya existe: {script.Name} ({script.Id}) en {scriptPath}");
            }
        }
    }

}
