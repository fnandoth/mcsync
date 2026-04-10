using System.Diagnostics;
using System.Text.RegularExpressions;
using MCSync.Core;

namespace MCSync.Minecraft;

public sealed class ServerManager : IDisposable
{
    private static readonly Regex ReadyRegex = new("(Done \\(|For help, type)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AppLogger _logger;
    private Process? _process;
    private TaskCompletionSource<bool>? _readyTcs;

    public ServerManager(AppLogger logger)
    {
        _logger = logger;
    }

    public event EventHandler? ServerExited;

    public bool IsRunning => _process is { HasExited: false };

    public async Task StartAsync(UserConfig config, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("El servidor ya esta corriendo.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-Xmx{config.JavaMaxMemoryMb}M -Xms{config.JavaMinMemoryMb}M -jar server.jar nogui",
            WorkingDirectory = config.ServerFolderPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnOutputDataReceived;
        _process.Exited += OnProcessExited;

        _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_process.Start())
        {
            throw new InvalidOperationException("No fue posible iniciar el proceso de Java.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(90));
        using var _ = linkedCts.Token.Register(() => _readyTcs.TrySetCanceled(linkedCts.Token));

        await _readyTcs.Task;
    }

    public async Task StopGracefullyAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || _process is null)
        {
            return;
        }

        await SendCommandAsync("save-off");
        await SendCommandAsync("save-all flush");
        await SendCommandAsync("stop");

        var exited = _process.WaitForExit(30000);
        if (!exited)
        {
            _logger.Warning("El servidor no termino a tiempo. Se forzara el cierre.");
            await KillAsync();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    public Task KillAsync()
    {
        if (IsRunning)
        {
            _process!.Kill(true);
            _process.WaitForExit(10000);
        }

        return Task.CompletedTask;
    }

    private async Task SendCommandAsync(string command)
    {
        if (_process is null || _process.HasExited)
        {
            return;
        }

        await _process.StandardInput.WriteLineAsync(command);
        await _process.StandardInput.FlushAsync();
        _logger.Info($"> {command}");
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        _logger.Info($"[server] {e.Data}");

        if (_readyTcs is not null && ReadyRegex.IsMatch(e.Data))
        {
            _readyTcs.TrySetResult(true);
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        _readyTcs?.TrySetResult(true);
        ServerExited?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            _process.OutputDataReceived -= OnOutputDataReceived;
            _process.ErrorDataReceived -= OnOutputDataReceived;
            _process.Exited -= OnProcessExited;
            _process.Dispose();
        }
    }
}
