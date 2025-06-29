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

    public string engineId = "stable-diffusion-v1-6";
    public string apiUrl = "https://api.stability.ai/v1/generation/";

    public StableDiffusionClient()
    {
        apiKey = Environment.GetEnvironmentVariable("STABLE_DIFFUSION_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("La variable de entorno STABLE_DIFFUSION_API_KEY no está definida.");
        }
    }

    public async Task<bool> GenerateImageAndSaveAsync(string prompt, string outputPath, string negativePrompt = null)
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
            height = 512,
            width = 512,
        };

        string jsonData = JsonUtility.ToJson(requestBody);
        using UnityWebRequest request = new(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "image/png");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

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
            Debug.LogError($"SD API Error: {request.error}");
            return false;
        }
    }
}

