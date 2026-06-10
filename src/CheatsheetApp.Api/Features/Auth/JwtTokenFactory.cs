using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CheatsheetApp.Api.Features.Auth;

public static class JwtTokenFactory
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    public const string Issuer = "cheatsheet-api";
    public const string Audience = "cheatsheet-app";

    public static string Create(string username, string signingKey)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, username)],
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return TokenHandler.WriteToken(token);
    }
}
