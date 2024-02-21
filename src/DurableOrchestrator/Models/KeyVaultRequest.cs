namespace DurableOrchestrator.Models;

public class KeyVaultRequest : IObservableContext
{
    [JsonPropertyName("secretName")]
    public string? SecretName { get; set; }

    [JsonPropertyName("secretNames")]
    public IEnumerable<string>? SecretNames { get; set; }

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservableProperties { get; set; } = new();
}
