using MCSync.Core;
using MCSync.GitHub;

namespace MCSync.Storage;

public sealed class GitHubSnapshotStorageProvider : ISnapshotStorageProvider
{
    private readonly GitHubClient _gitHubClient;
    private readonly AppLogger _logger;

    public GitHubSnapshotStorageProvider(GitHubClient gitHubClient, AppLogger logger)
    {
        _gitHubClient = gitHubClient;
        _logger = logger;
    }

    public async Task<SnapshotUploadResult> UploadSnapshotAsync(
        UserConfig config,
        SnapshotArtifact artifact,
        int worldVersion,
        CancellationToken cancellationToken = default)
    {
        var snapshotRef = $"{config.SnapshotFolderPath.TrimEnd('/')}/{config.WorldId}/world-v{worldVersion:000000}.zip";
        var bytes = await File.ReadAllBytesAsync(artifact.FilePath, cancellationToken);

        await _gitHubClient.PutBytesAsync(
            config,
            snapshotRef,
            bytes,
            $"Upload world snapshot v{worldVersion}",
            null,
            cancellationToken);

        _logger.Info($"Snapshot subido a GitHub en {snapshotRef}.");
        return new SnapshotUploadResult(snapshotRef, artifact.Checksum, artifact.SizeBytes, worldVersion);
    }

    public async Task<string> DownloadSnapshotAsync(UserConfig config, string snapshotRef, CancellationToken cancellationToken = default)
    {
        var file = await _gitHubClient.GetBytesAsync(config, snapshotRef, cancellationToken)
            ?? throw new FileNotFoundException($"No se encontro el snapshot remoto {snapshotRef}.");

        AppPaths.EnsureCreated();
        var localPath = Path.Combine(AppPaths.TempDirectory, $"{Path.GetFileNameWithoutExtension(snapshotRef)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");
        await File.WriteAllBytesAsync(localPath, file.Content, cancellationToken);

        _logger.Info($"Snapshot descargado a {localPath}.");
        return localPath;
    }
}
