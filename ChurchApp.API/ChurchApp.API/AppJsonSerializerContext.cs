using System.Text.Json.Serialization;
using ChurchApp.API.Endpoints;

namespace ChurchApp.API;

/// <summary>
/// JSON serializer context for AOT compilation.
/// Add your DTOs and response types here for source generation.
/// </summary>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(HealthResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext;
