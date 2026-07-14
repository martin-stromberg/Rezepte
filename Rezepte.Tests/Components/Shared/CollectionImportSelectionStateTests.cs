using FluentAssertions;
using Rezepte.Web.Components.Shared;
using Xunit;

namespace Rezepte.Tests.Components.Shared;

public sealed class CollectionImportSelectionStateTests
{
    [Fact]
    public void InitializeItems_ShouldSelectItemsAndUseDefaultTargetCookbook()
    {
        var state = new CollectionImportSelectionState();

        state.Reset("cookbook-default");
        state.InitializeItems(["item-1", "item-2"], readOnly: false);

        state.SelectedItemIds.Should().BeEquivalentTo("item-1", "item-2");
        state.GetTargetCookbook("item-1").Should().Be("cookbook-default");
        state.GetTargetCookbook("item-2").Should().Be("cookbook-default");
        state.BulkTargetCookbookId.Should().Be("cookbook-default");
    }

    [Fact]
    public void ClearSelectionAndSelectAll_ShouldPreserveDisabledItems()
    {
        var state = new CollectionImportSelectionState();
        state.Reset("cookbook-default");
        state.InitializeItems(["item-1", "item-2", "item-3"], readOnly: false);
        state.ReplaceItemStates([
            ("item-2", "Succeeded")
        ]);

        state.ClearSelection(readOnly: false);

        state.SelectedItemIds.Should().BeEquivalentTo("item-2");

        state.SelectAll(readOnly: false);

        state.SelectedItemIds.Should().BeEquivalentTo("item-1", "item-2", "item-3");
    }

    [Fact]
    public void ApplyBulkTargetCookbook_ShouldUpdateOnlySelectedEnabledItems()
    {
        var state = new CollectionImportSelectionState();
        state.Reset("cookbook-default");
        state.InitializeItems(["item-1", "item-2", "item-3"], readOnly: false);
        state.ReplaceItemStates([
            ("item-2", "Importing")
        ]);
        state.ToggleItem("item-3", selected: false);
        state.SetBulkTargetCookbook("cookbook-bulk");

        state.ApplyBulkTargetCookbook(readOnly: false);

        state.GetTargetCookbook("item-1").Should().Be("cookbook-bulk");
        state.GetTargetCookbook("item-2").Should().Be("cookbook-default");
        state.GetTargetCookbook("item-3").Should().Be("cookbook-default");
    }

    [Fact]
    public void CanSubmit_ShouldRequireSessionSelectionAndTargetCookbooks()
    {
        var state = new CollectionImportSelectionState();
        state.Reset("cookbook-default");
        state.InitializeItems(["item-1"], readOnly: false);

        state.CanSubmit("session-1", readOnly: false).Should().BeTrue();

        state.SetTargetCookbook("item-1", null);

        state.CanSubmit("session-1", readOnly: false).Should().BeFalse();
        state.CanSubmit(null, readOnly: false).Should().BeFalse();
        state.CanSubmit("session-1", readOnly: true).Should().BeFalse();
    }
}
