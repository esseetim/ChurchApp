using FastEndpoints;

namespace ChurchApp.API.Endpoints;

/// <summary>
/// Health check endpoint to verify the API is running.
/// </summary>
public class HealthCheckEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Health Check";
            s.Description = "Returns the health status of the API";
            s.Response<HealthResponse>(200, "API is healthy");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendAsync(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        }, cancellation: ct);
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
}

