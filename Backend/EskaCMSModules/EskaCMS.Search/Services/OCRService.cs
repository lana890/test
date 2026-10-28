using EskaCMS.Search.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OcrService;

public class OcrClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _engine; // "mrz+visual+ollama" or "easyocr"

    public OcrClient(string baseUrl = "http://127.0.0.1:5050", bool useOllama = false)
    {
        _engine = useOllama ? "mrz+visual+ollama" : "easyocr";
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/health", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<JsonElement> GetHealthAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/health", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    public async Task<PassportResponse> ScanPassportAsync(
        string imagePath, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["image_path"] = imagePath,
            ["engine"] = _engine
        };
        var content = Serialize(payload);
        var resp = await _http.PostAsync("/ocr/passport", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PassportResponse>(ct)
               ?? throw new InvalidOperationException("Empty response from OCR service");
    }

    public async Task<JsonElement> ScanPassportRawAsync(
        string imagePath, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["image_path"] = imagePath,
            ["engine"] = _engine
        };
        var content = Serialize(payload);
        var resp = await _http.PostAsync("/ocr/passport", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    public async Task<NationalIdResponse> ScanNationalIdAsync(
        string frontPath, string backPath, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["front_path"] = frontPath,
            ["back_path"] = backPath,
            ["image_paths"] = new[] { frontPath, backPath },
            ["engine"] = _engine
        };
        var content = Serialize(payload);
        var resp = await _http.PostAsync("/ocr/id", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<NationalIdResponse>(ct)
               ?? throw new InvalidOperationException("Empty response from OCR service");
    }

    public async Task<JsonElement> ScanNationalIdRawAsync(
        string frontPath, string backPath, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["doc_type"] = "national_id",
            ["front_path"] = frontPath,
            ["back_path"] = backPath,
            ["image_paths"] = new[] { frontPath, backPath },
            ["engine"] = _engine
        };
        var content = Serialize(payload);
        var resp = await _http.PostAsync("/ocr/id", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    public async Task<VehicleResponse> ScanVehicleAsync(
        string frontPath, string backPath, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["front_path"] = frontPath,
            ["back_path"] = backPath,
            ["image_paths"] = new[] { frontPath, backPath },
            ["engine"] = _engine
        };
        var content = Serialize(payload);
        var resp = await _http.PostAsync("/ocr/vehicle", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<VehicleResponse>(ct)
               ?? throw new InvalidOperationException("Empty response from OCR service");
    }

    public async Task<JsonElement> ScanVehicleRawAsync(
        string frontPath, string backPath, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["doc_type"] = "vehicle",
            ["front_path"] = frontPath,
            ["back_path"] = backPath,
            ["image_paths"] = new[] { frontPath, backPath },
            ["engine"] = _engine
        };
        var content = Serialize(payload);
        var resp = await _http.PostAsync("/ocr/vehicle", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    public async Task<JsonElement> ScanDocumentAsync(
        string docType,
        string? imagePath = null,
        string? frontPath = null,
        string? backPath = null,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["doc_type"] = docType,
            ["image_path"] = imagePath,
            ["front_path"] = frontPath,
            ["back_path"] = backPath,
            ["engine"] = _engine
        };
        var content = Serialize(payload);
        var resp = await _http.PostAsync("/ocr", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    private static StringContent Serialize(Dictionary<string, object?> payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    public void Dispose() => _http.Dispose();
}