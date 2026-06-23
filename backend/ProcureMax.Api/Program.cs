using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using ProcureMax.Core;
using ProcureMax.Core.Auth;
using ProcureMax.Core.Authorization;
using ProcureMax.Core.Middleware;
using ProcureMax.Features.Auth;
using ProcureMax.Features.CostCenters;
using ProcureMax.Features.GlAccounts;
using ProcureMax.Features.Items;
using ProcureMax.Features.Roles;
using ProcureMax.Features.Suppliers;
using ProcureMax.Features.Units;
using ProcureMax.Features.Users;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration & options ---
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<ProcurementOptions>(builder.Configuration.GetSection("Procurement"));

// --- Logging & infra ---
builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();

// --- Global error handling (RFC 9457 Problem Details) ---
// Order: register an IExceptionHandler that maps domain exceptions to a ProblemDetails,
// then AddProblemDetails() so framework-emitted 4xx (authn/authz/not-found) share the same shape.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(opts =>
{
    opts.CustomizeProblemDetails = ctx =>
    {
        // Always expose a stable trace id so callers can correlate with server logs.
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

        // Never leak internal stacktrace/Detail text for 5xx outside Development.
        var isDev = builder.Environment.IsDevelopment();
        if (!isDev && ctx.ProblemDetails.Status is >= 500 and <= 599)
        {
            ctx.ProblemDetails.Detail = "An unexpected error occurred.";
        }
    };
});

// --- Database (Dapper + SQLite) ---
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=procuremax.db";
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqliteConnectionFactory(connectionString));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddScoped<Seeder>();

// --- Auth services ---
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Authorization services ---
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// --- Slice services ---
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ICostCenterService, CostCenterService>();
builder.Services.AddScoped<IGlAccountService, GlAccountService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IItemService, ItemService>();
// Repositories (introduced by the master data slices — Phase 1 services held the factory directly).
builder.Services.AddScoped<ICostCenterRepository, CostCenterRepository>();
builder.Services.AddScoped<IGlAccountRepository, GlAccountRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// --- JWT Bearer auth ---
var authOpts = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
if (string.IsNullOrWhiteSpace(authOpts.SigningSecret) || authOpts.SigningSecret.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        authOpts.SigningSecret = "DEV-ONLY-CHANGE-ME-32-char-minimum-secret-key-zzzz";
    }
    else
    {
        throw new InvalidOperationException("Auth:SigningSecret must be set (>= 32 chars) in production.");
    }
}
builder.Services.AddSingleton(authOpts);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOpts.Issuer,
            ValidAudience = authOpts.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOpts.SigningSecret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// --- CORS ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// --- OpenAPI ---
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Bootstrap DB ---
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var init = sp.GetRequiredService<DatabaseInitializer>();
    init.Initialize();
    var seeder = sp.GetRequiredService<Seeder>();
    seeder.Seed();
}

// --- Middleware pipeline ---
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

// --- Health ---
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }))
   .AllowAnonymous().WithTags("Meta");

// --- Slice routing ---
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapRoleEndpoints();
app.MapCostCenterEndpoints();
app.MapGlAccountEndpoints();
app.MapUnitEndpoints();
app.MapSupplierEndpoints();
app.MapItemEndpoints();

app.Run();

