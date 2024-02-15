
namespace event_processing;

public static class SayHelloActivity
{
    [Function("SayHello")]
    public static string SayHello([ActivityTrigger] WorkFlowInput input, FunctionContext executionContext)
    {
        var log = executionContext.GetLogger("SayHello");

        log.LogInformation($"got a secret {input.Destination}.");
        return $"Hello {input.Destination}!";
    }
}
