using MCSync.Core;

namespace MCSync.UI;

public sealed class MCSyncApplicationContext : ApplicationContext
{
    private readonly SyncOrchestrator _orchestrator;
    private readonly TrayIconController _trayIconController;

    public MCSyncApplicationContext(SyncOrchestrator orchestrator, ConfigStore configStore, AppLogger logger)
    {
        _orchestrator = orchestrator;
        _trayIconController = new TrayIconController(orchestrator, configStore, logger, ExitThread);
    }

    protected override void ExitThreadCore()
    {
        _trayIconController.Dispose();
        _orchestrator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.ExitThreadCore();
    }
}
