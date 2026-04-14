using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCSync.Core;

namespace MCSync.GitHub;

public sealed class GitHubClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly HttpClient _httpClient = new();
    private readonly AppLogger _logger;

    public GitHubClient(AppLogger logger)
    {
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MCSync", "0.1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    }

    public async Task<GitHubFileResult<T>?> GetJsonAsync<T>(UserConfig config, string path, CancellationToken cancellationToken = default)
    {
        var file = await GetFileAsync(config, path, cancellationToken);
        if (file is null)
        {
            return null;
        }

        var model = JsonSerializer.Deserialize<T>(file.TextContent, JsonOptions);
        return model is null ? null : new GitHubFileResult<T>(file.Sha, model);
    }

    public async Task<GitHubFileResult<byte[]>?> GetBytesAsync(UserConfig config, string path, CancellationToken cancellationToken = default)
    {
        var file = await GetFileAsync(config, path, cancellationToken);
        if (file is null)
        {
            return null;
        }

        // GitHub Contents API may return encoding=none for larger files in JSON mode.
        // In that case we fetch raw bytes to avoid truncated/empty snapshots.
        if (string.Equals(file.Encoding, "none", StringComparison.OrdinalIgnoreCase))
        {
            var rawContent = await GetRawBytesAsync(config, path, cancellationToken);
            return new GitHubFileResult<byte[]>(file.Sha, rawContent);
        }

        return new GitHubFileResult<byte[]>(file.Sha, file.RawContent);
    }

    public async Task<GitHubWriteResult> PutJsonAsync<T>(
        UserConfig config,
        string path,
        T content,
        string message,
        string? sha,
        CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(content, JsonOptions));
        return await PutBytesAsync(config, path, bytes, message, sha, cancellationToken);
    }

    public async Task<GitHubWriteResult> PutBytesAsync(
        UserConfig config,
        string path,
        byte[] content,
        string message,
        string? sha,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Put, BuildContentsUriWithoutRef(config, path), config.GetGitHubToken());
        var payload = new
        {
            message,
            content = Convert.ToBase64String(content),
            sha,
            branch = config.GitHubBranch
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
        {
            throw new GitHubConflictException($"GitHub rechazo la actualizacion de {path} por conflicto de version.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub z respondio {(int)response.StatusCode} al actualizar {path}: {body}");
        }

        var parsed = JsonSerializer.Deserialize<GitHubPutResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Respuesta invalida de GitHub al actualizar contenido.");

        _logger.Info($"GitHub actualizo {path}.");
        return new GitHubWriteResult(parsed.Content.Sha, parsed.Content.Path);
    }

    private async Task<GitHubRawFile?> GetFileAsync(UserConfig config, string path, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, BuildContentsUri(config, path), config.GetGitHubToken());
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub respondio {(int)response.StatusCode} al leer {path}: {body}");
        }

        var parsed = JsonSerializer.Deserialize<GitHubGetResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Respuesta invalida de GitHub al leer contenido.");

        var normalized = (parsed.Content ?? string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
        var bytes = string.Equals(parsed.Encoding, "base64", StringComparison.OrdinalIgnoreCase)
            ? Convert.FromBase64String(normalized)
            : Array.Empty<byte>();

        return new GitHubRawFile(parsed.Sha, parsed.Path, parsed.Encoding ?? string.Empty, bytes, Encoding.UTF8.GetString(bytes));
    }

    private async Task<byte[]> GetRawBytesAsync(UserConfig config, string path, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, BuildContentsUri(config, path), config.GetGitHubToken());
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"No se encontro el contenido remoto {path}.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"GitHub respondio {(int)response.StatusCode} al leer contenido crudo de {path}: {body}");
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, string token)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    
    private static Uri BuildContentsUri(UserConfig config, string path)
    {
        var owner = Uri.EscapeDataString(config.GitHubOwner);
        var repo = Uri.EscapeDataString(config.GitHubRepo);
        var encodedPath = string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
        var branch = Uri.EscapeDataString(config.GitHubBranch);
        return new Uri($"https://api.github.com/repos/{owner}/{repo}/contents/{encodedPath}?ref={branch}");
    }
    // GET /contents/{path} with ref query param is not supported for PUT requests, so we need to build the URI without the ref for PUT operations
    private static Uri BuildContentsUriWithoutRef(UserConfig config, string path)
    {
        var owner = Uri.EscapeDataString(config.GitHubOwner);
        var repo = Uri.EscapeDataString(config.GitHubRepo);
        var encodedPath = string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
        return new Uri($"https://api.github.com/repos/{owner}/{repo}/contents/{encodedPath}");
    }

    private sealed record GitHubGetResponse(string Sha, string Path, string Content, string? Encoding);

    private sealed record GitHubPutResponse(GitHubPutContent Content);

    private sealed record GitHubPutContent(string Sha, string Path);

    private sealed record GitHubRawFile(string Sha, string Path, string Encoding, byte[] RawContent, string TextContent);
}

public sealed record GitHubFileResult<T>(string Sha, T Content);

public sealed record GitHubWriteResult(string Sha, string Path);

public sealed class GitHubConflictException : Exception
{
    public GitHubConflictException(string message) : base(message)
    {
    }
}
