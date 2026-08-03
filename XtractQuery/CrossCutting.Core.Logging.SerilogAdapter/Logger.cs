using System.Globalization;
using Serilog.Events;
using ILogger = CrossCutting.Core.Contract.Logging.ILogger;

namespace CrossCutting.Core.Logging.SerilogAdapter;

public sealed class Logger : ILogger
{
    private readonly Serilog.ILogger _logger;

    public bool IsTraceEnabled => _logger.IsEnabled(LogEventLevel.Verbose);
    public bool IsDebugEnabled => _logger.IsEnabled(LogEventLevel.Debug);
    public bool IsInfoEnabled => _logger.IsEnabled(LogEventLevel.Information);
    public bool IsWarnEnabled => _logger.IsEnabled(LogEventLevel.Warning);
    public bool IsErrorEnabled => _logger.IsEnabled(LogEventLevel.Error);

    public Logger(LoggingSerilogConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _logger = SerilogBootstrapper.GetOrCreate(configuration);
    }

    public void Trace(string msg) => Write(LogEventLevel.Verbose, null, msg);
    public void Trace(string msg, object arg0) => Write(LogEventLevel.Verbose, null, msg, arg0);
    public void Trace(string msg, object arg0, object arg1) => Write(LogEventLevel.Verbose, null, msg, arg0, arg1);
    public void Trace(string msg, params object[] args) => Write(LogEventLevel.Verbose, null, msg, args);

    public void Debug(string msg) => Write(LogEventLevel.Debug, null, msg);
    public void Debug(string msg, object arg0) => Write(LogEventLevel.Debug, null, msg, arg0);
    public void Debug(string msg, object arg0, object arg1) => Write(LogEventLevel.Debug, null, msg, arg0, arg1);
    public void Debug(string msg, params object[] args) => Write(LogEventLevel.Debug, null, msg, args);

    public void Info(string msg) => Write(LogEventLevel.Information, null, msg);
    public void Info(string msg, object arg0) => Write(LogEventLevel.Information, null, msg, arg0);
    public void Info(string msg, object arg0, object arg1) => Write(LogEventLevel.Information, null, msg, arg0, arg1);
    public void Info(string msg, params object[] args) => Write(LogEventLevel.Information, null, msg, args);

    public void Warn(string msg) => Write(LogEventLevel.Warning, null, msg);
    public void Warn(string msg, object arg0) => Write(LogEventLevel.Warning, null, msg, arg0);
    public void Warn(string msg, object arg0, object arg1) => Write(LogEventLevel.Warning, null, msg, arg0, arg1);
    public void Warn(string msg, params object[] args) => Write(LogEventLevel.Warning, null, msg, args);

    public void Error(string msg) => Write(LogEventLevel.Error, null, msg);
    public void Error(string msg, object arg0) => Write(LogEventLevel.Error, null, msg, arg0);
    public void Error(string msg, object arg0, object arg1) => Write(LogEventLevel.Error, null, msg, arg0, arg1);
    public void Error(string msg, params object[] args) => Write(LogEventLevel.Error, null, msg, args);

    public void Error(string msg, Exception ex) => Write(LogEventLevel.Error, ex, msg);
    public void Error(string msg, Exception ex, object arg0) => Write(LogEventLevel.Error, ex, msg, arg0);
    public void Error(string msg, Exception ex, object arg0, object arg1) => Write(LogEventLevel.Error, ex, msg, arg0, arg1);
    public void Error(string msg, Exception ex, params object[] args) => Write(LogEventLevel.Error, ex, msg, args);

    public void Error(Exception ex)
    {
        if (IsErrorEnabled)
            _logger.Write(LogEventLevel.Error, ex, "{Message}", string.Empty);
    }

    private void Write(LogEventLevel level, Exception? exception, string message)
    {
        if (!_logger.IsEnabled(level))
            return;

        _logger.Write(level, exception, "{Message}", message);
    }

    private void Write(LogEventLevel level, Exception? exception, string message, object arg0)
    {
        if (!_logger.IsEnabled(level))
            return;

        _logger.Write(level, exception, "{Message}", Format(message, arg0));
    }

    private void Write(LogEventLevel level, Exception? exception, string message, object arg0, object arg1)
    {
        if (!_logger.IsEnabled(level))
            return;

        _logger.Write(level, exception, "{Message}", Format(message, arg0, arg1));
    }

    private void Write(LogEventLevel level, Exception? exception, string message, params object[] args)
    {
        if (!_logger.IsEnabled(level))
            return;

        _logger.Write(level, exception, "{Message}", Format(message, args));
    }

    private static string Format(string message, params object[] args)
    {
        if (args.Length == 0)
            return message;

        return string.Format(CultureInfo.InvariantCulture, message, args);
    }
}
