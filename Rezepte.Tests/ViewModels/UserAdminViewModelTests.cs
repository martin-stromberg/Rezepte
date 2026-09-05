using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.ViewModels;
using Xunit;

namespace Rezepte.Tests.ViewModels;

/// <summary>
/// Class representing the user admin view model tests.
/// </summary>
public class UserAdminViewModelTests
{
    private const string UsersJson = """
        [
          { "id": "1", "username": "alice", "email": "alice@example.com", "isAdmin": true },
          { "id": "2", "username": "bob", "email": null, "isAdmin": false }
        ]
        """;

    private static (UserAdminViewModel Sut, ApiClientTestFactory Factory) CreateSut(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var factory = new ApiClientTestFactory(responseFactory);
        return (new UserAdminViewModel(factory.Create(), NullLogger<UserAdminViewModel>.Instance), factory);
    }

    /// <summary>
    /// Constructor should reject missing api client.
    /// </summary>
    [Fact]
    public void Constructor_ShouldRejectMissingApiClient()
    {
        Assert.Throws<ArgumentNullException>(() => new UserAdminViewModel(null!, NullLogger<UserAdminViewModel>.Instance));
    }

    /// <summary>
    /// Load async should populate users.
    /// </summary>
    [Fact]
    public async Task LoadAsync_ShouldPopulateUsers()
    {
        var (sut, factory) = CreateSut(_ => ApiClientTestFactory.Json(HttpStatusCode.OK, UsersJson));

        await sut.LoadAsync();

        sut.IsLoading.Should().BeFalse();
        sut.IsError.Should().BeFalse();
        sut.Users.Select(u => u.Username).Should().Equal("alice", "bob");
        factory.Requests[0].RequestUri!.AbsolutePath.Should().Be("/api/admin/users");
    }

    /// <summary>
    /// Load async should yield empty list for null payload.
    /// </summary>
    [Fact]
    public async Task LoadAsync_ShouldYieldEmptyListForNullPayload()
    {
        var (sut, _) = CreateSut(_ => ApiClientTestFactory.Json(HttpStatusCode.OK, "null"));

        await sut.LoadAsync();

        sut.Users.Should().BeEmpty();
        sut.IsError.Should().BeFalse();
    }

    /// <summary>
    /// Load async should report failure.
    /// </summary>
    [Fact]
    public async Task LoadAsync_ShouldReportFailure()
    {
        var (sut, _) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await sut.LoadAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Users could not be loaded.");
        sut.IsLoading.Should().BeFalse();
    }

    /// <summary>
    /// Reload async should request users again.
    /// </summary>
    [Fact]
    public async Task ReloadAsync_ShouldRequestUsersAgain()
    {
        var (sut, factory) = CreateSut(_ => ApiClientTestFactory.Json(HttpStatusCode.OK, UsersJson));

        await sut.LoadAsync();
        await sut.ReloadAsync();

        factory.Requests.Should().HaveCount(2);
        sut.Users.Should().HaveCount(2);
    }

    /// <summary>
    /// Filtered should match username and email case insensitively.
    /// </summary>
    [Fact]
    public async Task Filtered_ShouldMatchUsernameAndEmailCaseInsensitively()
    {
        var (sut, _) = CreateSut(_ => ApiClientTestFactory.Json(HttpStatusCode.OK, UsersJson));
        await sut.LoadAsync();

        sut.Filtered.Should().HaveCount(2);

        sut.Query = "BO";
        sut.Filtered.Select(u => u.Username).Should().Equal("bob");

        sut.Query = "ALICE@EXAMPLE";
        sut.Filtered.Select(u => u.Username).Should().Equal("alice");

        sut.Query = "nobody";
        sut.Filtered.Should().BeEmpty();
    }

