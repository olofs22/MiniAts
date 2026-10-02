using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Controllers;
using MiniAts.Api.Dtos;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Tests;

public class SignupRequestsControllerTests
{
    private static CreateSignupRequest Valid(string? website = null, string? message = "We hire 20 people a year.") =>
        new("  Acme AB ", " Anna Svensson ", " anna@acme.se ", message, website);

    [Fact]
    public async Task Create_Valid_StoresTrimmedRequest()
    {
        using var db = TestDb.CreateContext();

        var result = await new SignupRequestsController(db).Create(Valid());

        Assert.IsType<AcceptedResult>(result);
        var stored = await db.SignupRequests.SingleAsync();
        Assert.Equal("Acme AB", stored.CompanyName);
        Assert.Equal("Anna Svensson", stored.ContactName);
        Assert.Equal("anna@acme.se", stored.Email);
        Assert.Equal("We hire 20 people a year.", stored.Message);
    }

    [Fact]
    public async Task Create_WithHoneypotFilled_PretendsSuccessButStoresNothing()
    {
        using var db = TestDb.CreateContext();

        var result = await new SignupRequestsController(db).Create(Valid(website: "http://spam.example"));

        Assert.IsType<AcceptedResult>(result);
        Assert.False(await db.SignupRequests.AnyAsync());
    }

    [Theory]
    [InlineData(null, "Anna", "anna@acme.se")]
    [InlineData("  ", "Anna", "anna@acme.se")]
    [InlineData("Acme", null, "anna@acme.se")]
    [InlineData("Acme", "Anna", null)]
    [InlineData("Acme", "Anna", "not-an-email")]
    [InlineData("Acme", "Anna", "Anna <anna@acme.se>")]
    public async Task Create_MissingOrInvalidFields_ReturnsBadRequest(string? company, string? contact, string? email)
    {
        using var db = TestDb.CreateContext();

        var result = await new SignupRequestsController(db).Create(new CreateSignupRequest(company, contact, email, null, null));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(await db.SignupRequests.AnyAsync());
    }

    [Fact]
    public async Task Create_MessageTooLong_ReturnsBadRequest()
    {
        using var db = TestDb.CreateContext();

        var result = await new SignupRequestsController(db).Create(Valid(message: new string('x', 2001)));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AdminGetAll_ReturnsNewestFirst()
    {
        using var db = TestDb.CreateContext();
        db.SignupRequests.AddRange(
            new SignupRequest { CompanyName = "Old", ContactName = "A", Email = "a@a.se", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new SignupRequest { CompanyName = "New", ContactName = "B", Email = "b@b.se", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var result = await new AdminSignupRequestsController(db).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var requests = Assert.IsAssignableFrom<IEnumerable<SignupRequestResponse>>(ok.Value).ToList();
        Assert.Equal(["New", "Old"], requests.Select(r => r.CompanyName));
    }

    [Fact]
    public async Task AdminDelete_RemovesRequest_AndReturnsNotFoundForUnknown()
    {
        using var db = TestDb.CreateContext();
        var request = new SignupRequest { CompanyName = "Acme", ContactName = "A", Email = "a@a.se" };
        db.SignupRequests.Add(request);
        await db.SaveChangesAsync();
        var controller = new AdminSignupRequestsController(db);

        Assert.IsType<NoContentResult>(await controller.Delete(request.Id));
        Assert.False(await db.SignupRequests.AnyAsync());
        Assert.IsType<NotFoundResult>(await controller.Delete(request.Id));
    }
}
