using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace UnifiedMonitoring.Services
{
    public class MonitoringBackgroundService : BackgroundService
    {
        private readonly IWalletMonitoringService _monitoringService;

        public MonitoringBackgroundService(IWalletMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _monitoringService.MonitorWalletsAsync();
                await _monitoringService.MonitorVolumesAsync();

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
