var builder = DistributedApplication.CreateBuilder(args);

var sqliteDataPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", ".local", "sqlite"));
Directory.CreateDirectory(sqliteDataPath);
var sqliteDbPath = Path.Combine(sqliteDataPath, "churchapp.db");

var sqlite = builder.AddContainer("sqlite", "nouchka/sqlite3")
    .WithBindMount(sqliteDataPath, "/data")
    .WithArgs("tail", "-f", "/dev/null");

builder.AddProject("api", "../../ChurchApp.API/ChurchApp.API/ChurchApp.API.csproj")
    .WithEnvironment("ConnectionStrings__ChurchApp", $"Data Source={sqliteDbPath}");

builder.Build().Run();
