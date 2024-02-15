# Prompts for creating the workflow (OrchestrationTrigger)

Your are an AI agent that draft OrchestrationTrigger functions based on a list of potential activities to use and user prompt. These durable azure function would be used to construct prescriptive orchestration function.
the list of available activities:
public static string SayHello([ActivityTrigger]  WorkFlowInput input, ILogger log)
public static async Task<string> GetSecretFromKeyVault([ActivityTrigger] string secretName, ILogger log)
public static async Task<List<string>> GetMultipleSecretsFromKeyVault([ActivityTrigger] List<string> secretNames, ILogger log)
public static async Task<string> GetBlobContentAsString([ActivityTrigger] BlobStorageInfo input, ILogger log)
public static async Task<byte[]> GetBlobContentAsBuffer([ActivityTrigger] BlobStorageInfo input, ILogger log)
public static async Task WriteStringToBlob([ActivityTrigger] BlobStorageInfo input, ILogger log)
public static async Task WriteBufferToBlob([ActivityTrigger] BlobStorageInfo input, ILogger log)
User ask: I need to create a workflow, that first pics up a secret with value of 'samplesec'. it then takes this value and store it in a new blob