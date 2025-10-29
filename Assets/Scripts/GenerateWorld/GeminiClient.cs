using GenerativeAI;
using System.Threading.Tasks;
using System;
using UnityEngine;

public class GeminiClient
{
    private static readonly object _lock = new object();
    private static GeminiClient _instance;
    public static GeminiClient Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new GeminiClient();
                }
            }
            return _instance;
        }
    }

    private GoogleAi googleAi;
    private GenerativeModel generativeModel;
    private readonly string modelName;

    private const int DefaultMaxRetries = 5;
    private const int DefaultInitialWaitMs = 60_000; // 60s

    private GeminiClient()
    {
        googleAi = new GoogleAi();

        // Use the environment variable if provided, otherwise default to the requested model
        modelName = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.0-flash-lite";

        generativeModel = googleAi.CreateGenerativeModel(modelName);
        Debug.Log($"GeminiClient initialized with model: {modelName}");
    }

    // Simple generate with retry on exceptions. If an exception occurs (for example a rate-limit),
    // wait and retry. Uses a fixed initial wait (60s) and exponential backoff.
    public async Task<string> GenerateContentAsync(string prompt, int maxRetries = DefaultMaxRetries)
    {
        int attempt = 0;
        int waitMs = DefaultInitialWaitMs;
        while (true)
        {
            try
            {
                var response = await generativeModel.GenerateContentAsync(prompt);
                return response.Text;
            }
            catch (Exception ex)
            {
                attempt++;
                Debug.LogWarning($"GeminiClient request failed (attempt {attempt}): {ex.Message}");

                if (attempt > maxRetries)
                {
                    Debug.LogError($"GeminiClient: maximum retries reached ({maxRetries}). Rethrowing exception.");
                    throw;
                }

                Debug.Log($"GeminiClient: waiting {waitMs}ms before retry {attempt}...");
                await Task.Delay(waitMs);
                // Exponential backoff (capped)
                waitMs = Math.Min(waitMs * 2, 5 * 60_000); // cap at 5 minutes
            }
        }
    }
}