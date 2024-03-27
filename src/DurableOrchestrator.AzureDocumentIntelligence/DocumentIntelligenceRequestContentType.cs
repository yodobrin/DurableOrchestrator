namespace DurableOrchestrator.AzureDocumentIntelligence;

/// <summary>
/// Defines the methods by which document content is provided in a <see cref="DocumentIntelligenceRequest"/>.
/// </summary>
public enum DocumentIntelligenceRequestContentType
{
    /// <summary>
    /// The document content is provided via a URI.
    /// </summary>
    Uri,

    /// <summary>
    /// The document content is provided in-memory as a byte array.
    /// </summary>
    InMemory
}
