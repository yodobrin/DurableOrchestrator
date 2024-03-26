using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AzureOpenAI;

/// <summary>
/// Defines a set of extension methods for configuring Azure OpenAI services.
/// </summary>
public static class OpenAIExtensions
{
    /// <summary>
    /// Configures the Azure OpenAI services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure OpenAI services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure OpenAI settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddOpenAI(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = OpenAISettings.FromConfiguration(configuration);
        services.AddScoped(_ => settings);

        services.AddScoped(sp =>
        {
            // The role of either the user or spn needs to have 'Cognitive Services OpenAI User' role to access the OpenAI service.
            // Where key-based authorization is preferred, the following code can be used:

            // var key = configuration["OPENAI_KEY"];
            // AzureKeyCredential credentials = new AzureKeyCredential(key);

            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            return new OpenAIClient(new Uri(settings.OpenAIEndpoint), credentials);
        });

        return services;
    }
}
