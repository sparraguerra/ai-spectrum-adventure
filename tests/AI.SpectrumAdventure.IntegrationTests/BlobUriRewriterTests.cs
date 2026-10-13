namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Infrastructure.Storage;
using FluentAssertions;

public sealed class BlobUriRewriterTests
{
    [Fact]
    public void RewriteForClient_ReplacesInternalHostAndPreservesEscapedBlobPath()
    {
        const string internalUri = "http://azurite:10000/devstoreaccount1/visual-assets/world%253Aabc%257Clocation%253Aforest-entrance.png";

        var result = BlobUriRewriter.RewriteForClient(internalUri, "http://localhost:10000");

        result.Should().Be("http://localhost:10000/devstoreaccount1/visual-assets/world%253Aabc%257Clocation%253Aforest-entrance.png");
    }

    [Fact]
    public void RewriteForClient_LeavesUriUnchangedWhenNoPublicEndpointIsConfigured()
    {
        const string internalUri = "https://account.blob.core.windows.net/visual-assets/scene.png";

        BlobUriRewriter.RewriteForClient(internalUri, null).Should().Be(internalUri);
    }
}