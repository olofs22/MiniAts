using MiniAts.Api.Supabase;

namespace MiniAts.Api.Tests;

public class FakeSupabaseAdminAuthClient : ISupabaseAdminAuthClient
{
    public Func<string, Task<SupabaseUserResult>> InviteUserByEmail { get; set; } =
        _ => throw new InvalidOperationException("InviteUserByEmail was not configured for this test.");

    public Func<string, Task<SupabaseUserResult?>> FindUserByEmail { get; set; } =
        _ => throw new InvalidOperationException("FindUserByEmail was not configured for this test.");

    public Task<SupabaseUserResult> InviteUserByEmailAsync(string email, CancellationToken ct = default) =>
        InviteUserByEmail(email);

    public Task<SupabaseUserResult?> FindUserByEmailAsync(string email, CancellationToken ct = default) =>
        FindUserByEmail(email);
}
