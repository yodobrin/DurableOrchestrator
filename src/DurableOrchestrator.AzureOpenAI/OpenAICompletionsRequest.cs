using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureOpenAI;

/// <summary>
/// Defines a model that represents a chat completions request to the OpenAI service.
/// </summary>
public class OpenAICompletionsRequest : OpenAIRequest
{
    /// <summary>
    /// Gets or sets the chat completions options to use for the operation.
    /// </summary>
    [JsonPropertyName("chatOptions")]
    public ChatCompletionsOptions? ChatOptions { get; set; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();

        if (ChatOptions is null)
        {
            result.AddErrorMessage($"ChatCompletionsOptions is missing.");
        }

        return result;
    }
}
