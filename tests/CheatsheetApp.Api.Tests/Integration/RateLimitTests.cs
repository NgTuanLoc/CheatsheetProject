using System.Net;
using System.Net.Http.Json;

namespace CheatsheetApp.Api.Tests.Integration;

public class RateLimitTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Sixth_login_attempt_within_a_minute_returns_429()
    {
        var client = fixture.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/login",
                new { username = "admin", password = "wrong" });
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }
}
