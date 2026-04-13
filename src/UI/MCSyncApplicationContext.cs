using MCSync.Core;

namespace MCSync.UI;

public sealed class MCSyncApplicationContext : ApplicationContext
{
    private readonly SyncOrchestrator _orchestrator;
    private readonly TrayIconController _trayIconController;
    private readonly DashboardForm _dashboardForm;

    public MCSyncApplicationContext(SyncOrchestrator orchestrator, ConfigStore configStore, AppLogger logger)
    {
        _orchestrator = orchestrator;
        _dashboardForm = new DashboardForm(orchestrator, configStore, logger);
        MainForm = _dashboardForm;
        _trayIconController = new TrayIconController(orchestrator, configStore, logger, _dashboardForm.ShowDashboard, ExitThread);
        _dashboardForm.ShowDashboard();
    }

    protected override void ExitThreadCore()
    {
        _trayIconController.Dispose();
        _dashboardForm.Dispose();
        _orchestrator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.ExitThreadCore();
    }
}
