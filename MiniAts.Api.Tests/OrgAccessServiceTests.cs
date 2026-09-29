using System.Security.Claims;
using MiniAts.Api.Auth;

namespace MiniAts.Api.Tests;

public class OrgAccessServiceTests
{
    private static readonly Guid OwnOrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();

    private static ClaimsPrincipal CreateUser(string role, Guid? orgId)
    {
        var claims = new List<Claim> { new(AppClaimTypes.AtsRole, role) };
        if (orgId is not null)
        {
            claims.Add(new Claim(AppClaimTypes.OrgId, orgId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public void Customer_IgnoresRequestedOrgId_AndUsesOwnOrg()
    {
        var user = CreateUser("Customer", OwnOrgId);
        var service = new OrgAccessService();

        var result = service.ResolveOrgId(user, OtherOrgId);

        Assert.Equal(OwnOrgId, result);
    }

    [Fact]
    public void Admin_WithRequestedOrgId_UsesRequestedOrg()
    {
        var user = CreateUser("Admin", OwnOrgId);
        var service = new OrgAccessService();

        var result = service.ResolveOrgId(user, OtherOrgId);

        Assert.Equal(OtherOrgId, result);
    }

    [Fact]
    public void Admin_WithoutRequestedOrgId_DefaultsToOwnOrg()
    {
        var user = CreateUser("Admin", OwnOrgId);
        var service = new OrgAccessService();

        var result = service.ResolveOrgId(user, null);

        Assert.Equal(OwnOrgId, result);
    }

    [Fact]
    public void MissingOrgIdClaim_Throws()
    {
        var user = CreateUser("Customer", null);
        var service = new OrgAccessService();

        Assert.Throws<InvalidOperationException>(() => service.ResolveOrgId(user, null));
    }
}
