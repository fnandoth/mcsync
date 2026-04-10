using System.Text.Json;

namespace MCSync.Core;

public sealed class LocalWorldState
{
    public int WorldVersion { get; set; }
    public string? WorldChecksum { get; set; }
    public DateTimeOffset? LastSyncedAtUtc { get; set; }
}

public sealed class LocalWorldStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<LocalWorldState> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();

        if (!File.Exists(AppPaths.LocalWorldStatePath))
        {
            return new LocalWorldState();
        }

        await using var stream = File.OpenRead(AppPaths.LocalWorldStatePath);
        var state = await JsonSerializer.DeserializeAsync<LocalWorldState>(stream, JsonOptions, cancellationToken);
        return state ?? new LocalWorldState();
    }

    public async Task SaveAsync(LocalWorldState state, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        await using var stream = File.Create(AppPaths.LocalWorldStatePath);
        await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
    }
}
