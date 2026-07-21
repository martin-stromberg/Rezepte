namespace Rezepte.Tests.TestHelpers;

public sealed class EnvironmentVariableScope : IDisposable
{
    public const string ServiceAccountEnvironmentVariable = "GOOGLE_APPLICATION_CREDENTIALS";
    public const string GeminiApiKeyEnvironmentVariable = "GOOGLE_GEMINI_API_KEY";

    private readonly string? _originalServiceAccount = Environment.GetEnvironmentVariable(ServiceAccountEnvironmentVariable);
    private readonly string? _originalGeminiApiKey = Environment.GetEnvironmentVariable(GeminiApiKeyEnvironmentVariable);

    public void Set(string variable, string? value)
    {
        Environment.SetEnvironmentVariable(variable, value);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ServiceAccountEnvironmentVariable, _originalServiceAccount);
        Environment.SetEnvironmentVariable(GeminiApiKeyEnvironmentVariable, _originalGeminiApiKey);
    }
}
