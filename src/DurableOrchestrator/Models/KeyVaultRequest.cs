using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Models;

public class KeyVaultRequest : IObservabilityContext
{
    [JsonPropertyName("secretName")]
    public string? SecretName { get; set; }

    [JsonPropertyName("secretNames")]
    public IEnumerable<string>? SecretNames { get; set; }

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();
}
