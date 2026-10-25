using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Rezepte.Web.Components.Layout;
using System.Reflection;
using Xunit;

namespace Rezepte.Tests.Components;

/// <summary>
/// Class representing the main layout nav search tests.
/// </summary>
public sealed class MainLayoutSearchTests
{
    /// <summary>
    /// On submit search navigates to the search page when the bound search query is null,
    /// because <see cref="Microsoft.AspNetCore.Components.Forms.InputText"/> binds at
    /// <see langword="string?"/> level and can assign null to the bound field even though
    /// the field is declared as non-nullable <see langword="string"/>. The submit must not
    /// crash with a <see cref="NullReferenceException"/> in that case.
    /// The handler and its bound field are exercised via reflection because the layout
    /// cannot be rendered standalone (its <see cref="Microsoft.AspNetCore.Components.Forms.InputText"/>
    /// requires a cascading <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/>).
    /// </summary>
    [Fact]
    public async Task OnSubmitSearch_WhenBoundSearchQueryIsNull_NavigatesToSearchPage()
    {
        var navigationManager = new TestNavigationManager();
        var layout = new MainLayout();

        typeof(MainLayout)
            .GetProperty("Nav", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)!
            .SetValue(layout, navigationManager);
        typeof(MainLayout)
            .GetField("searchQuery", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(layout, null);

        var onSubmitSearch = typeof(MainLayout)
            .GetMethod("OnSubmitSearch", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var act = async () => await (Task)onSubmitSearch.Invoke(layout, null)!;

        await act.Should().NotThrowAsync();
        navigationManager.Uri.Should().Be("http://localhost/recipes/search?q=");
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).AbsoluteUri;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = ToAbsoluteUri(uri).AbsoluteUri;
        }
    }
}
