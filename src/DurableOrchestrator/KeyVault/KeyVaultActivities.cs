using Azure.Security.KeyVault.Secrets;
using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.KeyVault;

[ActivitySource(nameof(KeyVaultActivities))]
public class KeyVaultActivities(SecretClient client, ILogger<KeyVaultActivities> log)
{
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(KeyVaultActivities));

    private const string DefaultSecretValue = "N/A";

    [Function(nameof(GetSecretFromKeyVault))]
    public async Task<string> GetSecretFromKeyVault([ActivityTrigger] string secretName, FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetSecretFromKeyVault));

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name must not be null or whitespace.");
        }

        try
        {
            KeyVaultSecret secret = await client.GetSecretAsync(secretName);
            log.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
            return secret.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            log.LogWarning("Secret not found: {SecretName}. Using default value.", secretName);
            return DefaultSecretValue; // Return default value if secret not found
        }
        catch (Exception ex)
        {
            log.LogError("Error retrieving secret {SecretName}: {Message}", secretName, ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw; // Rethrow exceptions other than not found
        }
    }

    [Function(nameof(GetMultipleSecretsFromKeyVault))]
    public async Task<List<string>> GetMultipleSecretsFromKeyVault([ActivityTrigger] List<string> secretNames, FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetMultipleSecretsFromKeyVault));

        var secretsValues = new List<string>();

        foreach (var secretName in secretNames)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                log.LogWarning("One of the secret names is null or whitespace.");
                secretsValues.Add(DefaultSecretValue);
                continue;
            }

            try
            {
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                secretsValues.Add(secret.Value);
                log.LogInformation("Successfully retrieved secret: {SecretName}", secretName);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                log.LogWarning("Secret not found: {SecretName}. Using default value.", secretName);
                secretsValues.Add(DefaultSecretValue);
            }
            catch (Exception ex)
            {
                log.LogError("Error retrieving secret {SecretName}: {Message}", secretName, ex.Message);
                secretsValues.Add(DefaultSecretValue);
            }
        }

        return secretsValues;
    }
}
