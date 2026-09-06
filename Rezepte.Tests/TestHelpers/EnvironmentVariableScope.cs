namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Class representing the environment variable scope.
/// </summary>
public sealed class EnvironmentVariableScope : IDisposable
{
    // Intentionally duplicated from the private constants in GoogleCredentialsProvider:
    // these names are kept test-local (rather than shared via internal visibility) to keep
    // the test helper independent of the production class's implementation details.

    /// <summary>
    /// The service account environment variable value.
    /// </summary>
    public const string ServiceAccountEnvironmentVariable = "GOOGLE_APPLICATION_CREDENTIALS";
    /// <summary>
    /// The gemini api key environment variable value.
    /// </summary>
    public const string GeminiApiKeyEnvironmentVariable = "GOOGLE_GEMINI_API_KEY";

    private readonly string? _originalServiceAccount = Environment.GetEnvironmentVariable(ServiceAccountEnvironmentVariable);
    private readonly string? _originalGeminiApiKey = Environment.GetEnvironmentVariable(GeminiApiKeyEnvironmentVariable);

    /// <summary>
    /// Set.
    /// </summary>
    /// <param name="variable">The variable parameter.</param>
    /// <param name="value">The value parameter.</param>
    public void Set(string variable, string? value)
    {
        Environment.SetEnvironmentVariable(variable, value);
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ServiceAccountEnvironmentVariable, _originalServiceAccount);
        Environment.SetEnvironmentVariable(GeminiApiKeyEnvironmentVariable, _originalGeminiApiKey);
    }
}
