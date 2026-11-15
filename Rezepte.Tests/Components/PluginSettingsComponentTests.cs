using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Web.Components.Settings;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Components;

/// <summary>
/// bUnit tests for the <see cref="PluginSettings"/> component: per-source update check button,
/// busy state, localized result alerts, and the localized explanation text.
/// </summary>
public sealed class PluginSettingsComponentTests : IDisposable
{
    private readonly TestContext _context = new();
    private readonly Mock<IPluginSettingsService> _service = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSettingsComponentTests"/> class.
    /// </summary>
    public PluginSettingsComponentTests()
    {
        _context.Services.AddLocalization();
        _service.Setup(s => s.GetPluginsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PluginSettingsItem>());
        _context.Services.AddSingleton(_service.Object);
    }

    /// <summary>
    /// The localized explanation text is rendered above the sources table.
    /// </summary>
    [Fact]
    public void InfoText_IsRendered()
    {
        WithSources(Source());
        var cut = Render();

        cut.Markup.Should().Contain("Pluginquellen werden beim App-Start automatisch auf Updates geprüft");
    }

    /// <summary>
    /// The check button is disabled and explains the reason via title when the source is not
    /// enabled or not confirmed as trusted.
    /// </summary>
    [Fact]
    public void CheckButton_Disabled_WhenSourceNotEnabledOrUntrusted()
    {
        WithSources(
            Source(id: "src-disabled", enabled: false),
            Source(id: "src-untrusted", trustConfirmed: false));
        var cut = Render();

        var disabledButton = cut.Find("button[aria-label='Quelle ist deaktiviert.']");
        disabledButton.HasAttribute("disabled").Should().BeTrue();
        disabledButton.GetAttribute("title").Should().Be("Quelle ist deaktiviert.");
        disabledButton.ParentElement!.GetAttribute("title").Should().Be("Quelle ist deaktiviert.");

        var untrustedButton = cut.Find("button[aria-label='Vertrauen ist nicht bestätigt.']");
        untrustedButton.HasAttribute("disabled").Should().BeTrue();
        untrustedButton.GetAttribute("title").Should().Be("Vertrauen ist nicht bestätigt.");
        untrustedButton.ParentElement!.GetAttribute("title").Should().Be("Vertrauen ist nicht bestätigt.");
    }

