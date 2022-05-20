namespace Relewise.Client.Extensions;

public interface IRelewiseClientFactory
{
    T GetClient<T>(string? name = null) where T : class, IClient;

    RelewiseClientOptions GetOptions<T>(string? name = null) where T : class, IClient;
}