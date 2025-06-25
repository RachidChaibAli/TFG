using GenerativeAI;
using System.Threading.Tasks;

public class GeminiClient
{
    private GoogleAi googleAi;
    private GenerativeModel generativeModel;

    public GeminiClient()
    {
        googleAi = new GoogleAi();
        generativeModel = googleAi.CreateGenerativeModel("gemini-2.0-flash-lite");
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        var response = await generativeModel.GenerateContentAsync(prompt);
        return response.Text;
    }
}