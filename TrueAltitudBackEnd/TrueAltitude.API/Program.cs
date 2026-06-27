using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using TrueAltitude.API.Security;
using TrueAltitude.Application.Services;
using TrueAltitude.Infrastructure.Interfaces;
using TrueAltitude.Infrastructure.Repositories;
using TrueAltitude.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
var jwtIssuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
var jwtAudience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");
var jwtAccessTokenExpirationMinutes = jwtSettings.GetValue<int?>("ExpirationMinutes") ?? 1440;
var requestTimeSharedKey = builder.Configuration["SecurityHeader:SharedKey"] ?? throw new InvalidOperationException("SecurityHeader SharedKey is not configured.");
var requestTimeMaxAgeSeconds = builder.Configuration.GetValue<int?>("SecurityHeader:MaxAgeSeconds") ?? 20;
var isDevelopmentEnvironment = builder.Environment.IsDevelopment();
var relaxJwtValidation = isDevelopmentEnvironment && builder.Configuration.GetValue<bool>("Authentication:RelaxJwtValidationInDevelopment");
var enforceRequestTimeHeader = builder.Configuration.GetValue<bool?>("SecurityHeader:EnforceRequestTimeHeader") ?? !isDevelopmentEnvironment;
var mysqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("MySQL connection string is not configured.");
var dbCommandTimeoutSeconds = builder.Configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 180;

// ===== Add Services =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi();

// ===== Database Context =====
builder.Services.AddDbContext<TrueAltitudeDbContext>(options =>
    options.UseMySql(
        mysqlConnectionString,
        ServerVersion.AutoDetect(mysqlConnectionString),
        mySqlOptions =>
        {
            mySqlOptions.CommandTimeout(dbCommandTimeoutSeconds);
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(2),
                errorNumbersToAdd: new[] { 1205, 1213 }
            );
        })
);

// ===== Dependency Injection =====
builder.Services.AddHttpClient();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<ISubscriptionPurchaseRepository, SubscriptionPurchaseRepository>();
builder.Services.AddScoped<ILearningRepository, LearningRepository>();
builder.Services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtTokenService>(_ => new JwtTokenService(jwtSecret, jwtIssuer, jwtAudience));
builder.Services.AddScoped<IAuthService>(provider =>
    new AuthService(
        provider.GetRequiredService<IUserRepository>(),
        provider.GetRequiredService<IGoogleOAuthService>(),
        provider.GetRequiredService<IEmailService>(),
        provider.GetRequiredService<IJwtTokenService>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<ILogger<AuthService>>()
    )
);
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ILearningService, LearningService>();
builder.Services.AddScoped<IAdminService>(provider =>
    new AdminService(
        provider.GetRequiredService<IUserRepository>(),
        provider.GetRequiredService<ISubscriptionPurchaseRepository>(),
        provider.GetRequiredService<ILearningRepository>(),
        provider.GetRequiredService<ILogger<AdminService>>()
    )
);

// ===== Authentication (JWT) =====
var key = Encoding.ASCII.GetBytes(jwtSecret);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.IncludeErrorDetails = isDevelopmentEnvironment;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = !relaxJwtValidation,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = !relaxJwtValidation,
        ValidIssuer = jwtIssuer,
        ValidateAudience = !relaxJwtValidation,
        ValidAudience = jwtAudience,
        ValidateLifetime = !relaxJwtValidation,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            var hasBearer = !string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

            if (!hasBearer)
            {
                return Task.CompletedTask;
            }

            var requestTimeHeader = context.Request.Headers[RequestTimeHeaderValidator.HeaderName].ToString();
            var isValid = RequestTimeHeaderValidator.IsValid(requestTimeHeader, requestTimeSharedKey, requestTimeMaxAgeSeconds);

            if (enforceRequestTimeHeader && !isValid)
            {
                // Ignore incoming bearer token when request-time header is missing/invalid/expired.
                context.NoResult();
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            DateTime validFromUtc;
            if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
            {
                validFromUtc = jwtToken.ValidFrom.ToUniversalTime();
            }
            else if (context.SecurityToken is JsonWebToken jsonWebToken)
            {
                validFromUtc = jsonWebToken.ValidFrom.ToUniversalTime();
            }
            else
            {
                context.Fail("Unsupported security token format.");
                return;
            }

            var tokenAge = DateTime.UtcNow - validFromUtc;
            if (tokenAge > TimeSpan.FromMinutes(jwtAccessTokenExpirationMinutes))
            {
                context.Fail("Access token is older than the allowed lifetime.");
                return;
            }

            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Fail("Invalid token subject.");
                return;
            }

            var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                context.Fail("User account is inactive.");
            }
        }
    };
});

// ===== CORS =====
builder.Services.AddCors(options =>
{
    static bool IsAllowedFrontendOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        return string.Equals(origin, "http://localhost:4200", StringComparison.OrdinalIgnoreCase)
            || string.Equals(origin, "https://www.truealtitude.in", StringComparison.OrdinalIgnoreCase)
            || string.Equals(origin, "https://truealtitude.in", StringComparison.OrdinalIgnoreCase)
            || origin.EndsWith(".azurestaticapps.net", StringComparison.OrdinalIgnoreCase);
    }

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(IsAllowedFrontendOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ===== API Versioning & Documentation =====
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");

            if (exceptionFeature?.Error != null)
            {
                logger.LogError(exceptionFeature.Error, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            var origin = context.Request.Headers.Origin.ToString();
            if (!string.IsNullOrWhiteSpace(origin) &&
                (string.Equals(origin, "http://localhost:4200", StringComparison.OrdinalIgnoreCase)
                || string.Equals(origin, "https://www.truealtitude.in", StringComparison.OrdinalIgnoreCase)
                || string.Equals(origin, "https://truealtitude.in", StringComparison.OrdinalIgnoreCase)
                || origin.EndsWith(".azurestaticapps.net", StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.Headers.AccessControlAllowOrigin = origin;
                context.Response.Headers.Vary = "Origin";
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                success = false,
                message = "An unexpected server error occurred.",
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        });
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