    /// <summary>
    /// Clicking the check button calls the service with the source id and shows the localized
    /// installed result message.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_CallsService_AndShowsInstalled()
    {
        WithSources(Source());
        _service.Setup(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PluginUpdateCheckResult.Installed("v1.2.3"));
        var cut = Render();

        CheckButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-success").TextContent.Should().Contain("Version v1.2.3 installiert.");
        });
        _service.Verify(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// A NoUpdate result shows the localized "no update found" success alert.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_ShowsNoUpdate()
    {
        WithSources(Source());
        _service.Setup(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PluginUpdateCheckResult.NoUpdate("v1.0.0", alreadyInstalled: true));
        var cut = Render();

        CheckButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-success").TextContent.Should().Contain("Kein Update gefunden.");
        });
    }

    /// <summary>
    /// A failed result with a known server-side error string is mapped to the localized message.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_ShowsLocalizedFailed()
    {
        WithSources(Source());
        _service.Setup(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PluginUpdateCheckResult.Failed("Private source has no stored PAT."));
        var cut = Render();

        CheckButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-danger").TextContent.Should().Contain("Für die private Quelle ist kein PAT hinterlegt.");
        });
    }

    /// <summary>
    /// An exception thrown by the service shows the generic localized failure alert with the
    /// exception message appended as detail.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_WhenServiceThrows_ShowsFailedAlert()
    {
        WithSources(Source());
        _service.Setup(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom-check"));
        var cut = Render();

        CheckButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger").TextContent;
            alert.Should().Contain("Prüfung fehlgeschlagen.");
            alert.Should().Contain("boom-check");
        });
    }

    /// <summary>
    /// The guard prevents the service call when the check button is triggered for a source that
    /// is not enabled.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_WhenSourceDisabled_DoesNotCallService()
    {
        WithSources(Source(enabled: false));
        var cut = Render();

        // bUnit fires the click handler even on a disabled button; the guard inside
        // CheckSourceAsync must suppress the service call anyway.
        cut.Find("button[aria-label='Quelle ist deaktiviert.']").Click();

        _service.Verify(s => s.CheckSourceForUpdatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// A failed result with an unknown error string shows the generic localized failure message
    /// with the error text appended as detail.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_ShowsFailedWithDetail_ForUnknownError()
    {
        WithSources(Source());
        _service.Setup(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PluginUpdateCheckResult.Failed("some weird server error"));
        var cut = Render();

        CheckButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger").TextContent;
            alert.Should().Contain("Prüfung fehlgeschlagen.");
            alert.Should().Contain("some weird server error");
        });
    }

    /// <summary>
    /// While the check is in flight the button shows a spinner and all row action buttons are
    /// disabled; the other row's buttons explain the block with the busy tooltip. After
    /// completion the rows are enabled again.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_SetsBusy_AndDisablesRow()
    {
        WithSources(Source(id: "src-1"), Source(id: "src-2"));
        var pending = new TaskCompletionSource<PluginUpdateCheckResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.Setup(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        var cut = Render();

        CheckButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".spinner-border").Should().NotBeNull();
            cut.FindAll("td.text-end button").Should().OnlyContain(b => b.HasAttribute("disabled"));
            cut.FindAll("td.text-end button[title]")
                .Should().OnlyContain(b => b.GetAttribute("title") == "Ein Vorgang läuft gerade.");
        });

        pending.SetResult(PluginUpdateCheckResult.NoUpdate());

        cut.WaitForAssertion(() =>
        {
            CheckButton(cut).HasAttribute("disabled").Should().BeFalse();
            CheckButton(cut).GetAttribute("title").Should().Be("Auf Updates prüfen");
        });
    }

    /// <summary>
    /// After the check finishes the sources are reloaded so the status column reflects the
    /// updated state.
    /// </summary>
    [Fact]
    public void CheckSourceAsync_ReloadsSources()
    {
        WithSources(Source());
        _service.Setup(s => s.CheckSourceForUpdatesAsync("src-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PluginUpdateCheckResult.NoUpdate());
        var cut = Render();

        CheckButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            _service.Verify(s => s.GetSourcesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        });
    }

    /// <summary>
    /// All three source action buttons have localized title and aria-label attributes.
    /// </summary>
    [Fact]
    public void ActionButtons_HaveLocalizedTooltipAndAriaLabel()
    {
        WithSources(Source());
        var cut = Render();

        AssertIconButton(cut, "bi-arrow-repeat", "Auf Updates prüfen");
        AssertIconButton(cut, "bi-pencil", "Bearbeiten");
        AssertIconButton(cut, "bi-trash", "Löschen");
    }

    /// <summary>
    /// The remove button is only rendered for rows whose status is <see cref="PluginStatus.Missing"/>;
    /// a loaded plugin row does not show a trash button.
    /// </summary>
    [Fact]
    public void RemoveButton_OnlyForMissingStatus()
    {
        WithSources();
        WithPlugins(
            Plugin("p-loaded", status: PluginStatus.Loaded, displayName: "Loaded Plugin"),
            Plugin("p-missing", displayName: "Missing Plugin"));
        var cut = Render();

        var trashIcon = cut.FindAll("i.bi-trash").Should().ContainSingle().Which;
        trashIcon.Closest("button")!.GetAttribute("aria-label")
            .Should().Be("Plugin-Eintrag Missing Plugin entfernen");
    }

    /// <summary>
    /// The remove button carries the parameterized localized text in title, aria-label, and the
    /// tooltip wrapper's title.
    /// </summary>
    [Fact]
    public void RemoveButton_HasLocalizedTooltipAndAriaLabel()
    {
        WithSources();
        WithPlugins(Plugin("p-missing", displayName: "Missing Plugin"));
        var cut = Render();

        AssertIconButton(cut, "bi-trash", "Plugin-Eintrag Missing Plugin entfernen");
    }

    /// <summary>
    /// Clicking the remove button calls the service with the plugin id, reloads the plugin list,
    /// shows the localized success alert, and the removed row disappears.
    /// </summary>
    [Fact]
    public void RemovePluginAsync_CallsService_AndReloads()
    {
        WithSources();
        WithPlugins(Plugin("p-missing", displayName: "Missing Plugin"));
        var cut = Render();

        // The reload after the removal must no longer return the entry.
        WithPlugins();
        cut.Find("button[aria-label='Plugin-Eintrag Missing Plugin entfernen']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".alert-success").TextContent.Should().Contain("Plugin-Eintrag entfernt.");
            cut.Markup.Should().NotContain("Missing Plugin");
        });
        _service.Verify(s => s.RemovePluginAsync("p-missing", It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(s => s.GetPluginsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>
    /// When the service throws because the entry no longer exists or is no longer missing, the
    /// localized danger alert is shown and the row stays visible.
    /// </summary>
    [Fact]
    public void RemovePluginAsync_WhenServiceThrows_ShowsDangerAlert()
    {
        WithSources();
        WithPlugins(Plugin("p-missing", displayName: "Missing Plugin"));
        _service.Setup(s => s.RemovePluginAsync("p-missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("stale entry"));
        var cut = Render();

        cut.Find("button[aria-label='Plugin-Eintrag Missing Plugin entfernen']").Click();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert-danger").TextContent;
            alert.Should().Contain("Plugin-Eintrag konnte nicht entfernt werden:");
            alert.Should().Contain("stale entry");
        });
        cut.FindAll("i.bi-trash").Should().HaveCount(1);
    }

    /// <summary>
    /// The status cell renders the LastErrorAt timestamp when present.
    /// </summary>
    [Fact]
    public void LastErrorAt_IsRendered()
    {
        WithSources(Source(
            lastError: "Private source has no stored PAT.",
            lastErrorAt: new DateTime(2026, 7, 20, 12, 30, 0, DateTimeKind.Utc)));
        var cut = Render();

        cut.Markup.Should().Contain("Letzter Fehler:");
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    private void WithSources(params PluginSourceSettingsItem[] sources)
    {
        _service.Setup(s => s.GetSourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);
    }

    private void WithPlugins(params PluginSettingsItem[] plugins)
    {
        _service.Setup(s => s.GetPluginsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(plugins);
    }

    private IRenderedComponent<PluginSettings> Render()
    {
        var cut = _context.RenderComponent<PluginSettings>();
        cut.WaitForAssertion(() => cut.FindAll("table").Should().NotBeEmpty());
        return cut;
    }

    private static IElement CheckButton(IRenderedComponent<PluginSettings> cut)
    {
        return cut.Find("button[aria-label='Auf Updates prüfen']");
    }

    private static void AssertIconButton(IRenderedComponent<PluginSettings> cut, string iconClass, string expectedText)
    {
        var icon = cut.Find($"i.{iconClass}");
        var button = icon.Closest("button")!;
        button.GetAttribute("title").Should().Be(expectedText);
        button.GetAttribute("aria-label").Should().Be(expectedText);
        button.ParentElement!.GetAttribute("title").Should().Be(expectedText);
    }

    private static PluginSourceSettingsItem Source(
        string id = "src-1",
        bool enabled = true,
        bool trustConfirmed = true,
        bool isPrivate = false,
        bool hasSecret = false,
        string? lastSuccessfulReleaseTag = null,
        string? lastError = null,
        DateTime? lastCheckedAt = null,
        DateTime? lastErrorAt = null)
    {
        return new PluginSourceSettingsItem(
            id,
            "https://github.com/owner/repo",
            "owner",
            "repo",
            isPrivate,
            enabled,
            trustConfirmed,
            hasSecret,
            lastSuccessfulReleaseTag,
            lastError,
            lastCheckedAt,
            lastErrorAt);
    }

    private static PluginSettingsItem Plugin(
        string pluginId,
        string status = PluginStatus.Missing,
        string? displayName = null,
        int orderIndex = 0)
    {
        var timestamp = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        return new PluginSettingsItem(
            pluginId,
            displayName ?? pluginId,
            null,
            "Rezepte.TestPlugin.dll",
            "Rezepte.TestPlugin.Handler",
            true,
            orderIndex,
            status,
            null,
            timestamp,
            timestamp);
    }
}
