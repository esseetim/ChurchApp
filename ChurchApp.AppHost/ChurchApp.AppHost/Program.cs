using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres, 16")
    .WithDataVolume("churchapp-postgres-data")
    .WithEnvironment("POSTGRES_USER", "churchapp")
    .WithEnvironment("POSTGRES_PASSWORD", "churchapp");

var churchAppDatabase = postgres.AddDatabase("churchapp");

builder.AddProject<ChurchApp_API>("api")
    .WithReference(churchAppDatabase);

builder.Build().Run();
