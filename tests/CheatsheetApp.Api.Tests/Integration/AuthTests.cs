using System.Net;
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class AuthTests(ApiFixture fixture)
{
    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new { username = ApiFixture.AdminUsername, password = ApiFixture.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>();
        Assert.True(body!.Success);
        Assert.False(string.IsNullOrWhiteSpace(body.Data!.Token));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new { username = ApiFixture.AdminUsername, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
    }

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record LoginPayload(string Token);
}
