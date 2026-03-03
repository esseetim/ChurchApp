using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Projects;

const string apiProjectName = "api";
const string webProjectName = "web";
const string appName = "churchapp";
const string appHostPostgresContainerName = "churchapp-apphost-postgres";
const string apiEndpointName = "api-http";
const string webEndpointName = "web-http";

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL parameters (Jez Humble's externalized configuration)
var postgresUser = builder.AddParameter("postgres-user", appName, publishValueAsDefault: true, secret: false);
var postgresPassword = builder.AddParameter("postgres-password", appName, publishValueAsDefault: false, secret: true);

// PostgreSQL database (production-ready with data volume)
var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
    .WithImage("postgres", "16")
    .WithDataVolume($"{appName}-postgres-data")
    .WithContainerName(appHostPostgresContainerName)
    .WithLifetime(ContainerLifetime.Persistent);

var churchAppDatabase = postgres.AddDatabase(appName);

// API service (HTTP for development, HTTPS in production via reverse proxy)
var api = builder.AddProject<ChurchApp_API>(apiProjectName)
    .WithReference(churchAppDatabase)
    .WithHttpEndpoint(port: 5121, name: apiEndpointName)
    .WaitFor(churchAppDatabase); // Unique name

// Blazor WebAssembly frontend
builder.AddProject<ChurchApp_Web_Blazor>(webProjectName)
    .WithHttpEndpoint(name: webEndpointName) // Unique name
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint(apiEndpointName))
    .WaitFor(api);

await builder.Build().RunAsync();
