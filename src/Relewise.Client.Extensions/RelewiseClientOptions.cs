using System;

namespace Relewise.Client.Extensions;

public class RelewiseClientOptions : IEquatable<RelewiseClientOptions>
{
    public RelewiseClientOptions(Guid datasetId, string apiKey, TimeSpan timeout)
    {
        if (datasetId.Equals(Guid.Empty)) throw new ArgumentException(@"Value cannot be empty.", nameof(datasetId));
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException(@"Value cannot be null or empty", nameof(apiKey));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), timeout, @"Timeout value cannot be negative.");
        
        DatasetId = datasetId;
        ApiKey = apiKey;
        Timeout = timeout;
    }

    public Guid DatasetId { get; }
    public string ApiKey { get; }
    public TimeSpan Timeout { get; }

    public bool Equals(RelewiseClientOptions? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return DatasetId.Equals(other.DatasetId) && ApiKey == other.ApiKey && Timeout.Equals(other.Timeout);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RelewiseClientOptions)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = DatasetId.GetHashCode();
            hashCode = (hashCode * 397) ^ ApiKey.GetHashCode();
            hashCode = (hashCode * 397) ^ Timeout.GetHashCode();
            return hashCode;
        }
    }
}