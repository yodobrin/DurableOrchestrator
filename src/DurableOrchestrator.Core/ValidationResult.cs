namespace DurableOrchestrator.Core;

/// <summary>
/// Defines a result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly List<string> _validationMessages = new();

    /// <summary>
    /// Gets a value indicating whether the validation result is valid.
    /// </summary>
    /// <remarks>
    /// This property is set to <see langword="true"/> by default.
    /// To set the validation result as invalid, use the <see cref="AddErrorMessage"/> method.
    /// </remarks>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets or sets the validation messages.
    /// </summary>
    public IEnumerable<string> ValidationMessages => _validationMessages;

    /// <summary>
    /// Adds a message to the validation result.
    /// </summary>
    /// <param name="message">The message to add.</param>
    public void AddMessage(string message)
    {
        _validationMessages.Add(message);
    }

    /// <summary>
    /// Adds an error message to the validation result and sets the result as invalid.
    /// </summary>
    /// <param name="message">The error message to add.</param>
    public void AddErrorMessage(string message)
    {
        IsValid = false;
        AddMessage(message);
    }

    /// <summary>
    /// Returns a string combining the validation messages.
    /// </summary>
    /// <returns>The combined validation messages.</returns>
    public override string ToString()
    {
        return string.Join(", ", ValidationMessages);
    }
}
