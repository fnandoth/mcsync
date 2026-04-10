using System.Text.Json.Serialization;

namespace MCSync.Core;

public sealed class AppState
{
    public int SchemaVersion { get; set; } = 1;
    public string WorldId { get; set; } = "survival-main";
    public int WorldVersion { get; set; }
    public string? WorldChecksum { get; set; }
    public string? SnapshotRef { get; set; }
    public long? SnapshotSizeBytes { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorldStatus Status { get; set; } = WorldStatus.Idle;
    public HostLeaseInfo? Host { get; set; }
    public DateTimeOffset? LastUploadCompletedAtUtc { get; set; }
    public string? LastCompletedBy { get; set; }

    public static AppState CreateDefault(string worldId) =>
        new()
        {
            WorldId = string.IsNullOrWhiteSpace(worldId) ? "survival-main" : worldId
        };

    public bool HasActiveHost(DateTimeOffset nowUtc) =>
        Host is not null && Host.LeaseExpiresAtUtc > nowUtc;

    public AppState Clone() =>
        new()
        {
            SchemaVersion = SchemaVersion,
            WorldId = WorldId,
            WorldVersion = WorldVersion,
            WorldChecksum = WorldChecksum,
            SnapshotRef = SnapshotRef,
            SnapshotSizeBytes = SnapshotSizeBytes,
            Status = Status,
            LastUploadCompletedAtUtc = LastUploadCompletedAtUtc,
            LastCompletedBy = LastCompletedBy,
            Host = Host?.Clone()
        };
}

public sealed class HostLeaseInfo
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LeaseId { get; set; } = string.Empty;
    public DateTimeOffset LeaseExpiresAtUtc { get; set; }
    public DateTimeOffset LastHeartbeatUtc { get; set; }
    public string? TunnelAddress { get; set; }
    public long HostEpoch { get; set; }

    public HostLeaseInfo Clone() =>
        new()
        {
            ClientId = ClientId,
            DisplayName = DisplayName,
            LeaseId = LeaseId,
            LeaseExpiresAtUtc = LeaseExpiresAtUtc,
            LastHeartbeatUtc = LastHeartbeatUtc,
            TunnelAddress = TunnelAddress,
            HostEpoch = HostEpoch
        };
}

public enum WorldStatus
{
    Idle,
    Hosting,
    Transferring,
    Recovering
}

public sealed record RemoteStateSnapshot(AppState State, string? Sha);
