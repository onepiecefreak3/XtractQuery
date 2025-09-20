using System;
using System.Globalization;
using NLog;
using ILogger = CrossCutting.Core.Contract.Logging.ILogger;

namespace CrossCutting.Core.Logging.NLogAdapter;

public sealed class Logger : ILogger
{
    private readonly NLog.ILogger _logger;

    public bool IsTraceEnabled => _logger.IsTraceEnabled;

    public bool IsDebugEnabled => _logger.IsDebugEnabled;

    public bool IsInfoEnabled => _logger.IsInfoEnabled;

    public bool IsWarnEnabled => _logger.IsWarnEnabled;

    public bool IsErrorEnabled => _logger.IsErrorEnabled;

    public Logger()
    {
        _logger = NLog.LogManager.GetCurrentClassLogger();
        Configuration.ConfigureLogger();
    }

    public void Trace(string msg)
    {
        if (IsTraceEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Trace, _logger.Name, msg));
    }

    public void Trace(string msg, object arg0)
    {
        if (IsTraceEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Trace, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0 }));
    }

    public void Trace(string msg, object arg0, object arg1)
    {
        if (IsTraceEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Trace, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0, arg1 }));
    }

    public void Trace(string msg, params object[] args)
    {
        if (IsTraceEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Trace, _logger.Name, CultureInfo.InvariantCulture, msg, args));
    }

    public void Debug(string msg)
    {
        if (IsDebugEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Debug, _logger.Name, msg));
    }

    public void Debug(string msg, object arg0)
    {
        if (IsDebugEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Debug, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0 }));
    }

    public void Debug(string msg, object arg0, object arg1)
    {
        if (IsDebugEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Debug, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0, arg1 }));
    }

    public void Debug(string msg, params object[] args)
    {
        if (IsDebugEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Debug, _logger.Name, CultureInfo.InvariantCulture, msg, args));
    }

    public void Info(string msg)
    {
        if (IsInfoEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Info, _logger.Name, msg));
    }

    public void Info(string msg, object arg0)
    {
        if (IsInfoEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Info, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0 }));
    }

    public void Info(string msg, object arg0, object arg1)
    {
        if (IsInfoEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Info, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0, arg1 }));
    }

    public void Info(string msg, params object[] args)
    {
        if (IsInfoEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Info, _logger.Name, CultureInfo.InvariantCulture, msg, args));
    }

    public void Warn(string msg)
    {
        if (IsWarnEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Warn, _logger.Name, msg));
    }

    public void Warn(string msg, object arg0)
    {
        if (IsWarnEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Warn, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0 }));
    }

    public void Warn(string msg, object arg0, object arg1)
    {
        if (IsWarnEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Warn, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0, arg1 }));
    }

    public void Warn(string msg, params object[] args)
    {
        if (IsWarnEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Warn, _logger.Name, CultureInfo.InvariantCulture, msg, args));
    }

    public void Error(string msg)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, msg));
    }

    public void Error(string msg, object arg0)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0 }));
    }

    public void Error(string msg, object arg0, object arg1)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0, arg1 }));
    }

    public void Error(string msg, params object[] args)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, CultureInfo.InvariantCulture, msg, args));
    }

    public void Error(string msg, Exception ex)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, msg) { Exception = ex });
    }

    public void Error(string msg, Exception ex, object arg0)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0 }) { Exception = ex });
    }

    public void Error(string msg, Exception ex, object arg0, object arg1)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, CultureInfo.InvariantCulture, msg, new[] { arg0, arg1 }) { Exception = ex });
    }

    public void Error(string msg, Exception ex, params object[] args)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, CultureInfo.InvariantCulture, msg, args) { Exception = ex });
    }

    public void Error(Exception ex)
    {
        if (IsErrorEnabled)
            _logger.Log(typeof(Logger), new LogEventInfo(LogLevel.Error, _logger.Name, string.Empty) { Exception = ex });
    }
}