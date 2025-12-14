using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using ShitpostBot.Application.Services;

namespace ShitpostBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        
        // Register Refit client for ML service
        services.AddOptions<ImageFeatureExtractorApiOptions>()
            .Bind(configuration.GetSection("ImageFeatureExtractorApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddRefitClient<IImageFeatureExtractorApi>(
                new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    PropertyNameCaseInsensitive = true
                })))
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ImageFeatureExtractorApiOptions>>().Value;
                client.BaseAddress = new Uri(options.Uri);
            });
        
        return services;
    }
}
