using System.Text.Json;

namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// Payload for the demo data seeding background job.
/// </summary>
/// <param name="UserId">The id of the user for whom the demo data is seeded.</param>
/// <returns>The demo data seed payload.</returns>
public sealed record DemoDataSeedPayload(string UserId)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes this payload to a JSON string suitable for <see cref="IBackgroundJobQueue.EnqueueAsync"/>.
    /// </summary>
    /// <returns>The JSON representation of this payload.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Deserializes a <see cref="DemoDataSeedPayload"/> from the JSON stored on a background job.
    /// </summary>
    /// <param name="payloadJson">The JSON payload of the background job; may be null or empty.</param>
    /// <returns>The deserialized payload; a payload with an empty <see cref="UserId"/> when the JSON is empty.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="payloadJson"/> is not valid JSON.</exception>
    public static DemoDataSeedPayload FromJson(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new DemoDataSeedPayload(string.Empty);
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"The payload of the '{Handlers.DemoDataSeedingJobHandler.JobTypeName}' job is not valid JSON.", ex);
        }

        using (doc)
        {
            var root = doc.RootElement;
            var userId = root.TryGetProperty("userId", out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
            return new DemoDataSeedPayload(userId);
        }
    }
}
