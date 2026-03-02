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

var web = builder.AddProject<Projects.ChurchApp_Web_Blazor>("web")
    .WithHttpsEndpoint(name: "https")
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint("https"));

builder.Build().Run();
