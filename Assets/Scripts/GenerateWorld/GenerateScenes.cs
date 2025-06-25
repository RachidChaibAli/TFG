using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using static SplitScenesClasses;


public class GenerateScenes
{
    private List<Escena> Scenes { get; set; } = new List<Escena>();

    private GeminiClient geminiClient;

    private string WorldPath;

    private string BasePrompt;

    private string GenerateScenesPrompt;

    private string GenerateNPCsPrompt;

    private string GenerateInteractiveObjectsPrompt;

    private string GenerateSpritesPrompt;

    private string GenerateEventScriptsPrompt;

    public GenerateScenes(string basePrompt, GeminiClient geminiClient, string worldPath)
    {
        WorldPath = worldPath;

        BasePrompt = basePrompt;

        this.geminiClient = geminiClient;

        var promptsPath = Path.Combine(Application.dataPath, "Scripts/GenerateWorld/ConfigPrompts");

        GenerateScenesPrompt = File.ReadAllText(Path.Combine(promptsPath, "generateScenesPrompt.txt"));

        GenerateNPCsPrompt = File.ReadAllText(Path.Combine(promptsPath, "generateNpcPrompt.txt"));

        GenerateInteractiveObjectsPrompt = File.ReadAllText(Path.Combine(promptsPath, "generateInteractiveObjectsPrompt.txt"));

        GenerateSpritesPrompt = File.ReadAllText(Path.Combine(promptsPath, "generateSpritesPrompt.txt"));

        //GenerateEventScriptsPrompt = File.ReadAllText(Path.Combine(promptsPath, "generateEventScriptsPrompt.txt"));
        WorldPath = worldPath;
    }

    public async Task GenerateAllScenes()
    {

        await GenerateScenesBase();

        foreach (var scene in Scenes)
        {
            await GenerateNPCs(scene);
            EnsureIds(scene.Elementos?.NPCs, $"escena_{scene.Id + 1}_npc");
            await GenerateInteractiveObjects(scene);
            EnsureIds(scene.Elementos?.ObjetosInteractivos, $"escena_{scene.Id + 1}_objetoInteractivo");
            await GenerateSprites(scene);
            EnsureIds(scene.Elementos?.SpritesPrincipales, $"escena_{scene.Id + 1}_sprite");
            await GenerateEventScripts(scene);
            EnsureIds(scene.Elementos?.ScriptsEventos, $"escena_{scene.Id + 1}_scriptEvento");
        }
    }
    private async Task GenerateScenesBase()
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
            EnsureIds(Scenes, "escena");
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
    private async Task GenerateNPCs(Escena scene)
    {
        var response = await geminiClient.GenerateContentAsync(GenerateNPCsPrompt + JsonConvert.SerializeObject(scene, Formatting.Indented));

        Debug.Log($"Respuesta de Gemini para NPCs: {response}");

        string rawText = response.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();

        try
        {
            var npcs = JsonConvert.DeserializeObject<List<NPC>>(rawText);
            if (npcs != null && npcs.Count > 0)
            {
                scene.Elementos ??= new Elementos();

                scene.Elementos.NPCs = npcs;
                Debug.Log($"Se han generado {npcs.Count} NPCs para la escena {scene.Id}.");
            }
            else
            {
                Debug.LogWarning($"No se generaron NPCs para la escena {scene.Id}.");
            }
        }
        catch (JsonReaderException ex)
        {
            Debug.LogError("Error al deserializar el JSON de NPCs: " + ex.Message);
        }

        var scenePath = Path.Combine(WorldPath, scene.Id);
        if (!Directory.Exists(scenePath))
        {
            Directory.CreateDirectory(scenePath);
            Debug.Log($"Carpeta de escena creada en: {scenePath}");
        }
        // Serializar el objeto 'escena' a JSON antes de escribirlo en el archivo
        string escenaJson = JsonConvert.SerializeObject(scene, Formatting.Indented);
        File.WriteAllText(Path.Combine(scenePath, "sceneInfo.json"), escenaJson);
    }

