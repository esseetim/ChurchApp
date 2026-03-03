using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChurchApp.Web.Blazor.Serialization;

/// <summary>
/// JSON serialization context for Blazor JS interop.
/// Required when trimming/AOT is enabled to avoid reflection.
/// Anders Hejlsberg: "AOT compilation requires explicit metadata."
/// </summary>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization
)]
public partial class BlazorJSInteropContext : JsonSerializerContext;