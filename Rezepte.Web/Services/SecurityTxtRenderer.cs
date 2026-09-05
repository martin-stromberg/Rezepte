using System.Net;
using System.Text;
using Rezepte.Web.Dtos;

namespace Rezepte.Web.Services;

/// <summary>
/// Represents the security txt renderer class.
/// </summary>
public class SecurityTxtRenderer : ISecurityTxtRenderer
{
    /// <summary>
    /// Renders the plain text.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <returns>The result.</returns>
    public string RenderPlainText(SecurityTxtSettings settings)
    {
        var sb = new StringBuilder();

        foreach (var directive in GetDirectives(settings))
        {
            if (directive.Multiline)
            {
                AppendMultiline(sb, directive.Key, directive.Value);
                continue;
            }

            AppendSingle(sb, directive.Key, directive.Value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders the markdown.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <returns>The result.</returns>
    public string RenderMarkdown(SecurityTxtSettings settings)
    {
        var sb = new StringBuilder();

        foreach (var directive in GetDirectives(settings))
        {
            AppendMarkdownSection(sb, directive.Key, directive.Value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders the html.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <returns>The result.</returns>
    public string RenderHtml(SecurityTxtSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html>");
        sb.AppendLine("<body>");

        foreach (var directive in GetDirectives(settings))
        {
            AppendHtmlSection(sb, directive.Key, directive.Value);
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static IEnumerable<Directive> GetDirectives(SecurityTxtSettings settings)
    {
        yield return new Directive("Contact", settings.Contact, true);
        yield return new Directive("Expires", settings.Expires?.ToString("O"), false);
        yield return new Directive("Encryption", settings.Encryption, false);
        yield return new Directive("Acknowledgments", settings.Acknowledgments, true);
        yield return new Directive("Preferred-Languages", settings.PreferredLanguages, false);
        yield return new Directive("Canonical", settings.Canonical, false);
        yield return new Directive("Policy", settings.Policy, false);
        yield return new Directive("Hiring", settings.Hiring, false);
    }

    private static void AppendSingle(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.AppendLine($"{key}: {value}");
    }

    private static void AppendMultiline(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        foreach (var line in value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            sb.AppendLine($"{key}: {line}");
        }
    }

    private static void AppendMarkdownSection(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.AppendLine($"## {key}");
        sb.AppendLine();
        sb.AppendLine(value);
        sb.AppendLine();
    }

    private static void AppendHtmlSection(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.AppendLine($"<h2>{WebUtility.HtmlEncode(key)}</h2>");
        foreach (var line in value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            sb.AppendLine($"<p>{WebUtility.HtmlEncode(line)}</p>");
        }
    }

    private sealed record Directive(string Key, string? Value, bool Multiline);
}
