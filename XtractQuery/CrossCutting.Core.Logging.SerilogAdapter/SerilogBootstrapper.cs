using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CrossCutting.Core.Logging.SerilogAdapter;

internal static class SerilogBootstrapper
{
    private static readonly Lock Sync = new();
    private static ILogger? _logger;

    public static ILogger GetOrCreate(LoggingSerilogConfiguration configuration)
    {
        if (_logger is not null)
            return _logger;

        lock (Sync)
        {
            return _logger ??= Create(configuration);
        }
    }

    private static ILogger Create(LoggingSerilogConfiguration configuration)
    {
        LogEventLevel? fileLevel = ParseLevel(configuration.FileLogLevel);
        LogEventLevel? consoleLevel = ParseLevel(configuration.ConsoleLogLevel);

        LogEventLevel minimumLevel = LogEventLevel.Fatal;
        if (fileLevel is { } fl)
            minimumLevel = fl;
        if (consoleLevel is { } cl && cl < minimumLevel)
            minimumLevel = cl;
        if (fileLevel is null && consoleLevel is null)
            minimumLevel = LogEventLevel.Fatal;

        long? fileSizeLimit = configuration.FileSizeLimitBytes is > 0
            ? configuration.FileSizeLimitBytes
            : null;

        int? retainedFileCountLimit = configuration.RetainedFileCountLimit is > 0
            ? configuration.RetainedFileCountLimit
            : null;

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel);

        if (fileLevel is { } enabledFileLevel)
        {
            loggerConfiguration.WriteTo.Logger(file => file
                .MinimumLevel.Is(enabledFileLevel)
                .WriteTo.File(
                    path: configuration.FilePath,
                    outputTemplate: configuration.FileOutputTemplate,
                    fileSizeLimitBytes: fileSizeLimit,
                    rollOnFileSizeLimit: fileSizeLimit.HasValue,
                    retainedFileCountLimit: retainedFileCountLimit));
        }

        if (consoleLevel is { } enabledConsoleLevel)
        {
            loggerConfiguration.WriteTo.Logger(console => console
                .MinimumLevel.Is(enabledConsoleLevel)
                .WriteTo.Console(outputTemplate: configuration.ConsoleOutputTemplate));
        }

        return loggerConfiguration.CreateLogger().ForContext(Constants.SourceContextPropertyName, "XtractQuery");
    }

    private static LogEventLevel? ParseLevel(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "trace" or "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "info" or "information" => LogEventLevel.Information,
            "warn" or "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            "off" => null,
            _ => LogEventLevel.Error
        };
    }
}
