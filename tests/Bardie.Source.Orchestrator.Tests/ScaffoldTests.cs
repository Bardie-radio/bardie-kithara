using Bardie.Source.Orchestrator;
using Xunit;

namespace Bardie.Source.Orchestrator.Tests;

public class ScaffoldTests
{
    [Fact]
    public void SourceOrchestrator_assembly_loads()
    {
        Assert.NotNull(typeof(SourceOrchestratorMarker).Assembly.GetName().Name);
    }
}
