using System.IO.Compression;
using System.Security.Cryptography;
using MCSync.Core;
using MCSync.Storage;

namespace MCSync.Minecraft;

public sealed class WorldManager
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "logs",
        "crash-reports",
        "cache",
        "libraries",
        "versions"
    };

    private static readonly HashSet<string> IgnoredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "server.jar"
    };

    private readonly AppLogger _logger;
    private readonly LocalWorldStateStore _localWorldStateStore;

    public WorldManager(AppLogger logger, LocalWorldStateStore localWorldStateStore)
    {
        _logger = logger;
        _localWorldStateStore = localWorldStateStore;
    }

    public async Task PrepareServerFolderAsync(UserConfig config, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var serverFolderPath = GetServerFolderPath(config);
            Directory.CreateDirectory(serverFolderPath);

            var targetJarPath = Path.Combine(serverFolderPath, "server.jar");
            
            // Copy only when server.jar is missing in destination
            // or differs from the configured source file.
            if (!File.Exists(targetJarPath) || !FilesAreIdentical(config.ServerJarPath, targetJarPath))
            {
                File.Copy(config.ServerJarPath, targetJarPath, true);
            }

            if (config.AutoAcceptEula)
            {
                var eulaPath = Path.Combine(serverFolderPath, "eula.txt");
                File.WriteAllText(eulaPath, "eula=true" + Environment.NewLine);
            }
        }, cancellationToken);
    }

    public async Task<SnapshotArtifact> CreateSnapshotAsync(UserConfig config, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();

        var zipPath = Path.Combine(AppPaths.TempDirectory, $"{config.WorldId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");

        await Task.Run(() =>
        {
            var serverFolderPath = GetServerFolderPath(config);
            if (!Directory.Exists(serverFolderPath))
            {
                throw new DirectoryNotFoundException("La carpeta del servidor no existe.");
            }

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var filePath in EnumerateFilesToArchive(serverFolderPath))
            {
                var relativePath = Path.GetRelativePath(serverFolderPath, filePath);
                archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.SmallestSize);
            }
        }, cancellationToken);

        var checksum = await ComputeSha256Async(zipPath, cancellationToken);
        var fileInfo = new FileInfo(zipPath);
        _logger.Info($"Snapshot local creado en {zipPath} ({fileInfo.Length / 1024.0 / 1024.0:F2} MB).");

        return new SnapshotArtifact(zipPath, checksum, fileInfo.Length);
    }

    public async Task ExtractSnapshotAsync(UserConfig config, string zipPath, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var serverFolderPath = GetServerFolderPath(config);
            Directory.CreateDirectory(serverFolderPath);

            foreach (var directory in Directory.GetDirectories(serverFolderPath))
            {
                // Do not delete the directory that contains the configured source server.jar.
                if (!DirectoryContainsFile(directory, config.ServerJarPath))
                {
                    Directory.Delete(directory, true);
                }
            }

            foreach (var file in Directory.GetFiles(serverFolderPath))
            {
                // Do not delete the configured source server.jar.
                if (!IsFilePath(file, config.ServerJarPath))
                {
                    File.Delete(file);
                }
            }

            ZipFile.ExtractToDirectory(zipPath, serverFolderPath, true);
            var targetJarPath = Path.Combine(serverFolderPath, "server.jar");
            
            // Copy only when the configured source JAR is outside the target server folder.
            if (!IsFilePath(targetJarPath, config.ServerJarPath))
            {
                // Retry copy to handle transient file locks from external processes.
                CopyFileWithRetry(config.ServerJarPath, targetJarPath, 3);
            }

            if (config.AutoAcceptEula)
            {
                var eulaPath = Path.Combine(serverFolderPath, "eula.txt");
                File.WriteAllText(eulaPath, "eula=true" + Environment.NewLine);
            }
        }, cancellationToken);
    }

    public Task<LocalWorldState> LoadLocalStateAsync(CancellationToken cancellationToken = default) =>
        _localWorldStateStore.LoadAsync(cancellationToken);

    public Task MarkLocalStateAsync(int worldVersion, string? checksum, CancellationToken cancellationToken = default) =>
        _localWorldStateStore.SaveAsync(
            new LocalWorldState
            {
                WorldVersion = worldVersion,
                WorldChecksum = checksum,
                LastSyncedAtUtc = DateTimeOffset.UtcNow
            },
            cancellationToken);

    private static IEnumerable<string> EnumerateFilesToArchive(string rootDirectory)
    {
        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(rootDirectory, file);
            var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (segments.Any(segment => IgnoredDirectories.Contains(segment)))
            {
                continue;
            }

            if (IgnoredFiles.Contains(Path.GetFileName(file)))
            {
                continue;
            }

            yield return file;
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static bool FilesAreIdentical(string sourcePath, string targetPath)
    {
        try
        {
            var sourceInfo = new FileInfo(sourcePath);
            var targetInfo = new FileInfo(targetPath);

            return sourceInfo.Length == targetInfo.Length &&
                   sourceInfo.LastWriteTimeUtc == targetInfo.LastWriteTimeUtc;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFilePath(string path1, string path2)
    {
        return Path.GetFullPath(path1).Equals(Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);
    }

    private static bool DirectoryContainsFile(string directory, string filePath)
    {
        var fullDirPath = Path.GetFullPath(directory);
        var fullFilePath = Path.GetFullPath(filePath);
        return fullFilePath.StartsWith(fullDirPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetServerFolderPath(UserConfig config)
    {
        var fullJarPath = Path.GetFullPath(config.ServerJarPath);
        var serverFolderPath = Path.GetDirectoryName(fullJarPath);

        if (string.IsNullOrWhiteSpace(serverFolderPath))
        {
            throw new DirectoryNotFoundException("No se pudo determinar la carpeta del servidor desde ServerJarPath.");
        }

        return serverFolderPath;
    }

    private static void CopyFileWithRetry(string source, string destination, int maxRetries)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                File.Copy(source, destination, true);
                return;
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                attempt++;
                System.Threading.Thread.Sleep(100 * attempt); // Progressive backoff between retries.
            }
        }
    }
}
