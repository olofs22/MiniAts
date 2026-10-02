using Microsoft.AspNetCore.Mvc;
using MiniAts.Api.Controllers;
using MiniAts.Api.Dtos;

namespace MiniAts.Api.Tests;

public class MeControllerTests
{
    [Fact]
    public async Task Get_Customer_ReturnsProfileWithOrgName()
    {
        using var db = TestDb.CreateContext();
        var (own, _) = await TestUsers.SeedTwoOrgs(db);
        var userId = Guid.NewGuid();
        var controller = new MeController(db)
            .As(TestUsers.Principal("Customer", own.Id, userId.ToString(), "jane@example.com"));

        var result = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var me = Assert.IsType<MeResponse>(ok.Value);
        Assert.Equal(userId, me.UserId);
        Assert.Equal("jane@example.com", me.Email);
        Assert.Equal("Customer", me.Role);
        Assert.Equal(own.Id, me.OrgId);
        Assert.Equal("Own Org", me.OrgName);
    }

    [Fact]
    public async Task Get_AdminWithoutOrg_ReturnsNullOrg()
    {
        using var db = TestDb.CreateContext();
        var controller = new MeController(db)
            .As(TestUsers.Principal("Admin", null, Guid.NewGuid().ToString(), "admin@example.com"));

        var result = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var me = Assert.IsType<MeResponse>(ok.Value);
        Assert.Null(me.OrgId);
        Assert.Null(me.OrgName);
    }

    [Fact]
    public async Task Get_WithoutRoleClaim_ReturnsForbid()
    {
        using var db = TestDb.CreateContext();
        var controller = new MeController(db)
            .As(TestUsers.Principal(null, null, Guid.NewGuid().ToString(), "nobody@example.com"));

        var result = await controller.Get();

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Get_WithOrgClaimForDeletedOrg_ReturnsForbid()
    {
        using var db = TestDb.CreateContext();
        var controller = new MeController(db)
            .As(TestUsers.Principal("Customer", Guid.NewGuid(), Guid.NewGuid().ToString(), "jane@example.com"));

        var result = await controller.Get();

        Assert.IsType<ForbidResult>(result.Result);
    }
}
