using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ChurchApp.Application.Tests")]
[assembly: InternalsVisibleTo("ChurchApp.AppHost.csproj")]

namespace ChurchApp.Application.Infrastructure;

internal static class DatabaseSettings
{
    public const string ImageName = "postgres, 16";
    public const string VolumeName = "churchapp-postgres-data";
    public const string EnvironmentUser = "POSTGRES_USER";
    public const string EnvironmentPassword = "POSTGRES_PASSWORD";
    public const string DefaultUser = "churchapp";
    public const string DefaultPassword = "churchapp";
}
