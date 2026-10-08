using Avalonia;
using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Book_Shelf;

/// <summary>Provides the process entry point and application-wide startup services.</summary>
class Program
{
    internal static string ErrorLogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Book Shelf",
        "error.log");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    /// <summary>Configures process-level handlers and starts the Avalonia desktop lifetime.</summary>
    [STAThread]
    public static void Main(string[] args)
    {
        ConfigureLogging();
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>Writes an unhandled application exception to the error log.</summary>
    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        var exception = args.ExceptionObject as Exception ??
            new Exception(Convert.ToString(args.ExceptionObject));
        Log.Fatal(exception, "Unhandled exception");
    }

    /// <summary>Logs an unobserved task exception and marks it as handled.</summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Log.Error(args.Exception, "Unobserved task exception");
        args.SetObserved();
    }

    /// <summary>Configures the file-based Serilog error logger.</summary>
    private static void ConfigureLogging()
    {
        var logDirectory = Path.GetDirectoryName(ErrorLogPath)!;
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Error()
            .WriteTo.File(ErrorLogPath, shared: true)
            .CreateLogger();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    /// <summary>Creates the platform-aware Avalonia application builder.</summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
