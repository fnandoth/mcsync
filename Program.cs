using MCSync.Core;
using MCSync.GitHub;
using MCSync.Minecraft;
using MCSync.Storage;
using MCSync.Tunnel;
using MCSync.UI;

namespace MCSync;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppPaths.EnsureCreated();
        Application.Run(BuildApplicationContext());
    }

    private static ApplicationContext BuildApplicationContext()
    {
        var logger = new AppLogger();
        var configStore = new ConfigStore();
        var localWorldStateStore = new LocalWorldStateStore();
        var gitHubClient = new GitHubClient(logger);
        var stateStore = new GitHubStateStore(gitHubClient, logger);
        var snapshotStorageProvider = new GitHubSnapshotStorageProvider(gitHubClient, logger);
        var worldManager = new WorldManager(logger, localWorldStateStore);
        var serverManager = new ServerManager(logger);
        var tunnelManager = new TunnelManager(logger);
        var orchestrator = new SyncOrchestrator(
            configStore,
            stateStore,
            snapshotStorageProvider,
            worldManager,
            serverManager,
            tunnelManager,
            logger);

        return new MCSyncApplicationContext(orchestrator, configStore, logger);
    }
}
