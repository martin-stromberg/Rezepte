using System.Net;
using System.Text;
using Rezepte.Web.Dtos;

namespace Rezepte.Web.Services;

public class SecurityTxtRenderer : ISecurityTxtRenderer
{
    public string RenderPlainText(SecurityTxtSettings settings)
    {
        var sb = new StringBuilder();

        AppendMultiline(sb, "Contact", settings.Contact);
        AppendSingle(sb, "Expires", settings.Expires?.ToString("O"));
        AppendSingle(sb, "Encryption", settings.Encryption);
        AppendMultiline(sb, "Acknowledgments", settings.Acknowledgments);
        AppendSingle(sb, "Preferred-Languages", settings.PreferredLanguages);
        AppendSingle(sb, "Canonical", settings.Canonical);
        AppendSingle(sb, "Policy", settings.Policy);
        AppendSingle(sb, "Hiring", settings.Hiring);

        return sb.ToString();
    }

    public string RenderMarkdown(SecurityTxtSettings settings)
    {
        var sb = new StringBuilder();

        AppendMarkdownSection(sb, "Contact", settings.Contact);
        AppendMarkdownSection(sb, "Expires", settings.Expires?.ToString("O"));
        AppendMarkdownSection(sb, "Encryption", settings.Encryption);
        AppendMarkdownSection(sb, "Acknowledgments", settings.Acknowledgments);
        AppendMarkdownSection(sb, "Preferred-Languages", settings.PreferredLanguages);
        AppendMarkdownSection(sb, "Canonical", settings.Canonical);
        AppendMarkdownSection(sb, "Policy", settings.Policy);
        AppendMarkdownSection(sb, "Hiring", settings.Hiring);

        return sb.ToString();
    }

    public string RenderHtml(SecurityTxtSettings settings)
    {
        var sb = new StringBuilder();

        AppendHtmlSection(sb, "Contact", settings.Contact);
        AppendHtmlSection(sb, "Expires", settings.Expires?.ToString("O"));
        AppendHtmlSection(sb, "Encryption", settings.Encryption);
        AppendHtmlSection(sb, "Acknowledgments", settings.Acknowledgments);
        AppendHtmlSection(sb, "Preferred-Languages", settings.PreferredLanguages);
        AppendHtmlSection(sb, "Canonical", settings.Canonical);
        AppendHtmlSection(sb, "Policy", settings.Policy);
        AppendHtmlSection(sb, "Hiring", settings.Hiring);

        return sb.ToString();
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
        sb.AppendLine($"<h2>{WebUtility.HtmlEncode(key)}</h2><p>{WebUtility.HtmlEncode(value)}</p>");
    }
}
