using System.Collections.Generic;
using System.Linq;

namespace Relewise.Client.Extensions.Infrastructure.Extensions;

internal static class DictionaryExtensions
{
    internal static IEnumerable<(string name, RelewiseClientsOptionsBuilder clientOptions)> AsTuples(this Dictionary<string, RelewiseClientsOptionsBuilder> clients)
    {
        return clients.Select(x => (name: x.Key, clientOptions: x.Value));
    }
}