using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureOpenAI;

/// <summary>
/// Defines the settings for configuring Azure OpenAI.
/// </summary>
/// <param name="openAIEndpoint">The endpoint URL for the Azure OpenAI service.</param>
/// <param name="embeddingModelDeployment">The deployment of an embedding model.</param>
/// <param name="completionModelDeployment">The deployment of a completion model.</param>
/// <param name="visionCompletionModelDeployment">The deployment of a vision completion model.</param>
public class OpenAISettings(
    string openAIEndpoint,
    string? embeddingModelDeployment = null,
    string? completionModelDeployment = null,
    string? visionCompletionModelDeployment = null)
{
    /// <summary>
    /// The configuration key for the Azure OpenAI endpoint URL.
    /// </summary>
    public const string OpenAIEndpointConfigKey = "OPENAI_ENDPOINT";

    /// <summary>
    /// The configuration key for the deployment of an embedding model.
    /// </summary>
    public const string EmbeddingModelDeploymentConfigKey = "OPENAI_EMBEDDING_MODEL_DEPLOYMENT";

    /// <summary>
    /// The configuration key for the deployment of a completion model.
    /// </summary>
    public const string CompletionModelDeploymentConfigKey = "OPENAI_COMPLETION_MODEL_DEPLOYMENT";

    /// <summary>
    /// The configuration key for the deployment of a vision completion model.
    /// </summary>
    public const string VisionCompletionModelDeploymentConfigKey = "OPENAI_VISION_COMPLETION_MODEL_DEPLOYMENT";

    /// <summary>
    /// Gets the endpoint URL for the Azure OpenAI service.
    /// </summary>
    public string OpenAIEndpoint { get; init; } = openAIEndpoint;

    /// <summary>
    /// Gets the name of the deployment for an embedding model, e.g., text-embedding-ada-002.
    /// </summary>
    public string? EmbeddingModelDeployment { get; init; } = embeddingModelDeployment;

    /// <summary>
    /// Gets the name of the deployment for a completion model, e.g., gpt-35-turbo.
    /// </summary>
    public string? CompletionModelDeployment { get; init; } = completionModelDeployment;

    /// <summary>
    /// Gets the name of the deployment for a vision completion model, e.g., gpt-4-vision-preview.
    /// </summary>
    public string? VisionCompletionModelDeployment { get; init; } = visionCompletionModelDeployment;

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

        return new OpenAISettings(
            openAIEndpoint,
            configuration[EmbeddingModelDeploymentConfigKey],
            configuration[CompletionModelDeploymentConfigKey],
            configuration[VisionCompletionModelDeploymentConfigKey]);
    }
}
