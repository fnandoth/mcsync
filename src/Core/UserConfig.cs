using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MCSync.Core;

public sealed class UserConfig
{
    public string ClientId { get; set; } = Guid.NewGuid().ToString("N");
    public string HostDisplayName { get; set; } = Environment.UserName;
    public string GitHubOwner { get; set; } = string.Empty;
    public string GitHubRepo { get; set; } = string.Empty;
    public string GitHubBranch { get; set; } = "main";
    public string GitHubTokenProtected { get; set; } = string.Empty;
    public string StateFilePath { get; set; } = "mcsync/state.json";
    public string SnapshotFolderPath { get; set; } = "mcsync/snapshots";
    public string WorldId { get; set; } = "survival-main";
    public string ServerFolderPath { get; set; } = Path.Combine(AppPaths.AppDataDirectory, "ServerFolder");
    public string ServerJarPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "server.jar");
    public string JavaExecutablePath { get; set; } = "java";
    public string PlayitGGUrl { get; set; } = string.Empty;
    public int JavaMinMemoryMb { get; set; } = 4096;
    public int JavaMaxMemoryMb { get; set; } = 4096;
    public int LeaseTtlSeconds { get; set; } = 45;
    public int HeartbeatIntervalSeconds { get; set; } = 10;
    public bool AutoAcceptEula { get; set; } = true;

    public string GetGitHubToken()
    {
        if (string.IsNullOrWhiteSpace(GitHubTokenProtected))
        {
            return string.Empty;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(GitHubTokenProtected);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetGitHubToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            GitHubTokenProtected = string.Empty;
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(token.Trim());
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        GitHubTokenProtected = Convert.ToBase64String(protectedBytes);
    }

    public bool IsValid(out string error)
    {
        if (string.IsNullOrWhiteSpace(GitHubOwner))
        {
            error = "Falta el owner del repositorio de GitHub.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(GitHubRepo))
        {
            error = "Falta el nombre del repositorio de GitHub.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetGitHubToken()))
        {
            error = "Falta el token de GitHub.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ServerJarPath) || !File.Exists(ServerJarPath))
        {
            error = "No se encontro server.jar en la ruta configurada.";
            return false;
        }


        if (string.IsNullOrEmpty(PlayitGGUrl))
        {
            error = "Falta la URL de playit.gg.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(JavaExecutablePath))
        {
            error = "Falta la ruta del ejecutable de Java.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public UserConfig Clone() =>
        JsonSerializer.Deserialize<UserConfig>(JsonSerializer.Serialize(this)) ?? new UserConfig();
}

public static class AppPaths
{
    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCSync");

    public static string ConfigFilePath => Path.Combine(AppDataDirectory, "config.json");

    public static string LogDirectory => Path.Combine(AppDataDirectory, "logs");

    public static string LogFilePath => Path.Combine(LogDirectory, "mcsync.log");

    public static string TempDirectory => Path.Combine(AppDataDirectory, "temp");

    public static string LocalWorldStatePath => Path.Combine(AppDataDirectory, "local-world-state.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(TempDirectory);
    }
}

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<UserConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();

        if (!File.Exists(AppPaths.ConfigFilePath))
        {
            return new UserConfig();
        }

        await using var stream = File.OpenRead(AppPaths.ConfigFilePath);
        var config = await JsonSerializer.DeserializeAsync<UserConfig>(stream, JsonOptions, cancellationToken);
        return config ?? new UserConfig();
    }

    public async Task SaveAsync(UserConfig config, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        await using var stream = File.Create(AppPaths.ConfigFilePath);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);
    }
}
