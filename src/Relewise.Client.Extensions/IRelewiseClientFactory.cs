namespace Relewise.Client.Extensions;

public interface IRelewiseClientFactory
{
    T GetClient<T>() where T : IClient;

    T GetClient<T>(string name) where T : IClient;

    // NOTE:
    //  Overvej at tilføje mulighed for at tilgå 'Options'-instansen - også named-udgaven.
    //  Nyttigt hvis man skal udveksle en API-nøgle med sin front-end.
}