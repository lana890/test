using EskaCMS.Search.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OcrService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly OcrClientAccessor _accessor;

    public DocumentController(OcrClientAccessor accessor)
    {
        _accessor = accessor;
    }

    // ── GET /api/document/health ─────────────────────────────────────────────
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var client = await _accessor.GetClientAsync();
        return Ok(await client.GetHealthAsync());
    }

    // ── POST /api/document/scan ──────────────────────────────────────────────
    // ── POST /api/document/scan ──────────────────────────────────────────────
    [HttpPost("scan")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Scan([FromForm] OcrDocumentScanRequest request)

    {
        var form = await Request.ReadFormAsync();
        var docType = ResolveDocumentType(request, form);

        if (string.IsNullOrWhiteSpace(docType))
        {
            return BadRequest(new
            {
                success = false,
                error = "Invalid DocumentType. Send 1 (or 'passport') for Passport, 2 (or 'national_id') for NationalId, or 3 (or 'vehicle_license') for Vehicle.",
                receivedDocumentType = request.DocumentType.ToString(),
                formKeys = form.Keys.ToArray(),
                formFiles = form.Files.Select(f => new { f.Name, f.FileName, f.Length }).ToArray()
            });
        }

        var tempFiles = new List<string>();

        try
        {
            // ── PASSPORT ──────────────────────────────────────────────────────
            if (docType == "passport")
            {
                var file = ResolveFile(
                    request.File,
                    form,
                    "File", "file",
                    "Image", "image",
                    "Passport", "passport",
                    "PassportImage", "passportImage", "passport_image");

                // Last-resort: grab first file in form
                if (!HasFile(file) && form.Files.Count >= 1)
                    file = form.Files[0];

                if (!HasFile(file))
                    return BadRequest(MissingFileError("Passport OCR requires one file upload.", form));

                var tempPath = await SaveTempImageAsync(file!);
                tempFiles.Add(tempPath);
                EnsureTempFileReady(tempPath);

                var passportClient = await _accessor.GetClientAsync();
                var result = await passportClient.ScanPassportRawAsync(tempPath);
                return OcrJson(result);
            }
            // ── vehicle_license ──────────────────────────────────────────────────────
            else if (docType == "vehicle_license")
            {
                var front_image = ResolveFile(
                request.Front,
                form,
                "Front", "front",
                "FrontFile", "frontFile", "front_file",
                "FrontImage", "frontImage", "front_image");

                var back_image = ResolveFile(
                    request.Back,
                    form,
                    "Back", "back",
                    "BackFile", "backFile", "back_file",
                    "BackImage", "backImage", "back_image");

                // Last-resort: positional fallback when files are present but field names are unknown
                if (!HasFile(front_image) && form.Files.Count >= 1)
                    front_image = form.Files[0];

                if (!HasFile(back_image) && form.Files.Count >= 2)
                    back_image = form.Files[1];

                if (!HasFile(front_image))
                    return BadRequest(MissingFileError("Passport OCR requires Front and Back file uploads.", form));

                var frontImageTempPath = await SaveTempImageAsync(front_image!);
                string? backImageTempPath = null;
                if (back_image != null)
                {
                    backImageTempPath = await SaveTempImageAsync(back_image!);
                    tempFiles.Add(backImageTempPath);
                    EnsureTempFileReady(backImageTempPath);

                }
                tempFiles.Add(frontImageTempPath);
                EnsureTempFileReady(frontImageTempPath);

                var id_Client = await _accessor.GetClientAsync();
                var id_Result = await id_Client.ScanVehicleRawAsync(frontImageTempPath, backImageTempPath);
                return OcrJson(id_Result);
            }

            // ── NATIONAL ID ───────────────────────────────────────────────────
            var front = ResolveFile(
                request.Front,
                form,
                "Front", "front",
                "FrontFile", "frontFile", "front_file",
                "FrontImage", "frontImage", "front_image");

            var back = ResolveFile(
                request.Back,
                form,
                "Back", "back",
                "BackFile", "backFile", "back_file",
                "BackImage", "backImage", "back_image");

            // Last-resort: positional fallback when files are present but field names are unknown
            if (!HasFile(front) && form.Files.Count >= 1)
                front = form.Files[0];

            if (!HasFile(back) && form.Files.Count >= 2)
                back = form.Files[1];

            if (!HasFile(front))
                return BadRequest(MissingFileError("NationalId OCR requires Front and Back file uploads.", form));

            var frontTempPath = await SaveTempImageAsync(front!);
            tempFiles.Add(frontTempPath);
            EnsureTempFileReady(frontTempPath);

            string backTempPath = null;

            if (back != null)
            {
                backTempPath = await SaveTempImageAsync(back!);
                tempFiles.Add(backTempPath);
                EnsureTempFileReady(backTempPath);

            }

            var idClient = await _accessor.GetClientAsync();
            var idResult = await idClient.ScanNationalIdRawAsync(frontTempPath, backTempPath);
            return OcrJson(idResult);
        }
        finally
        {
            foreach (var path in tempFiles)
                DeleteTempFile(path);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the first valid (non-null, Length > 0) file among:
    ///   1. <paramref name="fromModel"/> (model-bound property)
    ///   2. Named fields looked up in <paramref name="form"/>, in order
    /// </summary>
    private static IFormFile? ResolveFile(
        IFormFile? fromModel,
        IFormCollection form,
        params string[] names)
    {
        if (HasFile(fromModel))
            return fromModel;

        foreach (var name in names)
        {
            var file = form.Files.GetFile(name);
            if (HasFile(file))
                return file;
        }

        return null;
    }

    private static string? ResolveDocumentType(OcrDocumentScanRequest request, IFormCollection form)
    {
        var raw = GetFormValue(form, "DocumentType", "documentType", "docType", "Type", "type");
        var normalized = !string.IsNullOrWhiteSpace(raw) ? NormalizeDocType(raw) : null;

        var hasFront = HasFile(request.Front) || HasAnyFile(form,
            "Front", "front", "FrontFile", "frontFile", "front_file", "FrontImage", "frontImage", "front_image");
        var hasBack = HasFile(request.Back) || HasAnyFile(form,
            "Back", "back", "BackFile", "backFile", "back_file", "BackImage", "backImage", "back_image");
        var hasSingleFile = HasFile(request.File) || HasAnyFile(form,
            "File", "file", "Image", "image", "Passport", "passport", "PassportImage", "passportImage", "passport_image");

        // Front+Back uploads are a two-sided card flow. This avoids sending national IDs
        // through the passport parser when a client accidentally posts DocumentType=1.
        if (hasFront && hasBack && !hasSingleFile &&
            (normalized == "passport" || (normalized == null && request.DocumentType == OcrDocumentType.Passport)))
            return "national_id";

        if (!string.IsNullOrWhiteSpace(normalized))
            return normalized;

        return request.DocumentType switch
        {
            OcrDocumentType.Passport => "passport",
            OcrDocumentType.NationalId => "national_id",
            OcrDocumentType.Vehicle => "vehicle_license",
            _ => null
        };
    }

    private static object MissingFileError(string message, IFormCollection form) => new
    {
        success = false,
        error = message,
        receivedFiles = form.Files.Select(f => new
        {
            field = f.Name,
            fileName = f.FileName,
            length = f.Length,
            contentType = f.ContentType
        }).ToArray()
    };

    private static string CreateTempImagePath(IFormFile file)
    {
        var tempDir = Path.Combine(AppContext.BaseDirectory, "OCRTemp");
        Directory.CreateDirectory(tempDir);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
            ext = ".jpg";

        return Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");
    }

    private static async Task<string> SaveTempImageAsync(IFormFile file)
    {
        var tempPath = CreateTempImagePath(file);
        await using var fs = System.IO.File.Create(tempPath);
        await file.CopyToAsync(fs);
        // fs is disposed (flushed & closed) here before the path is used
        return tempPath;
    }

    private static void EnsureTempFileReady(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length == 0)
            throw new InvalidOperationException(
                $"OCR temp image was not saved correctly: {path}");
    }

    private static void DeleteTempFile(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup; log if you have a logger injected
        }
    }

    private static IFormFile? GetFile(IFormCollection form, params string[] names)
    {
        foreach (var name in names)
        {
            var file = form.Files.GetFile(name);
            if (HasFile(file)) return file;
        }
        return null;
    }

    private static bool HasFile(IFormFile? file) =>
        file != null && file.Length > 0;

    private static bool HasAnyFile(IFormCollection form, params string[] names)
    {
        foreach (var name in names)
        {
            if (HasFile(form.Files.GetFile(name)))
                return true;
        }
        return false;
    }
    private static string? GetFormValue(IFormCollection form, params string[] names)
    {
        foreach (var name in names)
        {
            var value = form[name].ToString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static string NormalizeDocType(string raw)
    {
        var value = raw.Trim().ToLowerInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");

        return value switch
        {
            "1" or "passport" or "passports" or "td3"
                => "passport",

            "2" or "nationalid" or "national_id" or "national_id_card"
                or "nationalidcard" or "id" or "id_card"
                or "identity" or "jordan_id" or "jordanian_id"
                or "td1"
                => "national_id",

            "3" or "vehicle" or "vehicle_license" or "car"
            or "car_license" or "car_card"
            => "vehicle_license",

            _ => string.Empty
        };
    }

    private static ContentResult OcrJson(System.Text.Json.JsonElement result)
    {
        return new ContentResult
        {
            Content = result.GetRawText(),
            ContentType = "application/json",
            StatusCode = IsSuccess(result)
                ? StatusCodes.Status200OK
                : StatusCodes.Status400BadRequest
        };
    }

    private static bool IsSuccess(System.Text.Json.JsonElement result) =>
        result.TryGetProperty("success", out var success)
        && success.ValueKind == System.Text.Json.JsonValueKind.True;
}
