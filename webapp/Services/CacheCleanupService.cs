namespace webapp.Services;

public class CacheCleanupService(ILogger<CacheCleanupService> logger,
    IServiceScopeFactory scopeFactory, IConfiguration configuration) : BackgroundService
{
    private readonly ILogger<CacheCleanupService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(configuration.GetValue("CacheCleanup:IntervalHours", 6));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache Cleanup Service is starting. Cleanup interval: {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                _logger.LogInformation("Starting scheduled cache cleanup...");
                
                using var scope = _scopeFactory.CreateScope();
                var compilerService = scope.ServiceProvider.GetRequiredService<MonoGameCompilerService>();
                
                var success = await compilerService.CleanupOldBuildCacheAsync(hoursOld: 24);
                
                if (success)
                {
                    _logger.LogInformation("Scheduled cache cleanup completed successfully");
                }
                else
                {
                    _logger.LogWarning("Scheduled cache cleanup completed with errors");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cache Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled cache cleanup");
            }
        }

        _logger.LogInformation("Cache Cleanup Service has stopped");
    }
}