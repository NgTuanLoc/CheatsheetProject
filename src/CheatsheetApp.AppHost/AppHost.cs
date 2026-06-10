var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb();

var db = postgres.AddDatabase("cheatsheets");

var api = builder.AddProject<Projects.CheatsheetApp_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    // Dev-only values. Production values come from .env (Plan 3).
    .WithEnvironment("Admin__Username", "admin")
    .WithEnvironment("Admin__Password", "dev-password-change-me")
    .WithEnvironment("Jwt__Key", "dev-only-jwt-signing-key-0123456789abcdef0123456789abcdef")
    .WithEnvironment("Database__SeedSampleData", "true");

builder.AddNpmApp("web", "../web", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
