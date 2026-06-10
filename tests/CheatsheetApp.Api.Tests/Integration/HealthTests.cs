namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class HealthTests(ApiFixture fixture)
{
    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
