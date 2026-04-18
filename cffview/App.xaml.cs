using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using cffview.Services;
using cffview.ViewModels;
using Serilog;

namespace cffview;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        ConfigureLogging();
        
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        Log.Information("CFF View starting...");
        
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            
            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
            };
            
            var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Loaded += async (s, args) =>
            {
                await viewModel.InitializeAsync();
            };
            
            mainWindow.Show();
            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"Erreur au démarrage: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
        
        base.OnStartup(e);
    }

    private void ConfigureLogging()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CFFView", "Logs", "cffview-.log");
        
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HttpClient>();
        
        services.AddSingleton<ITransportApiService, TransportApiService>();
        services.AddSingleton<IGtfsService, GtfsService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        
        services.AddSingleton<MainViewModel>();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled exception");
        Log.CloseAndFlush();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Dispatcher unhandled exception");
        MessageBox.Show($"Une erreur s'est produite: {e.Exception.Message}", 
            "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("CFF View shutting down");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}