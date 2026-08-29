// Copyright � - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Implementations;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Components;
using ServerStatusSite.Implementations;
using ServerStatusSite.Models;
using ServerStatusSite.Services;

namespace ServerStatusSite
{
    public class Program
    {
        // Configures the application at startup.
        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo(Path.Combine(
                AppContext.BaseDirectory,
                "log4net.config")));

            ILoggerService _logger = new LoggerServiceWrapper();

            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Starting Website");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Created Builder");

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            SharedSettingsModel sharedSettings = new();

            builder.Configuration.Bind(
                "AppSettings",
                sharedSettings);

            BackupToolSettingsModel backupToolSettings = builder.Configuration.GetSection("BackupToolAPI")
                .Get<BackupToolSettingsModel>()!;

            builder.Services.AddSingleton(backupToolSettings);

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Loaded Configuration");

            builder.Services.AddSingleton(sharedSettings);
            builder.Services.AddSingleton(backupToolSettings);
            builder.Services.AddSingleton<ILoggerService, LoggerServiceWrapper>();
            builder.Services.AddSingleton<IClock, SystemClockProvider>();
            builder.Services.AddSingleton<IFileSystem, FileSystemWrapper>();
            builder.Services.AddSingleton<IAPIClient, APIClientWrapper>();
            builder.Services.AddSingleton<IHTTPClient, HTTPClientWrapper>();
            builder.Services.AddSingleton<IBackupToolAPIClient, BackupToolAPIClientWrapper>();
            builder.Services.AddSingleton<RetryService>();
            builder.Services.AddSingleton<APIService>();
            builder.Services.AddSingleton<LogStreamService>();
            builder.Services.AddSingleton<BackupToolAPIService>();
            builder.Services.AddScoped<UserModel>();

            builder.Services.AddControllers();

            builder.Services.AddHttpContextAccessor();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured Services");

            WebApplication app = builder.Build();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Built Application");

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured HTTPS Redirection");

            app.UseStaticFiles();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured Static Files");

            app.UseAntiforgery();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured Antiforgery");

            app.MapControllers();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Mapped API Controllers");

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Mapped Razor Components with Interactive Server Render Mode");
            _logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Running Website");

            app.Run();
        }
    }
}
