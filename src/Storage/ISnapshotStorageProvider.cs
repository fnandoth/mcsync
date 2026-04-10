using MCSync.Core;

namespace MCSync.Storage;

public interface ISnapshotStorageProvider
{
    Task<SnapshotUploadResult> UploadSnapshotAsync(UserConfig config, SnapshotArtifact artifact, int worldVersion, CancellationToken cancellationToken = default);
    Task<string> DownloadSnapshotAsync(UserConfig config, string snapshotRef, CancellationToken cancellationToken = default);
}

public sealed record SnapshotArtifact(string FilePath, string Checksum, long SizeBytes);

public sealed record SnapshotUploadResult(string SnapshotRef, string Checksum, long SizeBytes, int WorldVersion);
