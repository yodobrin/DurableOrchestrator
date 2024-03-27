using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureOpenAI;

/// <summary>
/// Defines a model that represents an embedding request to the OpenAI service.
/// </summary>
public class OpenAIEmbeddingRequest : OpenAIRequest
{
    /// <summary>
    /// Gets or sets the text to embed.
    /// </summary>
    [JsonPropertyName("textToEmbed")]
    public string? TextToEmbed { get; set; } = string.Empty;

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();

        if (string.IsNullOrWhiteSpace(TextToEmbed))
        {
            result.AddErrorMessage($"{nameof(TextToEmbed)} is missing.");
        }

        return result;
    }
}
