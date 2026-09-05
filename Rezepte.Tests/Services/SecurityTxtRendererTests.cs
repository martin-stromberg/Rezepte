using FluentAssertions;
using Rezepte.Web.Dtos;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the security txt renderer tests.
/// </summary>
public class SecurityTxtRendererTests
{
    private static readonly SecurityTxtSettings FullSettings = new(
        Enabled: true,
        Contact: "mailto:security@example.com",
        Expires: new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
        Encryption: "https://example.com/pgp.txt",
        Acknowledgments: "https://example.com/thanks",
        PreferredLanguages: "de, en",
        Canonical: "https://example.com/.well-known/security.txt",
        Policy: "https://example.com/policy",
        Hiring: "https://example.com/jobs");

    /// <summary>
    /// Render plain text should return rfc9116 format when all fields set.
    /// </summary>
    [Fact]
    public void RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet()
    {
        var sut = new SecurityTxtRenderer();

        var result = sut.RenderPlainText(FullSettings);

        result.Should().Contain("Contact: mailto:security@example.com");
        result.Should().Contain("Encryption: https://example.com/pgp.txt");
        result.Should().Contain("Preferred-Languages: de, en");
        result.Should().Contain("Canonical: https://example.com/.well-known/security.txt");
        result.Should().Contain("Policy: https://example.com/policy");
        result.Should().Contain("Hiring: https://example.com/jobs");
        result.Should().Contain("Acknowledgments: https://example.com/thanks");
    }

    /// <summary>
    /// Render plain text should repeat directive for multiline contact.
    /// </summary>
    [Fact]
    public void RenderPlainText_ShouldRepeatDirective_ForMultilineContact()
    {
        var settings = FullSettings with { Contact = "mailto:a@example.com\nmailto:b@example.com" };
        var sut = new SecurityTxtRenderer();

        var result = sut.RenderPlainText(settings);

        result.Should().Contain("Contact: mailto:a@example.com");
        result.Should().Contain("Contact: mailto:b@example.com");
    }

    /// <summary>
    /// Render markdown should return section headers.
    /// </summary>
    [Fact]
    public void RenderMarkdown_ShouldReturnSectionHeaders()
    {
        var sut = new SecurityTxtRenderer();

        var result = sut.RenderMarkdown(FullSettings);

        result.Should().Contain("## Contact");
        result.Should().Contain("## Expires");
        result.Should().Contain("## Policy");
    }

    /// <summary>
    /// Render html should return h2 and paragraph.
    /// </summary>
    [Fact]
    public void RenderHtml_ShouldReturnH2AndParagraph()
    {
        var sut = new SecurityTxtRenderer();

        var result = sut.RenderHtml(FullSettings);

        result.Should().Contain("<h2>Contact</h2>");
        result.Should().Contain("<h2>Expires</h2>");
        result.Should().Contain("<h2>Policy</h2>");
        result.Should().Contain("<p>");
    }

    /// <summary>
    /// Render html should return html document structure.
    /// </summary>
    [Fact]
    public void RenderHtml_ShouldReturnHtmlDocumentStructure()
    {
        var sut = new SecurityTxtRenderer();

        var result = sut.RenderHtml(FullSettings);

        result.Should().Contain("<html>");
        result.Should().Contain("<body>");
        result.Should().Contain("</body>");
        result.Should().Contain("</html>");
    }

    /// <summary>
    /// Render html should render multiline values as separate paragraphs.
    /// </summary>
    [Fact]
    public void RenderHtml_ShouldRenderMultilineValuesAsSeparateParagraphs()
    {
        var settings = FullSettings with
        {
            Contact = "mailto:a@example.com\nmailto:b@example.com"
        };
        var sut = new SecurityTxtRenderer();

        var result = sut.RenderHtml(settings);

        result.Should().Contain("<p>mailto:a@example.com</p>");
        result.Should().Contain("<p>mailto:b@example.com</p>");
    }

    /// <summary>
    /// Render plain text should omit empty fields.
    /// </summary>
    [Fact]
    public void RenderPlainText_ShouldOmitEmptyFields()
    {
        var settings = new SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);
        var sut = new SecurityTxtRenderer();

        var result = sut.RenderPlainText(settings);

        result.Should().NotContain("Encryption:");
        result.Should().NotContain("Acknowledgments:");
        result.Should().NotContain("Policy:");
        result.Should().NotContain("Hiring:");
    }
}
