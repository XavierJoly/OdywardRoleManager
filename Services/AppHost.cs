using System;
using System.IO;
using _0900_OdywardRoleManager.Utils;
using _0900_OdywardRoleManager.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace _0900_OdywardRoleManager.Services;

public static class AppHost
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object SyncRoot = new();

    public static void EnsureInitialized()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_serviceProvider is not null)
            {
                return;
            }

            var configuration = BuildConfiguration();
            ConfigureLogging(configuration);

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            var azureOptions = configuration.GetSection("AzureAd").Get<AzureAdOptions>() ?? new AzureAdOptions();
            var graphOptions = configuration.GetSection("Graph").Get<GraphOptions>() ?? new GraphOptions();

            azureOptions.Validate();
            graphOptions.Validate();

            services.AddSingleton(azureOptions);
            services.AddSingleton(graphOptions);
            services.AddSingleton<ILogger>(sp => Log.Logger);
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<RoleCatalog>();
            services.AddSingleton<ExportService>();
            services.AddSingleton<GraphService>();
            services.AddSingleton<MainWindowViewModel>();

            _serviceProvider = services.BuildServiceProvider();
        }
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        EnsureInitialized();
        return _serviceProvider!.GetRequiredService<T>();
    }

    public static void Shutdown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Log.CloseAndFlush();
        _serviceProvider = null;
    }

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        return builder.Build();
    }

    private static void ConfigureLogging(IConfiguration configuration)
    {
        var logsDirectory = Path.Combine(AppContext.BaseDirectory, Constants.AuditDirectoryName);
        Directory.CreateDirectory(logsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", "OdywardRoleManager")
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logsDirectory, "odyward-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();
    }
}
