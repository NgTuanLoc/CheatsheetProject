using System.Text;
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using CheatsheetApp.Api.Features.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("cheatsheets");

// Enable Npgsql OpenTelemetry instrumentation (diagnostic source for tracing database queries).
AppContext.SetSwitch("Npgsql.EnableDiagnosticSource", true);

// Fail fast if secrets are missing (validated at startup, not first use).
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = JwtTokenFactory.Issuer,
        ValidAudience = JwtTokenFactory.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", window =>
    {
        window.PermitLimit = 5;
        window.Window = TimeSpan.FromMinutes(1);
        window.QueueLimit = 0;
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
        // Global default: all operations require bearer auth unless overridden.
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearer")] = []
        });
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, ct) =>
    {
        // Override global security to none for AllowAnonymous endpoints (login).
        var isAnonymous = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Any();
        if (isAnonymous)
            operation.Security = [];
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    var adminUsername = app.Configuration["Admin:Username"] ?? "admin";
    var devToken = JwtTokenFactory.Create(adminUsername, jwtKey);

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.AddPreferredSecuritySchemes("bearer")
            .AddHttpAuthentication("bearer", auth => { }));
}

await DbInitializer.InitializeAsync(app.Services);

app.Run();

public partial class Program;
