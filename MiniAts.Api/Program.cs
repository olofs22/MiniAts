using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using MiniAts.Api.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Auth;
using MiniAts.Api.CvAnalysis;
using MiniAts.Api.Data;
using MiniAts.Api.Entities;
using MiniAts.Api.Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Falls back to App:FrontendUrl (already required for invite emails) so a deploy that only
// sets the frontend URL doesn't silently end up with an empty CORS allow-list.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null or { Length: 0 })
{
    var frontendUrl = builder.Configuration["App:FrontendUrl"];
    allowedOrigins = string.IsNullOrWhiteSpace(frontendUrl) ? [] : [frontendUrl];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddDbContext<MiniAtsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IClaimsTransformation, ProfileClaimsTransformation>();
builder.Services.AddScoped<IOrgAccessService, OrgAccessService>();
builder.Services.AddScoped<ICvAnalyzer, ClaudeCvAnalyzer>();

// App Service's front end appends the real client IP as the last X-Forwarded-For entry;
// trusting only that hop keeps a client from spoofing its IP for the rate limiter.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(SignupRequestsController.RateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));
});

builder.Services.AddExceptionHandler<OrgAccessExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpClient<ISupabaseAdminAuthClient, SupabaseAdminAuthClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var supabaseUrl = config["Supabase:Url"]
        ?? throw new InvalidOperationException("Supabase:Url is not configured.");
    var serviceRoleKey = config["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is not configured.");

    client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/auth/v1/");
    client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Supabase:Authority"];
        options.Audience = builder.Configuration["Supabase:Audience"];
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidIssuer = builder.Configuration["Supabase:Authority"];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(AppClaimTypes.AtsRole, nameof(ProfileRole.Admin)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
