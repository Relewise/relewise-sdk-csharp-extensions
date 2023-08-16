using System;

namespace Relewise.Client.Extensions;

/// <summary>
/// Represents configuration of a client.
/// </summary>
public class RelewiseClientOptions : IEquatable<RelewiseClientOptions>
{

    /// <summary>
    /// Initializes a new instance of the <see cref="RelewiseClientOptions"/>.
    /// </summary>
    /// <param name="datasetId">Defines the id of the dataset the client should access. The value can be found at https://my.relewise.com/developer-settings.</param>
    /// <param name="apiKey">Defines the api key that should be used. Api keys can be found (and created) at https://my.relewise.com/developer-settings.</param>
    /// <param name="timeout">Defines the timeout to be used by the client.</param>
    /// <param name="serverUrl">Defines the url of the server to target.The value can be found at https://my.relewise.com/developer-settings.</param>
    public RelewiseClientOptions(Guid datasetId, string apiKey, TimeSpan timeout, Uri? serverUrl = null)
    {
        if (datasetId.Equals(Guid.Empty)) throw new ArgumentException(@"Value cannot be empty.", nameof(datasetId));
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException(@"Value cannot be null or empty", nameof(apiKey));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), timeout, @"Timeout value cannot be negative.");

        DatasetId = datasetId;
        ApiKey = apiKey;
        Timeout = timeout;

        if (serverUrl == null) return;
        if (!serverUrl.IsAbsoluteUri || !serverUrl.IsWellFormedOriginalString())
            throw new ArgumentException(@"Value must be a valid absolute uri.", nameof(serverUrl));

        ServerUrl = serverUrl;
    }

    /// <summary>
    /// Defines the id of the dataset the client should access. The value can be found at https://my.relewise.com/developer-settings.
    /// </summary>
    public Guid DatasetId { get; }

    /// <summary>
    /// Defines the api key that should be used. Api keys can be found (and created) at https://my.relewise.com/developer-settings.
    /// </summary>
    public string ApiKey { get; }

    /// <summary>
    /// Defines the timeout to be used by the client.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Defines the url of the Relewise server to target by the client. The value can be found at https://my.relewise.com/developer-settings.
    /// </summary>
    public Uri? ServerUrl { get; }

    /// <summary>
    /// Returns a value indicating whether this instance and a specified <see cref="RelewiseClientOptions"/> object represent the same value.
    /// </summary>
    public bool Equals(RelewiseClientOptions? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return DatasetId.Equals(other.DatasetId) && ApiKey == other.ApiKey && Timeout.Equals(other.Timeout) && ServerUrl == other.ServerUrl;
    }

    /// <summary>
    /// Returns a value that indicates whether this instance is equal to a specified object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RelewiseClientOptions)obj);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = DatasetId.GetHashCode();
            hashCode = (hashCode * 397) ^ ApiKey.GetHashCode();
            hashCode = (hashCode * 397) ^ Timeout.GetHashCode();

            if (ServerUrl != null)
            {
                hashCode = (hashCode * 397) ^ ServerUrl.GetHashCode();
            }

            return hashCode;
        }
    }
}