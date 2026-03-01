using FastEndpoints;
using FastEndpoints.Swagger;
using ChurchApp.Application;

namespace ChurchApp.API;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        
        // Configure JSON serialization for AOT
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });
        
        // Add Application services
        builder.Services.AddChurchAppServices(builder.Configuration);
        
        // Add FastEndpoints
        builder.Services.AddFastEndpoints();
        
        // Add Swagger for development
        builder.Services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "ChurchApp API";
                s.Version = "v1";
            };
        });
        
        var app = builder.Build();
        
        // Configure middleware pipeline
        app.UseFastEndpoints();
        
        // Enable Swagger in development
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerGen();
        }
        
        await app.RunAsync();
    }
}
