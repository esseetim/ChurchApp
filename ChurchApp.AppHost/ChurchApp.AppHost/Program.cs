using Projects;
using ChurchApp.Application.Infrastructure;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage(DatabaseSettings.ImageName)
    .WithDataVolume(DatabaseSettings.VolumeName)
    .WithEnvironment(DatabaseSettings.EnvironmentUser, DatabaseSettings.DefaultUser)
    .WithEnvironment(DatabaseSettings.EnvironmentPassword, DatabaseSettings.DefaultPassword);

var churchAppDatabase = postgres.AddDatabase("churchapp");

builder.AddProject("api", "../../ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj")
    .WithReference(churchAppDatabase);

builder.Build().Run();
