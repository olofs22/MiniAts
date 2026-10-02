using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniAts.Api.Auth;
using MiniAts.Api.Data;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Tests;

public static class TestUsers
{
    public static ClaimsPrincipal Customer(Guid orgId) => Principal(nameof(ProfileRole.Customer), orgId);

    public static ClaimsPrincipal Admin(Guid? orgId = null) => Principal(nameof(ProfileRole.Admin), orgId);

    public static ClaimsPrincipal Principal(string? role, Guid? orgId, string? sub = null, string? email = null)
    {
        var claims = new List<Claim>();
        if (role is not null) claims.Add(new Claim(AppClaimTypes.AtsRole, role));
        if (orgId is not null) claims.Add(new Claim(AppClaimTypes.OrgId, orgId.Value.ToString()));
        if (sub is not null) claims.Add(new Claim("sub", sub));
        if (email is not null) claims.Add(new Claim("email", email));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    public static T As<T>(this T controller, ClaimsPrincipal user) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        return controller;
    }

    public static async Task<(Organization Own, Organization Other)> SeedTwoOrgs(MiniAtsDbContext db)
    {
        var own = new Organization { Name = "Own Org" };
        var other = new Organization { Name = "Other Org" };
        db.Organizations.AddRange(own, other);
        await db.SaveChangesAsync();
        return (own, other);
    }
}
