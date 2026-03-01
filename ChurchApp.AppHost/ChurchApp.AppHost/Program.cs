using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgresUser = builder.AddParameter("postgres-user", "churchapp", publishValueAsDefault: true, secret: false);
var postgresPassword = builder.AddParameter("postgres-password", "churchapp", publishValueAsDefault: false, secret: true);

var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
    .WithImage("postgres", "16")
    .WithDataVolume("churchapp-postgres-data");

var churchAppDatabase = postgres.AddDatabase("churchapp");

var api = builder.AddProject<ChurchApp_API>("api")
    .WithReference(churchAppDatabase)
    .WithHttpsEndpoint(name: "https");

builder.AddExecutable("web", "npm", "../../ChurchApp.Web", "run", "dev", "--", "--host", "0.0.0.0", "--port", "5173")
    .WithHttpsEndpoint(name: "https", targetPort: 5173)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
    .WithEnvironment("PATH", $"{Environment.GetEnvironmentVariable("PATH")}:/opt/homebrew/opt/node@22/bin");

builder.Build().Run();
