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
    public string sampler = "K_EULER";
    public int samples = 1;
    public int seed = 0;
    public int steps = 30;
    public string style_preset = "pixel-art";
}

public class StableDiffusionClient : MonoBehaviour
{
    private string apiKey;

    public string engineId = "stable-diffusion-v1-6";
    public string apiUrl = "https://api.stability.ai/v1/generation/";

    private void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("STABLE_DIFFUSION_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("La variable de entorno STABLE_DIFFUSION_API_KEY no está definida.");
        }
    }

    public IEnumerator GenerateImage(string prompt, System.Action<Texture2D> onComplete)
    {
        string url = $"{apiUrl}{engineId}/text-to-image";
        SDRequestBody requestBody = new()
        {
            text_prompts = new List<TextPrompt> { new() { text = prompt, weight = 1.0f } },
            height = 512,
            width = 512,
        };

        string jsonData = JsonUtility.ToJson(requestBody);
        UnityWebRequest request = new(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "image/png");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] data = request.downloadHandler.data;
            Debug.Log($"Bytes recibidos: {data.Length}");
            // Mostrar los primeros 8 bytes en hexadecimal
            string hexHeader = System.BitConverter.ToString(data, 0, Mathf.Min(8, data.Length));
            Debug.Log($"Cabecera PNG: {hexHeader}");

            Texture2D tex = new(2, 2);
            bool loaded = tex.LoadImage(data);
            Debug.Log($"¿LoadImage tuvo éxito?: {loaded}");
            onComplete?.Invoke(loaded ? tex : null);
        }
        else
        {
            Debug.LogError($"SD API Error: {request.error}");
            onComplete?.Invoke(null);
        }
    }

    public async Task GenerateImageAndSaveAsync(string prompt, string outputPath)
    {
        string url = $"{apiUrl}{engineId}/text-to-image";
        SDRequestBody requestBody = new()
        {
            text_prompts = new List<TextPrompt> { new() { text = prompt, weight = 1.0f } },
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
        }
        else
        {
            Debug.LogError($"SD API Error: {request.error}");
        }
    }
}

