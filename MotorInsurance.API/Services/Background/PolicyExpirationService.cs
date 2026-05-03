using MotorInsurance.API.Repositories.Policy;

namespace MotorInsurance.API.Services.Background
{
    public class PolicyExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PolicyExpirationService> _logger;

        public PolicyExpirationService(IServiceScopeFactory scopeFactory, ILogger<PolicyExpirationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpirePoliciesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Policy expiration check failed");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ExpirePoliciesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPolicyRepository>();
            await repository.ExpireOutdatedAsync();
            _logger.LogInformation("Policy expiration check completed at {Time:u}", DateTime.UtcNow);
        }
    }
}
