using MCSync.Minecraft;
using MCSync.Storage;
using MCSync.Tunnel;

namespace MCSync.Core;

public sealed class SyncOrchestrator : IAsyncDisposable
{
    private readonly ConfigStore _configStore;
    private readonly IStateStore _stateStore;
    private readonly ISnapshotStorageProvider _snapshotStorageProvider;
    private readonly WorldManager _worldManager;
    private readonly ServerManager _serverManager;
    private readonly TunnelManager _tunnelManager;
    private readonly AppLogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CancellationTokenSource? _heartbeatCts;
    private UserConfig? _activeConfig;
    private string? _activeLeaseId;
    private bool _isStopping;

    public SyncOrchestrator(
        ConfigStore configStore,
        IStateStore stateStore,
        ISnapshotStorageProvider snapshotStorageProvider,
        WorldManager worldManager,
        ServerManager serverManager,
        TunnelManager tunnelManager,
        AppLogger logger)
    {
        _configStore = configStore;
        _stateStore = stateStore;
        _snapshotStorageProvider = snapshotStorageProvider;
        _worldManager = worldManager;
        _serverManager = serverManager;
        _tunnelManager = tunnelManager;
        _logger = logger;

        _serverManager.ServerExited += OnServerExited;
    }

    public event EventHandler<SyncStatusChangedEventArgs>? StatusChanged;

    public SyncLifecycleStatus Status { get; private set; } = SyncLifecycleStatus.Idle;

    public string StatusMessage { get; private set; } = "Listo";

    public string? CurrentTunnelAddress { get; private set; }

    public AppState? LastKnownRemoteState { get; private set; }

    public bool IsHosting => _activeLeaseId is not null;

    public async Task<UserConfig> LoadConfigAsync(CancellationToken cancellationToken = default) =>
        await _configStore.LoadAsync(cancellationToken);

    public async Task<AppState> RefreshStateAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configStore.LoadAsync(cancellationToken);

        if (!config.IsValid(out _))
        {
            LastKnownRemoteState = AppState.CreateDefault(config.WorldId);
            return LastKnownRemoteState;
        }

