using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureOpenAI;

/// <summary>
/// Defines the settings for configuring Azure OpenAI.
/// </summary>
/// <param name="openAIEndpoint">The endpoint URL for the Azure OpenAI service.</param>
public class OpenAISettings(string openAIEndpoint)
{
    /// <summary>
    /// The configuration key for the Azure OpenAI endpoint URL.
    /// </summary>
    public const string OpenAIEndpointConfigKey = "OPENAI_ENDPOINT";

    /// <summary>
    /// Gets the endpoint URL for the Azure OpenAI service.
    /// </summary>
    public string OpenAIEndpoint { get; init; } = openAIEndpoint;

    /// <summary>
    /// Creates a new instance of the <see cref="OpenAISettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="OpenAISettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required configuration is not present.</exception>
    public static OpenAISettings FromConfiguration(IConfiguration configuration)
    {
        var openAIEndpoint = configuration[OpenAIEndpointConfigKey] ??
                             throw new InvalidOperationException($"{OpenAIEndpointConfigKey} is not configured.");

        return new OpenAISettings(openAIEndpoint);
    }
}
