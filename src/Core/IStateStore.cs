using MCSync.Storage;

namespace MCSync.Core;

public interface IStateStore
{
    Task<AppState> GetStateAsync(UserConfig config, CancellationToken cancellationToken = default);
    Task<AcquireLeaseResult> TryAcquireLeaseAsync(UserConfig config, CancellationToken cancellationToken = default);
    Task<AppState> HeartbeatAsync(UserConfig config, string leaseId, CancellationToken cancellationToken = default);
    Task<AppState> UpdateTunnelAddressAsync(UserConfig config, string leaseId, string? tunnelAddress, CancellationToken cancellationToken = default);
    Task<AppState> MarkTransferringAsync(UserConfig config, string leaseId, CancellationToken cancellationToken = default);
    Task<AppState> PublishSnapshotAsync(UserConfig config, string leaseId, SnapshotUploadResult snapshot, CancellationToken cancellationToken = default);
    Task ReleaseLeaseAsync(UserConfig config, string leaseId, CancellationToken cancellationToken = default);
}

public sealed record AcquireLeaseResult(bool Success, AppState State, string LeaseId, string? FailureMessage);
