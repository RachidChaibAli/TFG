using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;

public class StableImageCoreClient
{
    private readonly string apiKey;
    private readonly string apiUrl = "https://api.stability.ai/v2beta/stable-image/generate/core";

    public StableImageCoreClient()
    {
        apiKey = Environment.GetEnvironmentVariable("STABLE_DIFFUSION_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Environment variable STABLE_DIFFUSION_API_KEY is not defined.");
    }

    private static StringContent NamedStringContent(string value, string name)
    {
        var content = new StringContent(value);
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = $"\"{name}\"" };
        return content;
    }

    /// <summary>
    /// Generate an image using Stable Image Core API and save it as a PNG file.
    /// </summary>
    /// <param name="prompt">Prompt for the image.</param>
    /// <param name="outputPath">Where to save the image file.</param>
    /// <param name="negativePrompt">Negative prompt (optional).</param>
    /// <param name="aspectRatio">Aspect ratio (e.g., "1:1", "16:9", optional).</param>
    /// <param name="seed">Seed value (optional, 0 = random).</param>
    /// <param name="stylePreset">Style preset (e.g., "pixel-art", optional).</param>
    /// <param name="outputFormat">"png", "jpeg", or "webp" (optional, default "png").</param>
    public async Task<bool> GenerateImageAndSaveAsync(
        string prompt,
        string outputPath,
        string negativePrompt = null,
        string aspectRatio = "1:1",
        uint seed = 0,
        string stylePreset = "pixel-art",
        string outputFormat = "png")
    {
        using var client = new HttpClient();
        using var form = new MultipartFormDataContent();

        form.Add(NamedStringContent(prompt, "prompt"));
        if (!string.IsNullOrEmpty(negativePrompt))
            form.Add(NamedStringContent(negativePrompt, "negative_prompt"));
        if (!string.IsNullOrEmpty(aspectRatio))
            form.Add(NamedStringContent(aspectRatio, "aspect_ratio"));
        if (seed > 0)
            form.Add(NamedStringContent(seed.ToString(), "seed"));
        if (!string.IsNullOrEmpty(stylePreset))
            form.Add(NamedStringContent(stylePreset, "style_preset"));
        if (!string.IsNullOrEmpty(outputFormat))
            form.Add(NamedStringContent(outputFormat, "output_format"));

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

        try
        {
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"Stable Image Core API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return false;
            }

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(outputPath, data);
            Debug.Log($"Image saved at: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in StableImageCoreClient: {ex.Message}");
            return false;
        }
    }
}
