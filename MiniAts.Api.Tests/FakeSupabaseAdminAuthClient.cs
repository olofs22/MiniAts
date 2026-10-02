using MiniAts.Api.Supabase;

namespace MiniAts.Api.Tests;

public class FakeSupabaseAdminAuthClient : ISupabaseAdminAuthClient
{
    public Func<string, Task<SupabaseUserResult>> InviteUserByEmail { get; set; } =
        _ => throw new InvalidOperationException("InviteUserByEmail was not configured for this test.");

    public Func<string, Task<SupabaseUserResult?>> FindUserByEmail { get; set; } =
        _ => throw new InvalidOperationException("FindUserByEmail was not configured for this test.");

    public Func<Guid, Task<SupabaseUserResult?>> GetUserById { get; set; } =
        _ => throw new InvalidOperationException("GetUserById was not configured for this test.");

    public List<Guid> BannedUserIds { get; } = [];

    public Func<Guid, Task> BanUser { get; set; } = _ => Task.CompletedTask;

    public Task<SupabaseUserResult?> GetUserByIdAsync(Guid userId, CancellationToken ct = default) =>
        GetUserById(userId);

    public async Task BanUserAsync(Guid userId, CancellationToken ct = default)
    {
        await BanUser(userId);
        BannedUserIds.Add(userId);
    }

    public Task<SupabaseUserResult> InviteUserByEmailAsync(string email, string? redirectTo = null, CancellationToken ct = default) =>
        InviteUserByEmail(email);

    public Task<SupabaseUserResult?> FindUserByEmailAsync(string email, CancellationToken ct = default) =>
        FindUserByEmail(email);
}
