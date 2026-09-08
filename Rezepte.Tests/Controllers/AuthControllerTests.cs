using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rezepte.Web.Controllers;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Validation;
using Xunit;

namespace Rezepte.Tests.Controllers;

/// <summary>
/// Class representing the auth controller tests.
/// </summary>
public class AuthControllerTests
{
    /// <summary>
    /// Register should redirect form post with validation error when username is rejected.
    /// </summary>
    [Fact]
    public async Task Register_ShouldRedirectFormPostWithValidationError_WhenUsernameIsRejected()
    {
        var users = new Mock<IUserService>();
        users.Setup(service => service.RegisterAsync("admin", "password123", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, UsernameValidator.ReservedMessage, null));

        var controller = new AuthController(users.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateFormContext("Username=admin&Password=password123")
            }
        };

        var result = await controller.Register(CancellationToken.None);

        result.Should().BeOfType<LocalRedirectResult>()
            .Which.Url.Should().Be($"/register?error={Uri.EscapeDataString(UsernameValidator.ReservedMessage)}");
    }

    /// <summary>
    /// Register should call user service with create demo data true when form checkbox checked.
    /// </summary>
    [Fact]
    public async Task Register_ShouldCallUserServiceWithCreateDemoDataTrue_WhenFormCheckboxChecked()
    {
        var users = new Mock<IUserService>();
        users.Setup(service => service.RegisterAsync("alice", "password123", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, new Rezepte.Web.Services.User("1", "alice", string.Empty, string.Empty, false, DateTime.UtcNow)));

        var controller = new AuthController(users.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateFormContext("Username=alice&Password=password123&createDemoData=true")
            }
        };

        await controller.Register(CancellationToken.None);

        users.Verify(service => service.RegisterAsync("alice", "password123", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Register should call user service with create demo data false when form checkbox unchecked.
    /// </summary>
    [Fact]
    public async Task Register_ShouldCallUserServiceWithCreateDemoDataFalse_WhenFormCheckboxUnchecked()
    {
        var users = new Mock<IUserService>();
        users.Setup(service => service.RegisterAsync("bob", "password123", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, new Rezepte.Web.Services.User("2", "bob", string.Empty, string.Empty, false, DateTime.UtcNow)));

        var controller = new AuthController(users.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateFormContext("Username=bob&Password=password123")
            }
        };

        await controller.Register(CancellationToken.None);

        users.Verify(service => service.RegisterAsync("bob", "password123", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Register should call user service with create demo data from json when provided.
    /// </summary>
    [Fact]
    public async Task Register_ShouldCallUserServiceWithCreateDemoDataFromJson_WhenProvided()
    {
        var users = new Mock<IUserService>();
        users.Setup(service => service.RegisterAsync("carol", "password123", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, new Rezepte.Web.Services.User("3", "carol", string.Empty, string.Empty, false, DateTime.UtcNow)));

        var controller = new AuthController(users.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateJsonContext("{ \"username\": \"carol\", \"password\": \"password123\", \"createDemoData\": true }")
            }
        };

        await controller.Register(CancellationToken.None);

        users.Verify(service => service.RegisterAsync("carol", "password123", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DefaultHttpContext CreateFormContext(string formBody)
    {
        return new DefaultHttpContext
        {
            Request =
            {
                ContentType = "application/x-www-form-urlencoded",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(formBody))
            }
        };
    }

    private static DefaultHttpContext CreateJsonContext(string jsonBody)
    {
        return new DefaultHttpContext
        {
            Request =
            {
                ContentType = "application/json",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))
            }
        };
    }
}
