using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AI;

/// <summary>
/// Defines the settings for configuring Azure OpenAI.
/// </summary>
/// <param name="endpointUrl">The endpoint URL for the Azure OpenAI service.</param>
/// <param name="modelDeploymentName">The default GPT model deployment name.</param>
public class OpenAISettings(string endpointUrl, string modelDeploymentName)
{
    /// <summary>
    /// The configuration key for the Azure OpenAI endpoint URL.
    /// </summary>
    public const string EndpointUrlConfigKey = "OPENAI_ENDPOINT";

    /// <summary>
    /// The configuration key for the default GPT model deployment name.
    /// </summary>
    public const string ModelDeploymentNameConfigKey = "OPENAI_MODEL_DEPLOYMENT_NAME";

    /// <summary>
    /// Gets the endpoint URL for the Azure OpenAI service.
    /// </summary>
    public string EndpointUrl { get; init; } = endpointUrl;

    /// <summary>
    /// Gets the default GPT model deployment name.
    /// </summary>
    public string ModelDeploymentName { get; init; } = modelDeploymentName;

    /// <summary>
    /// Creates a new instance of the <see cref="OpenAISettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="OpenAISettings"/> class.</returns>
    public static OpenAISettings FromConfiguration(IConfiguration configuration)
    {
        return new OpenAISettings(
            configuration[EndpointUrlConfigKey] ?? string.Empty,
            configuration[ModelDeploymentNameConfigKey] ?? string.Empty);
    }
}
