namespace Relewise.Client.Extensions;

/// <summary>
/// Represents the configuration of a named client.
/// </summary>
public class RelewiseNamedClientOptions
{
    internal RelewiseNamedClientOptions(string name, RelewiseClientOptions options)
    {
        Name = name;
        Options = options;
    }

    /// <summary>
    /// This is the name of client registered during StartUp
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The options configured for the client
    /// </summary>
    public RelewiseClientOptions Options { get; }
}