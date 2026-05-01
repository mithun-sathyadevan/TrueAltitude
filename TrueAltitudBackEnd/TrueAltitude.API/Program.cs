using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
var mysqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("MySQL connection string is not configured.");

// ===== Add Services =====
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ===== Database Context =====
builder.Services.AddDbContext<TrueAltitudeDbContext>(options =>
    options.UseMySql(mysqlConnectionString, ServerVersion.AutoDetect(mysqlConnectionString))
);

// ===== Dependency Injection =====
builder.Services.AddHttpClient();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService>(provider =>
    new AuthService(
        provider.GetRequiredService<IUserRepository>(),
        provider.GetRequiredService<IGoogleOAuthService>(),
        provider.GetRequiredService<IEmailService>(),
        provider.GetRequiredService<ILogger<AuthService>>(),
        jwtSecret,
        jwtIssuer,
        jwtAudience
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
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
