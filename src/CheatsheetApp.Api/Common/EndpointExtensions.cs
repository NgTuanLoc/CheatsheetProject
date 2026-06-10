namespace CheatsheetApp.Api.Common;

public static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        // All endpoints require auth by default; opt out with .AllowAnonymous().
        var group = app.MapGroup("").RequireAuthorization();

        var endpoints = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEndpoint)) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IEndpoint)Activator.CreateInstance(t)!);

        foreach (var endpoint in endpoints)
            endpoint.Map(group);
    }
}
