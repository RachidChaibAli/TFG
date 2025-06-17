using GenerativeAI;
using System.IO;
using UnityEngine;

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
    public string Prompt { get; private set; }
    public string InitialPromt { get; private set; }
    public GoogleAi GoogleAi { get; private set; }
    public GenerativeModel GenerativeModel { get; private set; }

    private GenerationOrquestrator() { }

    public void Initialize(string worldName, string prompt, string initialPromt)
    {
        WorldName = worldName;
        Prompt = prompt;

        InitialPromt = initialPromt;

        GoogleAi = new GoogleAi();
        GenerativeModel = GoogleAi.CreateGenerativeModel(GoogleAIModels.Gemini15Flash);
    }

    public async void StartGeneration()
    {
        var response = await GenerativeModel.GenerateContentAsync(InitialPromt + "\n" + Prompt);
        Debug.Log("Respuesta de Gemini: " + response);
    }
}
