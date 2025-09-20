using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using NLog.Config;
using NLog.Targets;

namespace CrossCutting.Core.Logging.NLogAdapter;

public class Configuration
{
    private static string s_configFileName = "config.json";

    public static void ConfigureLogger()
    {
        LoggingConfiguration config = new NLog.Config.LoggingConfiguration();
        FileTarget fileLogTarget = new NLog.Targets.FileTarget();
        fileLogTarget.Layout = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "FileLayout", "${longdate}|${level:uppercase=true}|${logger}|${message}");
        fileLogTarget.FileName = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "FileName", "logs/iDxLog.log");
        fileLogTarget.ArchiveFileName = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "ArchiveFileName", "logs/iDxLog.{#}.log");
        fileLogTarget.ArchiveAboveSize = GetValue<long>("CrossCutting.Core.Logging.NLogAdapter", "ArchiveAboveSize", -1);
        fileLogTarget.MaxArchiveFiles = GetValue<int>("CrossCutting.Core.Logging.NLogAdapter", "MaxArchiveFiles", 0);
        fileLogTarget.KeepFileOpen = GetValue<bool>("CrossCutting.Core.Logging.NLogAdapter", "KeepFileOpen", true);
        string encoding = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "Encoding", "utf8");
        fileLogTarget.Encoding = StringToEncoding(encoding);
        string archiveNumbering = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "ArchiveNumbering", "DateAndSequence");
        fileLogTarget.ArchiveNumbering = StringToArchiveNumberingMode(archiveNumbering);
        fileLogTarget.ArchiveDateFormat = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "ArchiveDateFormat", "yyyy-MM-dd");

        ColoredConsoleTarget consoleLogTarget = new NLog.Targets.ColoredConsoleTarget();
        consoleLogTarget.Layout = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "ConsoleLayout", "${longdate}|${level:uppercase=true}|${logger}|${message}");

        string fileLogLevel = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "FileLogLevel", "Error");
        string consoleLogLevel = GetValue<string>("CrossCutting.Core.Logging.NLogAdapter", "ConsoleLogLevel", "Error");

        config.AddRule(StringToLogLevel(fileLogLevel), NLog.LogLevel.Fatal, fileLogTarget);
        config.AddRule(StringToLogLevel(consoleLogLevel), NLog.LogLevel.Fatal, consoleLogTarget);

        NLog.LogManager.Configuration = config;
    }

    private static ArchiveNumberingMode StringToArchiveNumberingMode(string value)
    {
        return value.ToLower() switch
        {
            "sequence" => ArchiveNumberingMode.Sequence,
            "rolling" => ArchiveNumberingMode.Rolling,
            "date" => ArchiveNumberingMode.Date,
            "dateandsequence" => ArchiveNumberingMode.DateAndSequence,
            _ => ArchiveNumberingMode.DateAndSequence
        };
    }

    private static NLog.LogLevel StringToLogLevel(string value)
    {
        return value.ToLower() switch
        {
            "trace" => NLog.LogLevel.Trace,
            "debug" => NLog.LogLevel.Debug,
            "info" => NLog.LogLevel.Info,
            "warn" => NLog.LogLevel.Warn,
            "error" => NLog.LogLevel.Error,
            "fatal" => NLog.LogLevel.Fatal,
            "off" => NLog.LogLevel.Off,
            _ => NLog.LogLevel.Error
        };
    }

    private static Encoding StringToEncoding(string value)
    {
        return value.ToLower() switch
        {
            "utf8" => Encoding.UTF8,
            "utf-8" => Encoding.UTF8,
            "unicode" => Encoding.Unicode,
            "bigendianunicode" => Encoding.BigEndianUnicode,
            "utf32" => Encoding.UTF32,
            "utf.32" => Encoding.UTF32,
            "ascii" => Encoding.ASCII,
            "latin1" => Encoding.Latin1,
#pragma warning disable SYSLIB0001 // Typ oder Element ist veraltet
            "utf7" => Encoding.UTF7,
            "utf-7" => Encoding.UTF7,
#pragma warning restore SYSLIB0001 // Typ oder Element ist veraltet
            _ => Encoding.UTF8
        };
    }

    private static T GetValue<T>(string configName, string key, T defaultValue)
    {
        try
        {
            JArray configFile = JArray.Parse(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, s_configFileName)));
            foreach (dynamic configuration in configFile)
            {
                if (!configuration.Name.ToString().Equals(configName, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (dynamic entry in configuration.Entries)
                {
                    if (entry.Key.ToString().Equals(key, StringComparison.OrdinalIgnoreCase))
                        return entry.Value;
                }
            }
        }
        catch
        {
            // ignored
        }

        return defaultValue;
    }
}