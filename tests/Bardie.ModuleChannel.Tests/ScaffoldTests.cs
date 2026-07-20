using Bardie.ModuleChannel;
using Xunit;

namespace Bardie.ModuleChannel.Tests;

public class ScaffoldTests
{
    [Fact]
    public void ModuleChannel_assembly_loads()
    {
        Assert.NotNull(typeof(ModuleChannelMarker).Assembly.GetName().Name);
    }
}
