using Rezepte.Web.Dtos;

namespace Rezepte.Web.Services;

/// <summary>
/// Defines the isecurity txt renderer interface.
/// </summary>
public interface ISecurityTxtRenderer
{
    /// <summary>
    /// Renders the plain text.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <returns>The result.</returns>
    string RenderPlainText(SecurityTxtSettings settings);
    /// <summary>
    /// Renders the markdown.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <returns>The result.</returns>
    string RenderMarkdown(SecurityTxtSettings settings);
    /// <summary>
    /// Renders the html.
    /// </summary>
    /// <param name="settings">The settings parameter.</param>
    /// <returns>The result.</returns>
    string RenderHtml(SecurityTxtSettings settings);
}
