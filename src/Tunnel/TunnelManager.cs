using System.Diagnostics;
using System.Text.RegularExpressions;
using MCSync.Core;

namespace MCSync.Tunnel;

public sealed class TunnelManager : IDisposable
{

    private readonly AppLogger _logger;
    private Process? _process;
    private TaskCompletionSource<string?>? _addressTcs;

    public TunnelManager(AppLogger logger)
    {
        _logger = logger;
    }

    public bool IsRunning => _process is { HasExited: false };

    public async Task<string?> StartAsync(UserConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.PlayitGGUrl))
        {
            throw new InvalidOperationException("No se configuro la URL de playit.gg.");
        }

        if (IsRunning)
        {
            return null;
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "playit",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _addressTcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _process.OutputDataReceived += OnDataReceived;
        _process.ErrorDataReceived += OnDataReceived;

        if (!_process.Start())
        {
            throw new InvalidOperationException("No fue posible iniciar playit-cli.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(45));
        using var _ = linkedCts.Token.Register(() => _addressTcs.TrySetResult(null));

        var address = config.PlayitGGUrl;
        if (string.IsNullOrWhiteSpace(address))
        {
            _logger.Warning("playit-cli no reporto una direccion publica dentro del tiempo esperado.");
        }

        return address;
    }

    public Task StopAsync()
    {
        if (IsRunning)
        {
            _process!.Kill(true);
            _process.WaitForExit(5000);
        }

        return Task.CompletedTask;
    }

    private void OnDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        _logger.Info($"[playit] {e.Data}");
        
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            _process.OutputDataReceived -= OnDataReceived;
            _process.ErrorDataReceived -= OnDataReceived;
            _process.Dispose();
        }
    }
}
