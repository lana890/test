using Microsoft.Extensions.Configuration;
using OcrService;
using System;
using System.Threading;
using System.Threading.Tasks;

public class OcrClientAccessor : IAsyncDisposable
{
    private readonly OcrServiceManager _manager;
    private readonly string _serviceUrl;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private OcrClient? _client;
    private bool _useOllama;
    public OcrClientAccessor(IConfiguration config)
    {
        _serviceUrl = config["OCRService:ServiceUrl"] ?? "http://127.0.0.1:5050";
        _useOllama = config.GetValue<bool>("OCRService:OllamaEngine");
        _manager = new OcrServiceManager(
            pythonExe: config["OCRService:PythonPath"]!,
            scriptPath: config["OCRService:ScriptPath"]!,
            serviceUrl: _serviceUrl,
            startupTimeoutMs: int.Parse(config["OCRService:TimeoutSeconds"] ?? "90") * 1000,
            useOllama: _useOllama
        );
    }

    public async Task<OcrClient> GetClientAsync(CancellationToken ct = default)
    {
        if (_client != null) return _client;

        await _lock.WaitAsync(ct);
        try
        {
            if (_client != null) return _client;

            var tempClient = new OcrClient(_serviceUrl, _useOllama);

            if (await tempClient.IsHealthyAsync(ct))
            {
                Console.WriteLine(" Resing already-running OCR service.");
                _client = tempClient;
                return _client;
            }

            // Not runningstart it automatically
            Console.WriteLine(" Python not running, starting it...");
            _client = await _manager.StartAsync(ct);
            return _client;
        }
        catch (Exception ex)
        {
            _client = null;
            throw new InvalidOperationException($"OCR service failed: {ex.Message}", ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    public OcrClient GetClient() => _client
        ?? throw new InvalidOperationException("OCR service not started yet. Call GetClientAsync first.");

    public async ValueTask DisposeAsync() => await _manager.DisposeAsync();
}
