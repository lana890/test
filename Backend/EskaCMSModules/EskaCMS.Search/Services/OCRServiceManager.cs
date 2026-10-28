
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OcrService;

/// <summary>
/// Manages the lifecycle of the Python OCR service process.
/// Call StartAsync() once at application startup, StopAsync() at shutdown.
/// </summary>
public class OcrServiceManager : IAsyncDisposable
{
    private Process? _process;
    private readonly string _pythonExe;
    private readonly string _scriptPath;
    private readonly string _serviceUrl;
    private readonly int _startupTimeoutMs;
    private readonly bool _useOllama;

    public OcrServiceManager(
        string pythonExe = "python",
        string scriptPath = "server.py",
        string serviceUrl = "http://127.0.0.1:5050",
        int startupTimeoutMs = 120_000,
        bool useOllama = false)
    {
        _pythonExe = pythonExe;
        _scriptPath = scriptPath;
        _serviceUrl = serviceUrl;
        _startupTimeoutMs = startupTimeoutMs;
        _useOllama = useOllama;
    }

    /// Start the Python process and wait until /health responds.
    public async Task<OcrClient> StartAsync(CancellationToken ct = default)
    {
        Console.WriteLine("â–¶  Starting Python OCR serviceâ€¦");

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExe,
            Arguments = $"\"{_scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        // Pipe Python stdout/stderr to .NET console
        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.WriteLine("[PY] " + e.Data);
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine("[PY-ERR] " + e.Data);
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Poll /health until the service is ready
        var client = new OcrClient(_serviceUrl, _useOllama);
        var deadline = DateTime.UtcNow.AddMilliseconds(_startupTimeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (await client.IsHealthyAsync(ct))
            {
                Console.WriteLine("âœ…  OCR service is ready.");
                return client;
            }
            await Task.Delay(500, ct);
        }

        throw new TimeoutException(
            $"OCR service did not become healthy within {_startupTimeoutMs} ms. " +
            "Check that EasyOCR models are downloaded and GPU/CPU is available.");
    }

    ///Gracefully stop the Python process
    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            Console.WriteLine("â¹  Stopping Python OCR serviceâ€¦");
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            catch { /* process already dead */ }
        }
        _process?.Dispose();
    }
}
