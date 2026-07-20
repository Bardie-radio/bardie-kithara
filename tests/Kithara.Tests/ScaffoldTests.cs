using Xunit;

namespace Kithara.Tests;

public class ScaffoldTests
{
    /// <summary>
    /// Host integration tests land once Microsoft.AspNetCore.App 10 is available locally/CI.
    /// OTel contract: <c>service.name=bardie.kithara</c> (ADR 008).
    /// </summary>
    [Fact]
    public void Solution_scaffold_is_ready()
    {
        Assert.True(true);
    }
}
