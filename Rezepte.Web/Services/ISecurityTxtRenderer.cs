using Rezepte.Web.Dtos;

namespace Rezepte.Web.Services;

public interface ISecurityTxtRenderer
{
    string RenderPlainText(SecurityTxtSettings settings);
    string RenderMarkdown(SecurityTxtSettings settings);
    string RenderHtml(SecurityTxtSettings settings);
}
