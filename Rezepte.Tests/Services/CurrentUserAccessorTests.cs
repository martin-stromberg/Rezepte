using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class CurrentUserAccessorTests
{
    private static ClaimsPrincipal CreatePrincipal(string name)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));

    [Fact]
    public void User_ShouldBeNullInitially()
    {
        var sut = new CurrentUserAccessor();

        sut.User.Should().BeNull();
    }

    [Fact]
    public async Task WaitForUserAsync_ShouldCompleteImmediatelyWhenUserIsAlreadySet()
    {
        var principal = CreatePrincipal("alice");
        var sut = new CurrentUserAccessor { User = principal };

        var result = await sut.WaitForUserAsync();

        result.Should().BeSameAs(principal);
    }

    [Fact]
    public async Task WaitForUserAsync_ShouldCompleteWhenUserIsSetLater()
    {
        var sut = new CurrentUserAccessor();
        var pending = sut.WaitForUserAsync();
        pending.IsCompleted.Should().BeFalse();

        var principal = CreatePrincipal("alice");
        sut.User = principal;

        (await pending).Should().BeSameAs(principal);
    }

    [Fact]
    public async Task WaitForUserAsync_ShouldReturnFirstAssignedUserForPendingWaiters()
    {
        var sut = new CurrentUserAccessor();
        var pending = sut.WaitForUserAsync();

        var first = CreatePrincipal("first");
        sut.User = first;
        sut.User = CreatePrincipal("second");

        (await pending).Should().BeSameAs(first);
        sut.User!.Identity!.Name.Should().Be("second");
    }

    [Fact]
    public async Task WaitForUserAsync_ShouldCancelWhenTokenIsCancelled()
    {
        var sut = new CurrentUserAccessor();
        using var cts = new CancellationTokenSource();

        var pending = sut.WaitForUserAsync(cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task WaitForUserAsync_ShouldIgnoreCancellationAfterUserWasSet()
    {
        var sut = new CurrentUserAccessor();
        using var cts = new CancellationTokenSource();
        var pending = sut.WaitForUserAsync(cts.Token);

        var principal = CreatePrincipal("alice");
        sut.User = principal;
        await cts.CancelAsync();

        (await pending).Should().BeSameAs(principal);
    }

    [Fact]
    public async Task WaitForUserAsync_ShouldReturnUserForAlreadyCancelledTokenWhenUserIsSet()
    {
        var principal = CreatePrincipal("alice");
        var sut = new CurrentUserAccessor { User = principal };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        (await sut.WaitForUserAsync(cts.Token)).Should().BeSameAs(principal);
    }
}
