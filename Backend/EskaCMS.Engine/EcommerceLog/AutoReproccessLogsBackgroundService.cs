using EskaCMS.Engine;
using EskaCommerce.ShoppingCart.EcommerceLog.ModelView;
using EskaCommerce.ShoppingCart.EcommerceLog.Services.Interface;
using System.Text.Json;

public class AutoReproccessLogsBackgroundService : BaseBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private RetryConfig? _retryConfig;

    public AutoReproccessLogsBackgroundService(
        ILogger<AutoReproccessLogsBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration config)
        : base(nameof(AutoReproccessLogsBackgroundService), logger, serviceProvider, config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _retryConfig = _config.GetSection("RetryConfig").Get<RetryConfig>() ?? new RetryConfig();

    }

    public override async Task<bool> Execute()
    {
        try
        {

            if (_retryConfig == null)
            {
                _logger.LogWarning("RetryConfig not found in appsettings - skipping retry execution");
                return false;
            }

            _logger.LogInformation("Retry service executing. MaxRetries: {MaxRetries}, Interval: {Interval} minutes", _retryConfig.MaxRetry, _retryConfig.RetryIntervalMin);

            // Process failed logs
            await ProcessFailedLogsAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in retry service execution");
            throw;
        }
    }

    private async Task ProcessFailedLogsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var logService = scope.ServiceProvider.GetRequiredService<IEcommerceLogService>();

        // Get all logs eligible for retry
        var failedLogs = await logService.GetLogsForRetryAsync(_retryConfig?.MaxRetry ?? 0);

        if (!failedLogs.Any())
        {
            _logger.LogInformation("No failed logs found for retry");
            return;
        }

        _logger.LogInformation("Found {Count} logs eligible for retry", failedLogs.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (var log in failedLogs)
        {
            try
            {
                _logger.LogInformation(
                    "Retrying log {LogId} (EcomLogId: {EcomLogId}). Attempt {RetryCount}/{MaxRetries}",
                    log.Id,
                    log.EcomLogId,
                    log.EcomLog.RetryCount + 1,
                    _retryConfig?.MaxRetry);

                // Rebuild request payload from version
                var request = new RequestPayload
                {
                    Url = log.URL,
                    Method = log.Method,
                    Body = log.Request,
                    Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(log.Header)
                              ?? new Dictionary<string, string>()
                };

                // Resubmit the log
                var success = await logService.ReSubmitLogAsync(log.EcomLogId, request);

                if (success)
                {
                    successCount++;
                    _logger.LogInformation(
                        "Log {EcomLogId} retry succeeded",
                        log.EcomLogId);
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning(
                        "Log {EcomLogId} retry completed but request failed",
                        log.EcomLogId);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Exception retrying log {EcomLogId}. Attempt {RetryCount}/{MaxRetries}",
                    log.EcomLogId,
                    log.EcomLog.RetryCount,
                    _retryConfig?.MaxRetry);
            }
        }

        _logger.LogInformation("Retry cycle complete. Processed: {Total}, Succeeded: {Success}, Failed: {Failed}",
            failedLogs.Count,
            successCount,
            failureCount);
    }
}