    private async Task GenerateInteractiveObjects(Escena scene)
    {
        var response = await geminiClient.GenerateContentAsync(GenerateInteractiveObjectsPrompt + JsonConvert.SerializeObject(scene, Formatting.Indented));
        Debug.Log($"Respuesta de Gemini para objetos interactivos: {response}");
        string rawText = response.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();
        try
        {
            var objetosInteractivos = JsonConvert.DeserializeObject<List<objetosInteractivos>>(rawText);
            if (objetosInteractivos != null && objetosInteractivos.Count > 0)
            {
                scene.Elementos ??= new Elementos();
                scene.Elementos.ObjetosInteractivos = objetosInteractivos;
                Debug.Log($"Se han generado {objetosInteractivos.Count} objetos interactivos para la escena {scene.Id}.");
            }
            else
            {
                Debug.LogWarning($"No se generaron objetos interactivos para la escena {scene.Id}.");
            }
        }
        catch (JsonReaderException ex)
        {
            Debug.LogError("Error al deserializar el JSON de objetos interactivos: " + ex.Message);
        }
        var scenePath = Path.Combine(WorldPath, scene.Id);
        if (!Directory.Exists(scenePath))
        {
            Directory.CreateDirectory(scenePath);
            Debug.Log($"Carpeta de escena creada en: {scenePath}");
        }
        // Serializar el objeto 'escena' a JSON antes de escribirlo en el archivo
        string escenaJson = JsonConvert.SerializeObject(scene, Formatting.Indented);
        File.WriteAllText(Path.Combine(scenePath, "sceneInfo.json"), escenaJson);
    }

    private async Task GenerateSprites(Escena scene)
    {
        var response = await geminiClient.GenerateContentAsync(GenerateSpritesPrompt + JsonConvert.SerializeObject(scene, Formatting.Indented));
        Debug.Log($"Respuesta de Gemini para sprites: {response}");
        string rawText = response.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();
        try
        {
            var sprites = JsonConvert.DeserializeObject<List<sprites>>(rawText);
            if (sprites != null && sprites.Count > 0)
            {
                scene.Elementos ??= new Elementos();
                scene.Elementos.SpritesPrincipales = sprites;
                Debug.Log($"Se han generado {sprites.Count} sprites para la escena {scene.Id}.");
            }
            else
            {
                Debug.LogWarning($"No se generaron sprites para la escena {scene.Id}.");
            }
        }
        catch (JsonReaderException ex)
        {
            Debug.LogError("Error al deserializar el JSON de sprites: " + ex.Message);
        }
        var scenePath = Path.Combine(WorldPath, scene.Id);
        if (!Directory.Exists(scenePath))
        {
            Directory.CreateDirectory(scenePath);
            Debug.Log($"Carpeta de escena creada en: {scenePath}");
        }
        // Serializar el objeto 'escena' a JSON antes de escribirlo en el archivo
        string escenaJson = JsonConvert.SerializeObject(scene, Formatting.Indented);
        File.WriteAllText(Path.Combine(scenePath, "sceneInfo.json"), escenaJson);
    }

    private async Task GenerateEventScripts(Escena scene)
    {
        var response = await geminiClient.GenerateContentAsync(GenerateEventScriptsPrompt + JsonConvert.SerializeObject(scene, Formatting.Indented));
        Debug.Log($"Respuesta de Gemini para scripts de eventos: {response}");
        string rawText = response.Trim();
        if (rawText.StartsWith("```json"))
            rawText = rawText[7..].TrimStart();
        if (rawText.EndsWith("```"))
            rawText = rawText[..^3].TrimEnd();
        try
        {
            var scriptsEventos = JsonConvert.DeserializeObject<List<ScriptEvento>>(rawText);
            if (scriptsEventos != null && scriptsEventos.Count > 0)
            {
                scene.Elementos ??= new Elementos();
                scene.Elementos.ScriptsEventos = scriptsEventos;
                Debug.Log($"Se han generado {scriptsEventos.Count} scripts de eventos para la escena {scene.Id}.");
            }
            else
            {
                Debug.LogWarning($"No se generaron scripts de eventos para la escena {scene.Id}.");
            }
        }
        catch (JsonReaderException ex)
        {
            Debug.LogError("Error al deserializar el JSON de scripts de eventos: " + ex.Message);
        }
        var scenePath = Path.Combine(WorldPath, scene.Id);
        if (!Directory.Exists(scenePath))
        {
            Directory.CreateDirectory(scenePath);
            Debug.Log($"Carpeta de escena creada en: {scenePath}");
        }
        // Serializar el objeto 'escena' a JSON antes de escribirlo en el archivo
        string escenaJson = JsonConvert.SerializeObject(scene, Formatting.Indented);
        File.WriteAllText(Path.Combine(scenePath, "sceneInfo.json"), escenaJson);
    }
    private static void EnsureIds<T>(List<T> lista, string prefijo)
    {
        if (lista == null) return;
        var type = typeof(T);
        var idProp = type.GetProperty("Id");
        if (idProp == null) return;

        for (int i = 0; i < lista.Count; i++)
        {
            var item = lista[i];
            var idValue = idProp.GetValue(item) as string;
            if (string.IsNullOrEmpty(idValue))
            {
                idProp.SetValue(item, $"{prefijo}_{i + 1}");
            }
        }
    }
}
