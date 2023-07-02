using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ConfigTryout;

public static class Program
{
    private static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        HostBuilderContext? hostBuilderContext = null;

        return new HostBuilder()
            .ConfigureHostConfiguration(configHost =>
            {
                configHost
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", reloadOnChange: false, optional: false)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureAppConfiguration((hostContext, configApp) =>
            {
                configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", reloadOnChange: false, optional: false);
                
                configApp.AddSqlDatabase(sqlConfig =>
                {
                    sqlConfig.ConnectionString = hostContext.Configuration.GetConnectionString("ConfigDb");
                    sqlConfig.RefreshInterval = TimeSpan.FromSeconds(10);
                    sqlConfig.Prefix = "WorkerSettings";
                });
                
                configApp.AddAzureBlobJson(blobConfig =>
                {
                    blobConfig.ConnectionString = hostContext.Configuration.GetConnectionString("Storage");
                    blobConfig.RefreshInterval = TimeSpan.FromSeconds(10);
                    blobConfig.BlobPath = "content/Config/WorkerSettings.json";
                });
                
                hostBuilderContext = hostContext;
            })
            .UseDefaultServiceProvider(config =>
            {
                var validate = hostBuilderContext?.HostingEnvironment.IsDevelopment() ?? false;
                config.ValidateScopes = validate;
                config.ValidateOnBuild = validate;
            })
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .Configure<WorkerSettings>(hostContext.Configuration.GetSection("WorkerSettings"))
                    .AddHostedService<Worker>();
            });
    }
}