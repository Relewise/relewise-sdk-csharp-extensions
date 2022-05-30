namespace Relewise.Client.Extensions;

/// <summary>
/// Represents the configuration of a named client.
/// </summary>
public class RelewiseNamedClientOptions
{
    internal RelewiseNamedClientOptions(
        string name, 
        RelewiseClientOptions? globalOptions, 
        RelewiseClientOptions? tracker,
        RelewiseClientOptions? recommender, 
        RelewiseClientOptions? searcher)
    {
        Name = name;
        Options = globalOptions;
        Tracker = tracker;
        Recommender = recommender;
        Searcher = searcher;
    }

    /// <summary>
    /// This is the name of client registered during StartUp
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The options configured for the client
    /// </summary>
    public RelewiseClientOptions? Options { get; }

    /// <summary>
    /// The options configured for the clients Tracker
    /// </summary>
    public RelewiseClientOptions? Tracker { get; }

    /// <summary>
    /// The options configured for the clients Recommender
    /// </summary>
    public RelewiseClientOptions? Recommender { get; }

    /// <summary>
    /// The options configured for the clients Searcher
    /// </summary>
    public RelewiseClientOptions? Searcher { get; }
}