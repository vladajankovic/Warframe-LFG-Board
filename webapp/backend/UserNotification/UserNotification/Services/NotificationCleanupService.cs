using UserNotification.Repositories;

namespace UserNotification.Services
{
    public class NotificationCleanupService : BackgroundService
    {
        private const int DefaultCleanupAfterMinutes = 5;
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationCleanupService> _logger;

        public NotificationCleanupService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<NotificationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CleanExpiredNotifications(stoppingToken);

            using var timer = new PeriodicTimer(CleanupInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanExpiredNotifications(stoppingToken);
            }
        }

        private async Task CleanExpiredNotifications(CancellationToken cancellationToken)
        {
            try
            {
                var cleanupAfterMinutes = GetCleanupAfterMinutes();
                var cutoff = DateTime.UtcNow.AddMinutes(-cleanupAfterMinutes);

                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IUserNotificationsRepository>();
                var deletedCount = await repository.DeleteOlderThan(cutoff, cancellationToken);

                if (deletedCount > 0)
                {
                    _logger.LogInformation(
                        "Deleted {DeletedCount} notifications older than {CleanupAfterMinutes} minutes",
                        deletedCount,
                        cleanupAfterMinutes);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean expired notifications");
            }
        }

        private int GetCleanupAfterMinutes()
        {
            var configuredValue =
                _configuration["CLEANUP_NOTIFICATIONS_AFTER"] ??
                _configuration["Notifications:CleanupAfterMinutes"];

            if (int.TryParse(configuredValue, out var cleanupAfterMinutes) && cleanupAfterMinutes > 0)
            {
                return cleanupAfterMinutes;
            }

            return DefaultCleanupAfterMinutes;
        }
    }
}
