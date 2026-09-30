using Microsoft.AspNetCore.Mvc;
using MiniAts.Api.Controllers;
using MiniAts.Api.Dtos;

namespace MiniAts.Api.Tests;

public class AdminOrganizationsControllerTests
{
    [Fact]
    public async Task Create_WithValidName_ReturnsCreatedOrganization()
    {
        using var db = TestDb.CreateContext();
        var controller = new AdminOrganizationsController(db);

        var result = await controller.Create(new CreateOrganizationRequest("Acme Inc"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<OrganizationResponse>(created.Value);
        Assert.Equal("Acme Inc", response.Name);
        Assert.NotEqual(Guid.Empty, response.Id);

        var stored = await db.Organizations.FindAsync(response.Id);
        Assert.NotNull(stored);
        Assert.Equal("Acme Inc", stored!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Create_WithoutName_ReturnsBadRequest(string? name)
    {
        using var db = TestDb.CreateContext();
        var controller = new AdminOrganizationsController(db);

        var result = await controller.Create(new CreateOrganizationRequest(name!));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Name is required.", badRequest.Value);
    }
}
