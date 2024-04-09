namespace DurableOrchestrator.Tests.Common;

public abstract class FunctionalTest(FunctionalTestFixture fixture)
{
    protected FunctionalTestFixture Fixture { get; } = fixture;
}
