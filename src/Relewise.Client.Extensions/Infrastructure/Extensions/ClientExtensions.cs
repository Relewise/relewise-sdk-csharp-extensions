using System;

namespace Relewise.Client.Extensions.Infrastructure.Extensions;

internal static class ClientExtensions
{
    public static TClient ConfigureClient<TClient>(this TClient client, Uri? serverUrl) where TClient : IClient
    {
        if (serverUrl != null)
            client.ServerUrl = serverUrl.ToString();

        return client;
    }
}