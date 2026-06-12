// Copyright � - 05/10/2025 - Toby Hunter
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Implementations;
using ServerStatusCommon.Models;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusSite.Components;

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

            var builder = WebApplication.CreateBuilder(args);

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Created Builder");

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            SharedSettingsModel sharedSettings = new();

            builder.Configuration.Bind(
                "AppSettings",
                sharedSettings);

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Loaded Configuration");

            builder.Services.AddSingleton(sharedSettings);
            builder.Services.AddSingleton<ILoggerService, LoggerServiceWrapper>();
            builder.Services.AddSingleton<IClock, SystemClockProvider>();
            builder.Services.AddSingleton<IFileSystem, FileSystemWrapper>();
            builder.Services.AddSingleton<IAPIClient, APIClientWrapper>();
            builder.Services.AddSingleton<IHTTPClient, HTTPClientWrapper>();
            builder.Services.AddSingleton<IRetryService, RetryService>();
            builder.Services.AddSingleton<APIService>();
            builder.Services.AddScoped<UserModel>();
            builder.Services.AddHttpContextAccessor();

            _logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                "Configured Services");

            var app = builder.Build();

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
