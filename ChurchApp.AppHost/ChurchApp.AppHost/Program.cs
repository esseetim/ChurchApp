using Projects;

const string ApiProjectName = "api";
const string WebProjectName = "web";
const string AppName = "churchapp";
const string ApiEndpointName = "api-http";
const string WebEndpointName = "web-http";

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL parameters (Jez Humble's externalized configuration)
var postgresUser = builder.AddParameter("postgres-user", AppName, publishValueAsDefault: true, secret: false);
var postgresPassword = builder.AddParameter("postgres-password", AppName, publishValueAsDefault: false, secret: true);

// PostgreSQL database (production-ready with data volume)
var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
    .WithImage("postgres", "16")
    .WithDataVolume($"{AppName}-postgres-data");

var churchAppDatabase = postgres.AddDatabase(AppName);

// API service (HTTP for development, HTTPS in production via reverse proxy)
var api = builder.AddProject<ChurchApp_API>(ApiProjectName)
    .WithReference(churchAppDatabase)
    .WithHttpEndpoint(port: 5121, name: ApiEndpointName); // Unique name

// Blazor WebAssembly frontend
var web = builder.AddProject<Projects.ChurchApp_Web_Blazor>(WebProjectName)
    .WithHttpEndpoint(name: WebEndpointName) // Unique name
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint(ApiEndpointName)); // Reference correct endpoint

builder.Build().Run();
