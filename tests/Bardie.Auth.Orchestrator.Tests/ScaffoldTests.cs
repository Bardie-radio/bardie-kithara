using Bardie.Auth.Orchestrator;
using Xunit;

namespace Bardie.Auth.Orchestrator.Tests;

public class ScaffoldTests
{
    [Fact]
    public void AuthOrchestrator_assembly_loads()
    {
        Assert.NotNull(typeof(AuthOrchestratorMarker).Assembly.GetName().Name);
    }
}
