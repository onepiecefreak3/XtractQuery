using CrossCutting.Core.Contract.Configuration.DataClasses;

namespace CrossCutting.Core.Logging.SerilogAdapter;

/// <summary>Configuration for the Serilog logging adapter (console and file sinks).</summary>
/// <summary_de>Konfiguration für den Serilog-Logging-Adapter (Konsolen- und Datei-Sinks).</summary_de>
[ConfigurationCategory("CrossCutting.Core.Logging.SerilogAdapter")]
public class LoggingSerilogConfiguration
{
    /// <summary>Path of the log file relative to the process working directory.</summary>
    /// <summary_de>Pfad der Logdatei relativ zum Arbeitsverzeichnis des Prozesses.</summary_de>
    /// <example>logs/iDxLog.log</example>
    public string FilePath { get; set; } = "logs/XtractQuery.log";

    /// <summary>Serilog output template used for the file sink.</summary>
    /// <summary_de>Serilog-Ausgabevorlage für den Datei-Sink.</summary_de>
    public string FileOutputTemplate { get; set; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u}|{Message:lj}{NewLine}{Exception}";

    /// <summary>Serilog output template used for the console sink.</summary>
    /// <summary_de>Serilog-Ausgabevorlage für den Konsolen-Sink.</summary_de>
    public string ConsoleOutputTemplate { get; set; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u}|{Message:lj}{NewLine}{Exception}";

    /// <summary>Minimum log level for the file sink (Verbose, Debug, Information, Warning, Error, Fatal, Off).</summary>
    /// <summary_de>Minimales Loglevel für den Datei-Sink.</summary_de>
    /// <example>Error</example>
    public string FileLogLevel { get; set; } = "Error";

    /// <summary>Minimum log level for the console sink (Verbose, Debug, Information, Warning, Error, Fatal, Off).</summary>
    /// <summary_de>Minimales Loglevel für den Konsolen-Sink.</summary_de>
    /// <example>Error</example>
    public string ConsoleLogLevel { get; set; } = "Error";

    /// <summary>Maximum size of a single log file in bytes before rolling. Null or less than or equal to zero disables size-based rolling.</summary>
    /// <summary_de>Maximale Größe einer Logdatei in Bytes vor dem Rollen. Null oder kleiner gleich 0 deaktiviert Größen-basiertes Rollen.</summary_de>
    public long? FileSizeLimitBytes { get; set; }

    /// <summary>Number of rolled log files to retain. Null or less than or equal to zero keeps an unlimited number.</summary>
    /// <summary_de>Anzahl der zu behaltenden gerollten Logdateien. Null oder kleiner gleich 0 behält unbegrenzt viele.</summary_de>
    public int? RetainedFileCountLimit { get; set; }
}
