using Google.Api;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Rezepte.Web.Entities;
using Rezepte.Web.Extensions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

    public async Task<AIRecipe[]> ExtractRecipeAsync(string ocrText)
    {
        var accessToken = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var prompt = $@"
Hier ist ein OCR-Text von einer Rezeptkarte. Bitte extrahiere:
- Titel des Rezepts
- Anzahl der Portionen (falls vorhanden)
- Zeiten (falls vorhanden)
- Zutatenliste (als Array)
- Zubereitungsschritte (als Fließtext)
- Ignoriere Logos, Kategorien, Kartennummern, Handschrift, Anmerkungen, Datum

Gib die Informationen wie folgt aus:
**Titel des Rezepts:** {{Hier der Titel}}

**Portionen:** {{Anzahl Personen oder leer lassen}}
**Vorbereitungszeit:** {{Zeit oder leer lassen}}
**Kochzeit:** {{Zeit oder leer lassen}}

**Zutatenliste:** 
{{Zutat 1}}
{{Zutat 2}}
{{Zutat 3}}
{{...}}

**Zubereitungsschritte:**
{{Fließtext}}

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

        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<AIRecipe>();

        return new AIRecipe[] { ParseRecipe(text) };
    }

    public async Task<AIRecipe[]> ExtractRecipeFromUrlAsync(string responseContent)
    {
        var accessToken = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var prompt = $@"
Hier ist Html-Code von einer Webseite, von der erwartet wir dass sie ein Kochrezept enthält. Bitte extrahiere:
- Titel des Rezepts
- Anzahl der Portionen (falls vorhanden)
- Zeiten (falls vorhanden)
- Zutatenliste
- Zubereitungsschritte 
- Rezeptbild (falls vorhanden)
- Ignoriere Navigation, Werbung, Kommentare, Bewertungen, Logos, Kategorien

Gib die Informationen wie folgt aus:
**Titel des Rezepts:** {{Hier der Titel}}

**Portionen:** {{Anzahl Personen oder leer lassen}}
**Vorbereitungszeit:** {{Zeit in Minuten oder leer lassen}}
**Kochzeit:** {{Zeit in Minuten oder leer lassen}}

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
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<AIRecipe>();

        return new AIRecipe[] { ParseRecipe(text) };
    }

    public class AIRecipe
    {
        public string Title { get; set; }
        public int Portions { get; set; }
        public List<string> Ingredients { get; set; }
        public string Instructions { get; set; }
        public byte[]? ImageData { get; set; }
        public string ImageUri { get; set; }
        public int PreparationTimeInMinutes { get; set; }
        public int CookingTimeInMinutes { get; set; }
    }
    private AIRecipe ParseRecipe(string recipeContent)
    {
        AIRecipe extractedRecipe = new AIRecipe();
        extractedRecipe.Title = ParseInformation(recipeContent, "Titel des Rezepts");
        extractedRecipe.Ingredients = ParseInformation(recipeContent, "Zutatenliste").Split("\r\n").Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.TrimStart(' ', '*')).ToList();
        extractedRecipe.Instructions = ParseInformation(recipeContent, "Zubereitungsschritte");
        extractedRecipe.Portions = ParsePortion(ParseInformation(recipeContent, "Portionen"));
        extractedRecipe.PreparationTimeInMinutes = ParseMinutes(ParseInformation(recipeContent, "Vorbereitungszeit"));
        extractedRecipe.CookingTimeInMinutes += ParseInformation(recipeContent, "Kochzeit").ToInt32(0);
        extractedRecipe.ImageUri = ParseInformation(recipeContent, "Bild-URL");
        if (!string.IsNullOrWhiteSpace(extractedRecipe.ImageUri))
        {
            try
            {
                using var httpClient = new HttpClient();
                var imageData = httpClient.GetByteArrayAsync(extractedRecipe.ImageUri).Result;
                extractedRecipe.ImageData = imageData;
            }
            catch (Exception ex)
            {
                extractedRecipe.ImageData = null;
            }
        }
        return extractedRecipe;
    }

    private int ParseMinutes(string text)
    {
        var input = text.ToLower().Trim();

        // Sonderfall: "1 1/2 Std." → 90 Minuten
        var bruchRegex = new Regex(@"(\d+)\s+(\d+)/(\d+)\s*std");
        var bruchMatch = bruchRegex.Match(input);
        if (bruchMatch.Success)
        {
            int ganz = int.Parse(bruchMatch.Groups[1].Value);
            int zähler = int.Parse(bruchMatch.Groups[2].Value);
            int nenner = int.Parse(bruchMatch.Groups[3].Value);
            return (int)Math.Round((ganz + (double)zähler / nenner) * 60);
        }

        // Bereichsangaben: "6-8 Min." oder "30-35 Min."
        var bereichRegex = new Regex(@"(\d+)\s*-\s*(\d+)\s*(min|std)");
        var bereichMatch = bereichRegex.Match(input);
        if (bereichMatch.Success)
        {
            int max = int.Parse(bereichMatch.Groups[2].Value);
            string einheit = bereichMatch.Groups[3].Value;
            return einheit == "std" ? max * 60 : max;
        }

        // Einzelangaben: "ca. 25 Min.", "30 Min.", "1 Std."
        var einzelRegex = new Regex(@"(\d+)(?:\s*std|\s*min)");
        var einzelMatch = einzelRegex.Match(input);
        if (einzelMatch.Success)
        {
            int wert = int.Parse(einzelMatch.Groups[1].Value);
            if (input.Contains("std")) return wert * 60;
            return wert;
        }

        return 0;
    }

    private int ParsePortion(string text)
    {
        return text.Split(' ').FirstOrDefault().ToInt32(0);
    }

    private string ParseInformation(string recipeContent, string sectionName)
    {
        return string.Join("\r\n", recipeContent
            .Replace("\r\n", "\n")
            .Replace("\r", "\rn")
            .Split("\n")
            .Select(line => line)
            .SkipWhile(line => !line.StartsWith($"**{sectionName}:**"))
            .Select(line => line.Replace($"**{sectionName}:**", "").Trim())
            .TakeWhile(line => !line.StartsWith("**")
            )).Trim();
    }

}
