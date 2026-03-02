using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using ChurchApp.Web.Blazor;
using ChurchApp.Web.Blazor.Services;
using ChurchApp.Web.Blazor.Services.Implementations;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base URL from environment or default to dev
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5121";

// Register IHttpClientFactory with named client and resilience policies
// Following Jez Humble''s reliability principles and Microsoft''s modern HttpClient patterns
builder.Services.AddHttpClient("ChurchAppApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "ChurchApp-Blazor/1.0");
})
.AddStandardResilienceHandler(options =>
{
    // Retry policy: Exponential backoff with jitter (Kent Beck''s test-driven reliability)
    options.Retry = new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    };
    
    // Circuit breaker: Prevent cascading failures (Uncle Bob''s fail-fast principle)
    options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
    {
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 5,
        FailureRatio = 0.5,
        BreakDuration = TimeSpan.FromSeconds(15)
    };
    
    // Timeout policy: Aggressive timeouts for better UX
    options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
});

// Register API services with Interface Segregation principle (Uncle Bob''s SOLID)
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IFamilyService, FamilyService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Register Radzen services (includes DialogService, NotificationService, etc.)
builder.Services.AddRadzenComponents();

await builder.Build().RunAsync();
