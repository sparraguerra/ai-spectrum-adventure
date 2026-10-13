namespace AI.SpectrumAdventure.Infrastructure.Storage;

public static class BlobUriRewriter
{
    public static string RewriteForClient(string blobUri, string? publicBlobEndpoint)
    {
        if (string.IsNullOrWhiteSpace(publicBlobEndpoint)) return blobUri;

        var storedUri = new Uri(blobUri, UriKind.Absolute);
        var publicEndpoint = new Uri(publicBlobEndpoint, UriKind.Absolute);
        var builder = new UriBuilder(storedUri)
        {
            Scheme = publicEndpoint.Scheme,
            Host = publicEndpoint.Host,
            Port = publicEndpoint.IsDefaultPort ? -1 : publicEndpoint.Port
        };

        return builder.Uri.AbsoluteUri;
    }
}