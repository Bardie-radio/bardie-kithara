using Microsoft.Extensions.Options;
using Xunit;

namespace Bardie.Module.Source.Debug.Tests;

public class DevProofTrackTests
{
    [Theory]
    [InlineData("sine", true)]
    [InlineData("magpie:sine", true)]
    [InlineData("starling:sine", true)]
    [InlineData("youtube", false)]
    public void Matches_proof_refs(string input, bool expected) =>
        Assert.Equal(expected, DevProofTrack.Matches(input));

    [Fact]
    public void SinePcmGenerator_writes_canonical_pcm()
    {
        var gen = new SinePcmGenerator(Options.Create(new SinePcmOptions
        {
            FrequencyHz = 440,
            DurationSeconds = 0.01,
        }));
        using var stream = gen.CreateStream();
        Assert.True(stream.Length > 0);
        Assert.Equal(0, stream.Length % (SinePcmGenerator.Channels * sizeof(short)));
    }
}
