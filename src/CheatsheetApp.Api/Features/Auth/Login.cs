using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Password).NotEmpty().MaximumLength(200);
    }
}

public sealed class LoginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/login", HandleAsync)
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

    private static async Task<IResult> HandleAsync(
        LoginRequest request, AppDbContext db, IConfiguration config, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Results.Json(
                ApiResponse<object>.Fail("Invalid username or password."),
                statusCode: StatusCodes.Status401Unauthorized);

        var token = JwtTokenFactory.Create(user.Username, config["Jwt:Key"]!);
        return Results.Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse(token)));
    }
}
