using System.Text.Json;

namespace Rezepte.Web.Services.BackgroundJobs;

public sealed record ExportJobPayload(bool IncludeImages = false, bool IncludePdf = false)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ExportJobPayload FromJson(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new ExportJobPayload();
        }

        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        return new ExportJobPayload(
            IncludeImages: TryGetBoolean(root, "includeImages"),
            IncludePdf: TryGetBoolean(root, "includePdf"));
    }

    private static bool TryGetBoolean(JsonElement root, string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }
}
