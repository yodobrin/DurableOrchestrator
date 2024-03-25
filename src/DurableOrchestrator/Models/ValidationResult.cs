namespace DurableOrchestrator.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationMessages { get; set; } = new List<string>();

    public void AddMessage(string error)
    {
        ValidationMessages.Add(error);
    }

    public string GetValidationMessages()
    {
        return string.Join(", ", ValidationMessages);
    }
}