        LastKnownRemoteState = await _stateStore.GetStateAsync(config, cancellationToken);
        return LastKnownRemoteState;
    }

    public async Task<string?> GetCurrentAddressAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(CurrentTunnelAddress))
        {
            return CurrentTunnelAddress;
        }

        var state = await RefreshStateAsync(cancellationToken);
        return state.Host?.TunnelAddress;
    }

    public async Task StartHostingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (IsHosting)
            {
                throw new InvalidOperationException("La app ya esta hospedando el mundo.");
            }

            var config = await _configStore.LoadAsync(cancellationToken);
            if (!config.IsValid(out var error))
            {
                throw new InvalidOperationException(error);
            }

            _activeConfig = config;
            SetStatus(SyncLifecycleStatus.SyncingDown, "Verificando estado remoto...");

            var acquireResult = await _stateStore.TryAcquireLeaseAsync(config, cancellationToken);
            if (!acquireResult.Success)
            {
                var activeEndpoint = acquireResult.State.Host?.TunnelAddress;
                if (!string.IsNullOrWhiteSpace(activeEndpoint))
                {
                    throw new InvalidOperationException($"{acquireResult.FailureMessage} Host actual: {activeEndpoint}");
                }

                throw new InvalidOperationException(acquireResult.FailureMessage ?? "No fue posible tomar el rol de host.");
            }

            _activeLeaseId = acquireResult.LeaseId;
            LastKnownRemoteState = acquireResult.State;
            StartHeartbeatLoop(config, acquireResult.LeaseId);

            try
            {
                await SyncDownIfRequiredAsync(config, acquireResult.State, cancellationToken);

                SetStatus(SyncLifecycleStatus.StartingServer, "Preparando carpeta del servidor...");
                await _worldManager.PrepareServerFolderAsync(config, cancellationToken);

                SetStatus(SyncLifecycleStatus.StartingServer, "Iniciando server.jar...");
                await _serverManager.StartAsync(config, cancellationToken);

                SetStatus(SyncLifecycleStatus.StartingServer, "Abriendo tunel playit...");
                CurrentTunnelAddress = await _tunnelManager.StartAsync(config, cancellationToken);
                LastKnownRemoteState = await _stateStore.UpdateTunnelAddressAsync(config, acquireResult.LeaseId, CurrentTunnelAddress, cancellationToken);

                var connectedAddress = string.IsNullOrWhiteSpace(CurrentTunnelAddress)
                    ? "Servidor listo. Esperando direccion de tunel..."
                    : $"Servidor listo en {CurrentTunnelAddress}";

                SetStatus(SyncLifecycleStatus.Hosting, connectedAddress);
                _logger.Info(connectedAddress);
            }
            catch
            {
                await CleanupFailedStartAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopHostingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            await FinalizeHostingInternalAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task FinalizeHostingInternalAsync(CancellationToken cancellationToken)
    {
        if (_activeConfig is null || string.IsNullOrWhiteSpace(_activeLeaseId) || _isStopping)
        {
            return;
        }

        _isStopping = true;
        var stopCompleted = false;

        try
        {
            SetStatus(SyncLifecycleStatus.StoppingServer, "Deteniendo host y bloqueando nuevos cambios...");
            LastKnownRemoteState = await _stateStore.MarkTransferringAsync(_activeConfig, _activeLeaseId, cancellationToken);

            if (_serverManager.IsRunning)
            {
                await _serverManager.StopGracefullyAsync(cancellationToken);
            }

            if (_tunnelManager.IsRunning)
            {
                await _tunnelManager.StopAsync();
            }

            SetStatus(SyncLifecycleStatus.SyncingUp, "Comprimiendo mundo...");
            var artifact = await _worldManager.CreateSnapshotAsync(_activeConfig, cancellationToken);

            var nextVersion = Math.Max(LastKnownRemoteState?.WorldVersion ?? 0, 0) + 1;
            _logger.Info($"Subiendo snapshot version {nextVersion}...");

            var upload = await _snapshotStorageProvider.UploadSnapshotAsync(_activeConfig, artifact, nextVersion, cancellationToken);
            LastKnownRemoteState = await _stateStore.PublishSnapshotAsync(_activeConfig, _activeLeaseId, upload, cancellationToken);

            await _worldManager.MarkLocalStateAsync(upload.WorldVersion, upload.Checksum, cancellationToken);
            stopCompleted = true;
        }
        finally
        {
            await ReleaseRuntimeStateAsync();
            if (stopCompleted)
            {
                SetStatus(SyncLifecycleStatus.Idle, "Mundo sincronizado. Host liberado.");
                _logger.Info("Host liberado y mundo sincronizado correctamente.");
            }
            _isStopping = false;
        }
    }

    private async Task SyncDownIfRequiredAsync(UserConfig config, AppState remoteState, CancellationToken cancellationToken)
    {
        if (remoteState.WorldVersion <= 0 || string.IsNullOrWhiteSpace(remoteState.SnapshotRef))
        {
            _logger.Info("No existe snapshot remoto previo. Se iniciara un mundo nuevo.");
            return;
        }

        var localState = await _worldManager.LoadLocalStateAsync(cancellationToken);
        if (localState.WorldVersion == remoteState.WorldVersion &&
            string.Equals(localState.WorldChecksum, remoteState.WorldChecksum, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info("El snapshot local ya coincide con la ultima version remota.");
            return;
        }

        SetStatus(SyncLifecycleStatus.SyncingDown, $"Descargando mundo v{remoteState.WorldVersion}...");
        var zipPath = await _snapshotStorageProvider.DownloadSnapshotAsync(config, remoteState.SnapshotRef, cancellationToken);
        await _worldManager.ExtractSnapshotAsync(config, zipPath, cancellationToken);
        await _worldManager.MarkLocalStateAsync(remoteState.WorldVersion, remoteState.WorldChecksum, cancellationToken);

        _logger.Info($"Snapshot remoto v{remoteState.WorldVersion} descargado y aplicado.");
    }

    private void StartHeartbeatLoop(UserConfig config, string leaseId)
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts = new CancellationTokenSource();
        var token = _heartbeatCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(config.HeartbeatIntervalSeconds, 5)), token);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    LastKnownRemoteState = await _stateStore.HeartbeatAsync(config, leaseId, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Fallo heartbeat: {ex.Message}");
                }
            }
        }, token);
    }

    private async Task CleanupFailedStartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_tunnelManager.IsRunning)
            {
                await _tunnelManager.StopAsync();
            }

            if (_serverManager.IsRunning)
            {
                await _serverManager.KillAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"La limpieza del arranque fallido reporto un error: {ex.Message}");
        }
        finally
        {
            if (_activeConfig is not null && !string.IsNullOrWhiteSpace(_activeLeaseId))
            {
                try
                {
                    await _stateStore.ReleaseLeaseAsync(_activeConfig, _activeLeaseId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"No fue posible liberar el lease tras un fallo: {ex.Message}");
                }
            }

            await ReleaseRuntimeStateAsync();
            SetStatus(SyncLifecycleStatus.Error, "El inicio como host fallo.");
        }
    }

    private Task ReleaseRuntimeStateAsync()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _heartbeatCts = null;
        _activeConfig = null;
        _activeLeaseId = null;
        CurrentTunnelAddress = null;
        return Task.CompletedTask;
    }

    private async void OnServerExited(object? sender, EventArgs e)
    {
        if (_isStopping || !IsHosting)
        {
            return;
        }

        _logger.Warning("El proceso del servidor termino. Iniciando sincronizacion de cierre automatica.");

        try
        {
            await StopHostingAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Fallo el cierre automatico luego de que el servidor termino.", ex);
            SetStatus(SyncLifecycleStatus.Error, "El servidor termino y la sincronizacion automatica fallo.");
        }
    }

    private void SetStatus(SyncLifecycleStatus status, string message)
    {
        Status = status;
        StatusMessage = message;
        StatusChanged?.Invoke(this, new SyncStatusChangedEventArgs(status, message));
    }

    public async ValueTask DisposeAsync()
    {
        _serverManager.ServerExited -= OnServerExited;

        if (IsHosting)
        {
            try
            {
                await StopHostingAsync();
            }
            catch (Exception ex)
            {
                _logger.Error("No fue posible detener el host al cerrar la app.", ex);
            }
        }

        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _gate.Dispose();
        _serverManager.Dispose();
        _tunnelManager.Dispose();
    }
}

public enum SyncLifecycleStatus
{
    Idle,
    SyncingDown,
    StartingServer,
    Hosting,
    StoppingServer,
    SyncingUp,
    Error
}

public sealed record SyncStatusChangedEventArgs(SyncLifecycleStatus Status, string Message);
