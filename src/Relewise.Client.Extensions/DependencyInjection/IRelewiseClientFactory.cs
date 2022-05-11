namespace Relewise.Client.Extensions.DependencyInjection;

public interface IRelewiseClientFactory
{
    T GetClient<T>() where T : IClient;
    T GetClient<T>(string name) where T : IClient;
}