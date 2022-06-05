using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ConfigTryout;

public class Worker : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<WorkerSettings> _monitorSettings;
    private Timer? _timer;
    
    public Worker(IConfiguration configuration, IOptionsMonitor<WorkerSettings> monitorSettings)
    {
        _configuration = configuration;
        _monitorSettings = monitorSettings;
        _timer = new Timer(_ => OnTimerPing(), null, Timeout.Infinite, Timeout.Infinite);
    }

    private void OnTimerPing()
    {
        Console.WriteLine("Timer ping: Configuration.IsEnabled:{0}, Monitor.IsEnabled: {1}, Configuration.Prop2:{2}, Monitor.Prop2: {3}",
            _configuration["WorkerSettings:IsEnabled"],
            _monitorSettings.CurrentValue.IsEnabled,
            _configuration["WorkerSettings:NestedSettings:Prop2"],
            _monitorSettings.CurrentValue.NestedSettings.Prop2);
        
        // Console.WriteLine("Configuration.ArrayProp:{0}, Monitor.ArrayProp: {1}",
        //     _configuration["WorkerSettings:NestedSettings:ArrayProp"],
        //     string.Join(", ", _monitorSettings.CurrentValue.NestedSettings.ArrayProp));

        // Console.WriteLine("Configuration.GuidList:{0}, Monitor.GuidList: {1}",
        //     _configuration["WorkerSettings:NestedSettings:GuidList"],
        //     string.Join(", ", _monitorSettings.CurrentValue.NestedSettings.GuidList));
        //
        // Console.WriteLine("Configuration.DicProp:{0}, Monitor.DicProp: {1}",
        //     _configuration["WorkerSettings:NestedSettings:DicProp"],
        //     JsonSerializer.Serialize(_monitorSettings.CurrentValue.NestedSettings.DicProp));

    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting worker");
        _timer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Stopping worker");
        _timer.Dispose();
        return Task.CompletedTask;
    }
}