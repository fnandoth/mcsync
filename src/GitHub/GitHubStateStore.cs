using MCSync.Core;
using MCSync.Storage;

namespace MCSync.GitHub;

public sealed class GitHubStateStore : IStateStore
{
    private readonly GitHubClient _gitHubClient;
    private readonly AppLogger _logger;

    public GitHubStateStore(GitHubClient gitHubClient, AppLogger logger)
    {
        _gitHubClient = gitHubClient;
        _logger = logger;
    }

    public async Task<AppState> GetStateAsync(UserConfig config, CancellationToken cancellationToken = default)
    {
        var snapshot = await ReadStateDocumentAsync(config, cancellationToken);
        return snapshot.State;
    }

    public async Task<AcquireLeaseResult> TryAcquireLeaseAsync(UserConfig config, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var snapshot = await ReadStateDocumentAsync(config, cancellationToken);
            var state = snapshot.State;
            var now = DateTimeOffset.UtcNow;

            if (state.HasActiveHost(now) && !string.Equals(state.Host?.ClientId, config.ClientId, StringComparison.Ordinal))
            {
                return new AcquireLeaseResult(false, state, string.Empty, "Otro jugador ya tiene el mundo abierto.");
            }

            var leaseId = Guid.NewGuid().ToString();
            var updated = state.Clone();
            updated.Status = WorldStatus.Hosting;
            updated.Host = new HostLeaseInfo
            {
                ClientId = config.ClientId,
                DisplayName = config.HostDisplayName,
                LeaseId = leaseId,
                LastHeartbeatUtc = now,
                LeaseExpiresAtUtc = now.AddSeconds(config.LeaseTtlSeconds),
                TunnelAddress = null,
                HostEpoch = (state.Host?.HostEpoch ?? 0) + 1
            };

            try
            {
                await _gitHubClient.PutJsonAsync(
                    config,
                    config.StateFilePath,
                    updated,
                    $"Acquire host lease for {config.ClientId}",
                    snapshot.Sha,
                    cancellationToken);

                _logger.Info($"Lease adquirido por {config.HostDisplayName}.");
                return new AcquireLeaseResult(true, updated, leaseId, null);
            }
            catch (GitHubConflictException)
            {
                _logger.Warning("Conflicto al adquirir lease. Reintentando...");
            }
        }

        var latestState = await GetStateAsync(config, cancellationToken);
        return new AcquireLeaseResult(false, latestState, string.Empty, "No fue posible adquirir el lease por una carrera entre clientes.");
    }

    public async Task<AppState> HeartbeatAsync(UserConfig config, string leaseId, CancellationToken cancellationToken = default)
    {
        return await UpdateLeaseAsync(config, leaseId, state =>
        {
            state.Status = state.Status == WorldStatus.Transferring ? WorldStatus.Transferring : WorldStatus.Hosting;
            return state;
        }, cancellationToken);
    }

    public async Task<AppState> UpdateTunnelAddressAsync(UserConfig config, string leaseId, string? tunnelAddress, CancellationToken cancellationToken = default)
    {
        return await UpdateLeaseAsync(config, leaseId, state =>
        {
            state.Status = WorldStatus.Hosting;
            if (state.Host is not null)
            {
                state.Host.TunnelAddress = tunnelAddress;
            }

            return state;
        }, cancellationToken);
    }

    public async Task<AppState> MarkTransferringAsync(UserConfig config, string leaseId, CancellationToken cancellationToken = default)
    {
        return await UpdateLeaseAsync(config, leaseId, state =>
        {
            state.Status = WorldStatus.Transferring;
            return state;
        }, cancellationToken);
    }

    public async Task<AppState> PublishSnapshotAsync(
        UserConfig config,
        string leaseId,
        SnapshotUploadResult snapshot,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var document = await ReadStateDocumentAsync(config, cancellationToken);
            var state = document.State;

            if (!LeaseMatches(state, leaseId))
            {
                throw new InvalidOperationException("El lease actual ya no corresponde a este cliente. Se aborta la publicacion del snapshot.");
            }

            var updated = state.Clone();
            updated.WorldVersion = snapshot.WorldVersion;
            updated.WorldChecksum = snapshot.Checksum;
            updated.SnapshotRef = snapshot.SnapshotRef;
            updated.SnapshotSizeBytes = snapshot.SizeBytes;
            updated.Status = WorldStatus.Idle;
            updated.LastUploadCompletedAtUtc = DateTimeOffset.UtcNow;
            updated.LastCompletedBy = config.ClientId;
            updated.Host = null;

            try
            {
                await _gitHubClient.PutJsonAsync(
                    config,
                    config.StateFilePath,
                    updated,
                    $"Publish snapshot v{snapshot.WorldVersion}",
                    document.Sha,
                    cancellationToken);

                return updated;
            }
            catch (GitHubConflictException)
            {
                _logger.Warning("Conflicto al publicar snapshot. Reintentando...");
            }
        }

        throw new InvalidOperationException("No fue posible publicar el snapshot final luego de varios intentos.");
    }

    public async Task ReleaseLeaseAsync(UserConfig config, string leaseId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var document = await ReadStateDocumentAsync(config, cancellationToken);
            if (!LeaseMatches(document.State, leaseId))
            {
                return;
            }

            var updated = document.State.Clone();
            updated.Status = WorldStatus.Idle;
            updated.Host = null;

            try
            {
                await _gitHubClient.PutJsonAsync(
                    config,
                    config.StateFilePath,
                    updated,
                    $"Release host lease for {config.ClientId}",
                    document.Sha,
                    cancellationToken);

                return;
            }
            catch (GitHubConflictException)
            {
                _logger.Warning("Conflicto al liberar lease. Reintentando...");
            }
        }
    }

    private async Task<AppState> UpdateLeaseAsync(
        UserConfig config,
        string leaseId,
        Func<AppState, AppState> mutator,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var document = await ReadStateDocumentAsync(config, cancellationToken);
            if (!LeaseMatches(document.State, leaseId))
            {
                throw new InvalidOperationException("El lease del host ya no es valido.");
            }

            var updated = mutator(document.State.Clone());
            if (updated.Host is not null)
            {
                updated.Host.LastHeartbeatUtc = DateTimeOffset.UtcNow;
                updated.Host.LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(config.LeaseTtlSeconds);
            }

            try
            {
                await _gitHubClient.PutJsonAsync(
                    config,
                    config.StateFilePath,
                    updated,
                    $"Update state for {config.ClientId}",
                    document.Sha,
                    cancellationToken);

                return updated;
            }
            catch (GitHubConflictException)
            {
                _logger.Warning("Conflicto al actualizar el estado. Reintentando...");
            }
        }

        throw new InvalidOperationException("No fue posible actualizar el estado remoto tras varios intentos.");
    }

    private async Task<RemoteStateSnapshot> ReadStateDocumentAsync(UserConfig config, CancellationToken cancellationToken)
    {
        var file = await _gitHubClient.GetJsonAsync<AppState>(config, config.StateFilePath, cancellationToken);
        if (file is null)
        {
            return new RemoteStateSnapshot(AppState.CreateDefault(config.WorldId), null);
        }

        var state = file.Content;
        state.WorldId = string.IsNullOrWhiteSpace(state.WorldId) ? config.WorldId : state.WorldId;
        return new RemoteStateSnapshot(state, file.Sha);
    }

    private static bool LeaseMatches(AppState state, string leaseId) =>
        state.Host is not null &&
        string.Equals(state.Host.LeaseId, leaseId, StringComparison.Ordinal);
}
