using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class PixianAIClient
{
    private readonly string apiId;
    private readonly string apiSecret;
    private readonly HttpClient httpClient;

    public PixianAIClient()
    {
        // Lee API Id y Secret de las variables de entorno
        apiId = Environment.GetEnvironmentVariable("PIXIAN_API_ID", EnvironmentVariableTarget.Process) ??
                throw new Exception("PIXIAN_API_ID no está definida");
        apiSecret = Environment.GetEnvironmentVariable("PIXIAN_API_SECRET", EnvironmentVariableTarget.Process) ??
                   throw new Exception("PIXIAN_API_SECRET no está definida");
        httpClient = new HttpClient();
    }

    public async Task<byte[]> RemoveBackgroundAsync(byte[] imageBytes, bool testMode = false)
    {
        var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{apiId}:{apiSecret}"));
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.pixian.ai/api/v2/remove-background");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageBytes), "image", "image.png" }
        };
        // Añade el campo test si está en modo prueba
        if (testMode)
        {
            content.Add(new StringContent("true"), "test");
        }
        request.Content = content;

        var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        else
        {
            throw new Exception($"Error al enviar la imagen: {response.StatusCode}");
        }
    }

}
