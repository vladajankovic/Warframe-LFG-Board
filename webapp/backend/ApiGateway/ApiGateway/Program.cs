using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token)
                    && context.Request.Path.StartsWithSegments("/hubs/user-notifications"))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Gateway", context => CreateFixedWindowPartition(
        GetClientPartition(context),
        builder.Configuration.GetValue("Gateway:RateLimiting:Api:PermitLimit", 120),
        builder.Configuration.GetValue("Gateway:RateLimiting:Api:WindowSeconds", 60)));
    options.AddPolicy("Authentication", context => CreateFixedWindowPartition(
        GetClientPartition(context),
        builder.Configuration.GetValue("Gateway:RateLimiting:Authentication:PermitLimit", 10),
        builder.Configuration.GetValue("Gateway:RateLimiting:Authentication:WindowSeconds", 60)));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/429",
            title = "Too many requests",
            status = StatusCodes.Status429TooManyRequests,
            detail = "The gateway rate limit has been exceeded. Retry after the indicated delay."
        }, cancellationToken);
    };
});

builder.Services.Configure<ApostropheTransformationOptions>(
    builder.Configuration.GetSection("Gateway:ApostropheTransformation"));
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/502",
            title = "Gateway request failed",
            status = StatusCodes.Status502BadGateway,
            detail = app.Environment.IsDevelopment() ? error?.Message : "The downstream service is unavailable."
        });
    });
});

app.UseWebSockets();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<ApostropheTransformationMiddleware>();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapReverseProxy();

app.Run();

static RateLimitPartition<string> CreateFixedWindowPartition(string partitionKey, int permitLimit, int windowSeconds)
{
    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
    {
        AutoReplenishment = true,
        PermitLimit = Math.Max(1, permitLimit),
        QueueLimit = 0,
        Window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds))
    });
}

static string GetClientPartition(HttpContext context)
{
    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    return !string.IsNullOrWhiteSpace(userId)
        ? $"user:{userId}"
        : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

public partial class Program
{
}
