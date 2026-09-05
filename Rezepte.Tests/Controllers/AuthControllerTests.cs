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
        users.Setup(service => service.RegisterAsync("admin", "password123", It.IsAny<CancellationToken>()))
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
}
