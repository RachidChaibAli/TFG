using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System;

[System.Serializable]
public class TextPrompt
{
    public string text;
    public float weight = 1.0f;
}

[System.Serializable]
public class SDRequestBody
{
    public List<TextPrompt> text_prompts;
    public int height = 512;
    public int width = 512;
    public float cfg_scale = 7;
    public string clip_guidance_preset = "NONE";
    public int samples = 1;
    public int seed = 0;
    public int steps = 50;
    public string style_preset = "pixel-art";
}

public class StableDiffusionClient 
{
    private string apiKey;

    public string engineId = "stable-diffusion-xl-1024-v1-0";
    public string apiUrl = "https://api.stability.ai/v1/generation/";

    public StableDiffusionClient()
    {
        apiKey = Environment.GetEnvironmentVariable("STABLE_DIFFUSION_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("La variable de entorno STABLE_DIFFUSION_API_KEY no est� definida.");
        }
        // Allow overriding engine via environment variable for compatibility across accounts
        var envEngine = Environment.GetEnvironmentVariable("STABILITY_ENGINE_ID");
        if (!string.IsNullOrEmpty(envEngine))
        {
            engineId = envEngine;
            Debug.Log($"StableDiffusionClient: using engine from env: {engineId}");
        }
    }

    public async Task<bool> GenerateImageAndSaveAsync(string prompt, string outputPath, string negativePrompt = null, int width = 1024, int height = 1024)
    {
        string url = $"{apiUrl}{engineId}/text-to-image";
        var textPrompts = new List<TextPrompt> { new() { text = prompt, weight = 2.0f } };
        if (!string.IsNullOrEmpty(negativePrompt))
        {
            // Peso negativo recomendado por la API de Stability
            textPrompts.Add(new TextPrompt { text = negativePrompt, weight = -1.5f });
        }

        SDRequestBody requestBody = new()
        {
            text_prompts = textPrompts,
            height = height,
            width = width,
        };

        string jsonData = JsonUtility.ToJson(requestBody);
        using UnityWebRequest request = new(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "image/png");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        Debug.Log($"StableDiffusionClient: POST {url}");
        var asyncOp = request.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] data = request.downloadHandler.data;
            File.WriteAllBytes(outputPath, data);
            Debug.Log($"Imagen guardada directamente en: {outputPath}");
            string hexHeader = System.BitConverter.ToString(data, 0, Mathf.Min(8, data.Length));
            Debug.Log($"Cabecera PNG: {hexHeader}");
            return true;
        }
        else
        {
            // Log detailed info to diagnose 404/other errors
            long responseCode = request.responseCode;
            string responseText = string.Empty;
            try { responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty; } catch { }
            Debug.LogError($"SD API Error: {request.error} (HTTP {responseCode})\nURL: {url}\nResponse: {responseText}");
            return false;
        }
    }
}

