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
        : this(datasetId, timeout, serverUrl)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException(@"Value cannot be null or empty", nameof(apiKey));
        ApiKey = apiKey;
    }

    /// <summary>
    /// Initializes client options using OAuth 2.0 client credentials.
    /// </summary>
    /// <param name="datasetId">The dataset ID from My Relewise.</param>
    /// <param name="clientId">The OAuth credential ID, separate from the dataset ID.</param>
    /// <param name="clientSecret">The client secret stored in secure server-side configuration.</param>
    /// <param name="timeout">The timeout for the request, including token acquisition.</param>
    /// <param name="serverUrl">The dataset server URL. Defaults to the SDK's production server.</param>
    public RelewiseClientOptions(Guid datasetId, Guid clientId, string clientSecret, TimeSpan timeout, Uri? serverUrl = null)
        : this(datasetId, timeout, serverUrl)
    {
        if (clientId == Guid.Empty) throw new ArgumentException(@"Value cannot be empty.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentException(@"Value cannot be null or empty.", nameof(clientSecret));

        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    private RelewiseClientOptions(Guid datasetId, TimeSpan timeout, Uri? serverUrl)
    {
        if (datasetId.Equals(Guid.Empty)) throw new ArgumentException(@"Value cannot be empty.", nameof(datasetId));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), timeout, @"Timeout value cannot be negative.");

        DatasetId = datasetId;
        Timeout = timeout;

        if (serverUrl != null && IsInvalidUrl(serverUrl))
            throw new ArgumentException(@"Value must be a valid absolute uri.", nameof(serverUrl));

        ServerUrl = serverUrl;
    }

    /// <summary>
    /// Defines the id of the dataset the client should access. The value can be found at https://my.relewise.com/developer-settings.
    /// </summary>
    public Guid DatasetId { get; }

    /// <summary>
    /// Defines the API key, or null when OAuth client credentials are used.
    /// </summary>
    public string? ApiKey { get; }

    /// <summary>Defines the OAuth credential ID, or null when an API key is used.</summary>
    public Guid? ClientId { get; }

    /// <summary>Defines the OAuth client secret, or null when an API key is used.</summary>
    public string? ClientSecret { get; }

    internal string EffectiveServerUrl => ServerUrl?.ToString() ?? "https://api.relewise.com";

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
        return DatasetId.Equals(other.DatasetId) && ApiKey == other.ApiKey && ClientId == other.ClientId &&
               ClientSecret == other.ClientSecret && Timeout.Equals(other.Timeout) && ServerUrl == other.ServerUrl;
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
            hashCode = (hashCode * 397) ^ (ApiKey?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ ClientId.GetHashCode();
            hashCode = (hashCode * 397) ^ (ClientSecret?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ Timeout.GetHashCode();
            if (ServerUrl != null) hashCode = (hashCode * 397) ^ ServerUrl.GetHashCode();

            return hashCode;
        }
    }

    private static bool IsInvalidUrl(Uri serverUrl)
    {
        return !serverUrl.IsAbsoluteUri || !serverUrl.IsWellFormedOriginalString();
    }

    /// <summary>
    /// Defines the internal property to read the optional ApiKey, when reading from parent.
    /// </summary>
    protected internal virtual string? ApiKeyWhenReadAsParent => ApiKey;

    internal class WithoutApiKey : RelewiseClientOptions
    {
        public WithoutApiKey(Guid datasetId, TimeSpan timeout, Uri? serverUrl = null)
            : base(datasetId, "dummy-api-key", timeout, serverUrl)
        {
        }

        protected internal override string? ApiKeyWhenReadAsParent => null;
    }
}
