using Google.Api;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Rezepte.Web.Services.Import;

public class GeminiClient
{
    private readonly HttpClient _httpClient;

    public GeminiClient(string serviceAccountJsonPath)
    {
        _httpClient = new HttpClient();
        _credential = GoogleCredential
            .FromFile(serviceAccountJsonPath)
            .CreateScoped("https://www.googleapis.com/auth/generative-language");
    }

    private readonly GoogleCredential _credential;

    public async Task<string> ExtractRecipeAsync(string ocrText)
    {
        var accessToken = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var prompt = $@"
Hier ist ein OCR-Text von einer Rezeptkarte. Bitte extrahiere:
- Titel des Rezepts
- Zutatenliste (als Array)
- Zubereitungsschritte (als Fließtext)
- Ignoriere Logos, Kategorien, Kartennummern

OCR-Text:
""{ocrText}""
";

        var requestBody = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var model = "gemini-2.5-flash-lite";
        //model = "gemini-pro";

        var response = await _httpClient.PostAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
            content
        );
        var responseJson = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        

        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }

    public async Task<string> ExtractRecipeFromUrlAsync(string responseContent)
    {
        var accessToken = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var prompt = $@"
Hier ist Html-Code von einer Webseite, von der erwartet wir dass sie ein Kochrezept enthält. Bitte extrahiere:
- Titel des Rezepts
- Zutatenliste
- Zubereitungsschritte 
- Rezeptbild (falls vorhanden)
- Ignoriere Navigation, Werbung, Kommentare, Bewertungen, Logos, Kategorien

Gib die Informationen wie folgt aus:
**Titel des Rezepts:** {{Hier der Titel}}

**Bild-URL:** {{Hier die Bild-URL oder leer lassen}}

**Zutatenliste:** 
{{Zutat 1}}
{{Zutat 2}}
{{Zutat 3}}
{{...}}

**Zubereitungsschritte:**
{{Fließtext}}

Html-Code:
""{responseContent}""
";

        var requestBody = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var model = "gemini-2.5-flash-lite";
        //model = "gemini-pro";

        var response = await _httpClient.PostAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
            content
        );
        var responseJson = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();


        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }
}
