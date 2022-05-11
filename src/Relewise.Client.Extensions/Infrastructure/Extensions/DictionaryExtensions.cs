using System.Collections.Generic;
using System.Linq;
using Relewise.Client.Extensions.DependencyInjection;

namespace Relewise.Client.Extensions.Infrastructure.Extensions;

internal static class DictionaryExtensions
{
    internal static IEnumerable<(string name, ClientOptions clientOptions)> AsTuples(this Dictionary<string, ClientOptions> clients)
    {
        return clients.Select(x => (name: x.Key, clientOptions: x.Value));
    }
}