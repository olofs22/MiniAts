using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MiniAts.Api.Supabase;

public interface ISupabaseAdminAuthClient
{
    Task<SupabaseUserResult> InviteUserByEmailAsync(string email, string? redirectTo = null, CancellationToken ct = default);
    Task<SupabaseUserResult?> FindUserByEmailAsync(string email, CancellationToken ct = default);
    Task<SupabaseUserResult?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task BanUserAsync(Guid userId, CancellationToken ct = default);
}

public record SupabaseUserResult(Guid UserId, string Email);

public class SupabaseAdminApiException(HttpStatusCode statusCode, string responseBody)
    : Exception($"Supabase Admin Auth API returned {(int)statusCode}: {responseBody}");

public class SupabaseUserAlreadyExistsException(string email)
    : Exception($"A Supabase Auth user already exists for '{email}'.");

// GoTrue Admin Auth REST API, called with the service role key (server-side only,
// per CLAUDE.md). Endpoint shapes confirmed against supabase/auth's openapi.yaml
// (POST /invite, GET /admin/users) - re-check if Supabase changes this contract.
public class SupabaseAdminAuthClient(HttpClient httpClient) : ISupabaseAdminAuthClient
{
    public async Task<SupabaseUserResult> InviteUserByEmailAsync(string email, string? redirectTo = null, CancellationToken ct = default)
    {
        // Without redirect_to, GoTrue falls back to the project's dashboard-configured
        // Site URL (often left at the default http://localhost:3000), which breaks the
        // invite link for anyone not running a server on that port.
        var requestUri = redirectTo is null
            ? "invite"
            : $"invite?redirect_to={Uri.EscapeDataString(redirectTo)}";

        var response = await httpClient.PostAsJsonAsync(requestUri, new InviteUserRequest(email), ct);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            throw new SupabaseUserAlreadyExistsException(email);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new SupabaseAdminApiException(response.StatusCode, body);
        }

        var user = await response.Content.ReadFromJsonAsync<GoTrueUser>(ct)
            ?? throw new SupabaseAdminApiException(response.StatusCode, "Empty response body.");

        return new SupabaseUserResult(user.Id, user.Email);
    }

    // GoTrue's GET /admin/users has no server-side email filter, only pagination.
    // Fine at MVP scale (called only from the duplicate-recovery path); revisit if
    // the user base grows large enough for this to be slow.
    public async Task<SupabaseUserResult?> FindUserByEmailAsync(string email, CancellationToken ct = default)
    {
        const int perPage = 200;
        const int maxPages = 25;

        for (var page = 1; page <= maxPages; page++)
        {
            var response = await httpClient.GetAsync($"admin/users?page={page}&per_page={perPage}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new SupabaseAdminApiException(response.StatusCode, body);
            }

            var page_ = await response.Content.ReadFromJsonAsync<GoTrueUserListResponse>(ct)
                ?? throw new SupabaseAdminApiException(response.StatusCode, "Empty response body.");

            var match = page_.Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return new SupabaseUserResult(match.Id, match.Email);
            }

            if (page_.Users.Count < perPage)
            {
                break;
            }
        }

        return null;
    }

    public async Task<SupabaseUserResult?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"admin/users/{userId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new SupabaseAdminApiException(response.StatusCode, body);
        }

        var user = await response.Content.ReadFromJsonAsync<GoTrueUser>(ct)
            ?? throw new SupabaseAdminApiException(response.StatusCode, "Empty response body.");

        return new SupabaseUserResult(user.Id, user.Email);
    }

    // GoTrue has no "disable" flag; a ~100-year ban_duration is the supported way to
    // block sign-in while keeping the auth user around.
    public async Task BanUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"admin/users/{userId}", new BanUserRequest("876600h"), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new SupabaseAdminApiException(response.StatusCode, body);
        }
    }

    private record InviteUserRequest(string Email);

    private record BanUserRequest([property: JsonPropertyName("ban_duration")] string BanDuration);

    private record GoTrueUser(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("email")] string Email);

    private record GoTrueUserListResponse(
        [property: JsonPropertyName("users")] List<GoTrueUser> Users);
}
