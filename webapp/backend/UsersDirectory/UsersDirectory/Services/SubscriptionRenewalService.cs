using UsersDirectory.Enums;
using UsersDirectory.Repositories;

namespace UsersDirectory.Services
{
    public class SubscriptionRenewalService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionRenewalService> _logger;

        public SubscriptionRenewalService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionRenewalService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RenewExpiredSubscriptionsAsync();
            }
        }

        private async Task RenewExpiredSubscriptionsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IUsersRepository>();

                var users = await repository.GetAllUsers();
                var now = DateTime.UtcNow;

                var toRenew = users
                    .Where(u => u.SubscriptionStatus == SubscriptionStatus.Active
                                && u.SubscriptionExpiryDate.HasValue
                                && u.SubscriptionExpiryDate.Value <= now)
                    .ToList();

                foreach (var user in toRenew)
                {
                    var newExpiry = user.SubscriptionExpiryDate!.Value.AddMonths(1);
                    await repository.UpdateSubscription(user.Email, SubscriptionStatus.Active, newExpiry);
                    _logger.LogInformation("Renewed subscription for {Email}, new expiry: {Expiry}", user.Email, newExpiry);
                }

                if (toRenew.Count > 0)
                {
                    _logger.LogInformation("Subscription renewal complete. Renewed {Count} subscription(s).", toRenew.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subscription renewal.");
            }
        }
    }
}
