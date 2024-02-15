
namespace event_processing
{
    public static class SayHelloActivity
    {
        [FunctionName("SayHello")]
        public static string SayHello([ActivityTrigger]  WorkFlowInput input, ILogger log)
        {
            
            log.LogInformation($"got a secret {input.Destination}.");
            return $"Hello {input.Destination}!";
        }
    }
}
