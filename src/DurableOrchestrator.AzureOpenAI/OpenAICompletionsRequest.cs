using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureOpenAI;

/// <summary>
/// Defines a model that represents a chat completions request to the OpenAI service.
/// </summary>
public class OpenAICompletionsRequest : OpenAIRequest
{
    [JsonPropertyName("maxTokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("topP")]
    public float? TopP { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }

    [JsonPropertyName("messages")]
    public IEnumerable<string>? Messages { get; set; }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();

        if (MaxTokens is < 0)
        {
            result.AddErrorMessage($"{nameof(MaxTokens)} must be greater than or equal to 0.");
        }

        if (Temperature is < 0 or > 2.0f)
        {
            result.AddErrorMessage($"{nameof(Temperature)} must be between 0 and 2.0.");
        }

        if (TopP is < 0 or > 1.0f)
        {
            result.AddErrorMessage($"{nameof(TopP)} must be between 0 and 1.0.");
        }

        if (Messages is null || !Messages.Any())
        {
            result.AddErrorMessage($"{nameof(Messages)} is missing or empty.");
        }

        return result;
    }
}
