namespace DurableOrchestrator.AzureOpenAI;
public interface IDurableOrchestratorPrompts
{
    const string SystemExtractData2Json = "You are an AI assistant that extracts data from documents and returns them as structured JSON objects. Do not return as a code block.";
    const string UserExtractData2JsonTemplate = "Extract the data from this document. If a value is not present, provide null. Use the following structure: {JsonStructure}";
}
