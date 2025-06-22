using GenerativeAI;
using System.Threading.Tasks;

public class GeminiClient
{
    private GoogleAi googleAi;
    private GenerativeModel generativeModel;

    public GeminiClient()
    {
        googleAi = new GoogleAi();
        generativeModel = googleAi.CreateGenerativeModel(GoogleAIModels.Gemini15Flash);
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        var response = await generativeModel.GenerateContentAsync(prompt);
        return response.Text;
    }
}