    /// <summary>
    /// Create async should require username and password.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldRequireUsernameAndPassword()
    {
        var (sut, factory) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.OK));

        sut.NewUser.Username = "  ";
        sut.NewUser.Password = "secret";
        await sut.CreateAsync();
        sut.Message.Should().Be("Username is required.");

        sut.NewUser.Username = "carol";
        sut.NewUser.Password = "   ";
        await sut.CreateAsync();
        sut.Message.Should().Be("Password is required.");

        sut.IsError.Should().BeTrue();
        factory.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Create async should add created user sorted and reset form.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAddCreatedUserSortedAndResetForm()
    {
        var (sut, factory) = CreateSut(request => request.Method == HttpMethod.Post
            ? ApiClientTestFactory.Json(HttpStatusCode.Created, "{\"id\":\"3\",\"username\":\"Aaron\",\"email\":null,\"isAdmin\":false}")
            : ApiClientTestFactory.Json(HttpStatusCode.OK, UsersJson));
        await sut.LoadAsync();

        sut.NewUser.Username = "  Aaron  ";
        sut.NewUser.Email = "  aaron@example.com ";
        sut.NewUser.Password = "secret";
        sut.NewUser.IsAdmin = true;

        await sut.CreateAsync();

        sut.IsError.Should().BeFalse();
        sut.Message.Should().Be("User created.");
        sut.Users.Select(u => u.Username).Should().Equal("Aaron", "alice", "bob");
        sut.NewUser.Username.Should().BeEmpty();
        sut.NewUser.Email.Should().BeEmpty();
        sut.NewUser.Password.Should().BeEmpty();
        sut.NewUser.IsAdmin.Should().BeFalse();
        factory.Requests.Last().Method.Should().Be(HttpMethod.Post);
    }

    /// <summary>
    /// Create async should report generic message for server problem details.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldReportGenericMessageForServerProblemDetails()
    {
        var (sut, _) = CreateSut(_ => ApiClientTestFactory.Error(HttpStatusCode.Conflict, "Name belegt."));
        sut.NewUser.Username = "carol";
        sut.NewUser.Password = "secret";

        await sut.CreateAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Create failed.");
        sut.Users.Should().BeEmpty();
        sut.NewUser.Username.Should().Be("carol");
    }

    /// <summary>
    /// Create async should handle transport failures.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldHandleTransportFailures()
    {
        var (sut, _) = CreateSut(_ => throw new HttpRequestException("offline"));
        sut.NewUser.Username = "carol";
        sut.NewUser.Password = "secret";

        await sut.CreateAsync();

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Create failed.");
        sut.IsBusy.Should().BeFalse();
    }

    /// <summary>
    /// Save async should put user and report success.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ShouldPutUserAndReportSuccess()
    {
        var (sut, factory) = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var user = new UserAdminViewModel.UserRow { Id = "1", Username = "alice" };

        await sut.SaveAsync(user);

        sut.IsError.Should().BeFalse();
        sut.Message.Should().Be("Saved.");
        factory.Requests[0].Method.Should().Be(HttpMethod.Put);
        factory.Requests[0].RequestUri!.AbsolutePath.Should().Be("/api/admin/users/1");
    }

    /// <summary>
    /// Save async should report failure.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ShouldReportFailure()
    {
        var (sut, _) = CreateSut(_ => ApiClientTestFactory.Error(HttpStatusCode.BadRequest));
        var user = new UserAdminViewModel.UserRow { Id = "1", Username = "alice" };

        await sut.SaveAsync(user);

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Save failed.");
    }

    /// <summary>
    /// Delete async should remove user on success.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveUserOnSuccess()
    {
        var (sut, factory) = CreateSut(request => request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : ApiClientTestFactory.Json(HttpStatusCode.OK, UsersJson));
        await sut.LoadAsync();
        var user = sut.Users.Single(u => u.Id == "2");

        await sut.DeleteAsync(user);

        sut.IsError.Should().BeFalse();
        sut.Message.Should().Be("Deleted.");
        sut.Users.Select(u => u.Id).Should().Equal("1");
        factory.Requests.Last().RequestUri!.AbsolutePath.Should().Be("/api/admin/users/2");
    }

    /// <summary>
    /// Delete async should keep user on failure.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldKeepUserOnFailure()
    {
        var (sut, _) = CreateSut(request => request.Method == HttpMethod.Delete
            ? new HttpResponseMessage(HttpStatusCode.Forbidden)
            : ApiClientTestFactory.Json(HttpStatusCode.OK, UsersJson));
        await sut.LoadAsync();

        await sut.DeleteAsync(sut.Users.Single(u => u.Id == "1"));

        sut.IsError.Should().BeTrue();
        sut.Message.Should().Be("Delete failed.");
        sut.Users.Should().HaveCount(2);
    }
}
