using System.Text.Json;

namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// exports the job payload.
/// </summary>
/// <param name="IncludeImages">The include images parameter.</param>
/// <param name="IncludePdf">The include pdf parameter.</param>
/// <returns>The result.</returns>
public sealed record ExportJobPayload(bool IncludeImages = false, bool IncludePdf = false)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// tos the json.
    /// </summary>
    /// <param name="JsonOptions">The json options parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// froms the json.
    /// </summary>
    /// <param name="payloadJson">The payload json parameter.</param>
    /// <returns>The result.</returns>
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
