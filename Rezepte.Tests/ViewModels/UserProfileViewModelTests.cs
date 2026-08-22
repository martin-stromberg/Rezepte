using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.ViewModels;
using Xunit;

namespace Rezepte.Tests.ViewModels;

public class UserProfileViewModelTests
{
    private const string ProfileJson = "{\"id\":\"user-1\",\"username\":\"alice\",\"email\":\"alice@example.com\"}";

    private static (UserProfileViewModel Sut, ApiClientTestFactory Factory, List<string> Notifications) CreateSut(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var factory = new ApiClientTestFactory(responseFactory);
        var sut = new UserProfileViewModel(factory.Create(), NullLogger<UserProfileViewModel>.Instance);
        var notifications = new List<string>();
        sut.OnChange += () => notifications.Add(sut.Message ?? string.Empty);
        return (sut, factory, notifications);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateProfile()
    {
        var (sut, _, notifications) = CreateSut(_ => ApiClientTestFactory.Json(HttpStatusCode.OK, ProfileJson));
        var profileInstance = sut.Profile;

        await sut.LoadAsync();

        sut.IsLoading.Should().BeFalse();
        sut.IsError.Should().BeFalse();
        sut.Message.Should().BeNull();
        sut.Profile.Should().BeSameAs(profileInstance);
        sut.Profile.Id.Should().Be("user-1");
        sut.Profile.Username.Should().Be("alice");
        sut.Profile.Email.Should().Be("alice@example.com");
        notifications.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldReportUnauthorized()
    {
        var (sut, _, _) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await sut.LoadAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Not signed in.");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldFallBackToGenericMessageForServerProblemDetails()
    {
        var (sut, _, _) = CreateSut(_ => ApiClientTestFactory.Error(HttpStatusCode.BadRequest, "Server sagt nein."));

        await sut.LoadAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Profile could not be loaded.");
    }

    [Fact]
    public async Task LoadAsync_ShouldFallBackToGenericMessageWithoutServerMessage()
    {
        var (sut, _, _) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await sut.LoadAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Profile could not be loaded.");
    }

    [Fact]
    public async Task LoadAsync_ShouldReportMissingUserForEmptyPayload()
    {
        var (sut, _, _) = CreateSut(_ => ApiClientTestFactory.Json(HttpStatusCode.OK, "null"));

        await sut.LoadAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task LoadAsync_ShouldHandleTransportFailures()
    {
        var (sut, _, _) = CreateSut(_ => throw new HttpRequestException("offline"));

        await sut.LoadAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Profile could not be loaded.");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SaveProfileAsync_ShouldTrimInputAndApplyServerResponse()
    {
        var (sut, factory, _) = CreateSut(_ => ApiClientTestFactory.Json(
            HttpStatusCode.OK,
            "{\"id\":\"user-1\",\"username\":\"bob\",\"email\":null}"));
        sut.Profile.Username = "  bob  ";
        sut.Profile.Email = "   ";

        await sut.SaveProfileAsync();

        sut.IsBusy.Should().BeFalse();
        sut.IsError.Should().BeFalse();
        sut.Message.Should().Be("Profile saved.");
        sut.Profile.Username.Should().Be("bob");
        sut.Profile.Email.Should().BeEmpty();
        factory.Requests.Should().ContainSingle();
        factory.Requests[0].Method.Should().Be(HttpMethod.Put);
        factory.Requests[0].RequestUri!.AbsolutePath.Should().Be("/api/users/me");
    }

    [Fact]
    public async Task SaveProfileAsync_ShouldReportServerError()
    {
        var (sut, _, _) = CreateSut(_ => ApiClientTestFactory.Error(HttpStatusCode.Conflict, "Name belegt."));

        await sut.SaveProfileAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Profile could not be saved.");
    }

    [Fact]
    public async Task SaveProfileAsync_ShouldHandleTransportFailures()
    {
        var (sut, _, _) = CreateSut(_ => throw new HttpRequestException("offline"));

        await sut.SaveProfileAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Profile could not be saved.");
        sut.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldRejectMismatchedConfirmationWithoutRequest()
    {
        var (sut, factory, _) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.OK));
        sut.Password.CurrentPassword = "old-secret";
        sut.Password.NewPassword = "new-secret";
        sut.Password.ConfirmPassword = "other-secret";

        await sut.ChangePasswordAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("The new passwords do not match.");
        factory.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldClearFieldsOnSuccess()
    {
        var (sut, factory, _) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var passwordInstance = sut.Password;
        sut.Password.CurrentPassword = "old-secret";
        sut.Password.NewPassword = "new-secret";
        sut.Password.ConfirmPassword = "new-secret";

        await sut.ChangePasswordAsync();

        sut.IsError.Should().BeFalse();
        sut.Message.Should().Be("Password changed.");
        sut.Password.Should().BeSameAs(passwordInstance);
        sut.Password.CurrentPassword.Should().BeEmpty();
        sut.Password.NewPassword.Should().BeEmpty();
        sut.Password.ConfirmPassword.Should().BeEmpty();
        factory.Requests[0].RequestUri!.AbsolutePath.Should().Be("/api/users/me/change-password");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldKeepFieldsOnServerError()
    {
        var (sut, _, _) = CreateSut(_ => ApiClientTestFactory.Error(HttpStatusCode.BadRequest, "Falsches Passwort."));
        sut.Password.CurrentPassword = "old-secret";
        sut.Password.NewPassword = "new-secret";
        sut.Password.ConfirmPassword = "new-secret";

        await sut.ChangePasswordAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Password could not be changed.");
        sut.Password.CurrentPassword.Should().Be("old-secret");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldHandleTransportFailures()
    {
        var (sut, _, _) = CreateSut(_ => throw new HttpRequestException("offline"));

        await sut.ChangePasswordAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Password could not be changed.");
        sut.IsBusy.Should().BeFalse();
    }
}
