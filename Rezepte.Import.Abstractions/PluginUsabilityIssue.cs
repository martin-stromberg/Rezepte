namespace Rezepte.Import.Abstractions;

/// <summary>
/// Describes a single issue that prevents an import plugin from being usable.
/// </summary>
/// <param name="Message">A human-readable description of the issue.</param>
/// <param name="Hint">An optional hint that helps resolving the issue.</param>
/// <returns>A new instance of the <see cref="PluginUsabilityIssue"/> record.</returns>
public sealed record PluginUsabilityIssue(string Message, string? Hint);
