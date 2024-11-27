using System;
using System.IO;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

public class Logger
{

    public static Logger Default = new Logger(LogLevel.Info);

    private readonly LogLevel _logLevel;
    private readonly string? _logFilePath;
    public Logger(LogLevel logLevel = LogLevel.Info, string? logFilePath = null)
    {
        _logLevel = logLevel;
        _logFilePath = logFilePath;
    }

    private void WriteLog(LogLevel level, string message)
    {
        if (level >= _logLevel)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            Console.WriteLine(logMessage); // Log to console

            // Optionally, log to file also
            if(_logFilePath != null) 
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
        }
    }

    public void Log(string message, LogLevel level)
    {
        WriteLog(level, message);
    }

    public void Log(string message)
    {
        WriteLog(_logLevel, message);

    }

    public void Debug(string message)
    {
        WriteLog(LogLevel.Debug, message);
    }

    public void Info(string message)
    {
        WriteLog(LogLevel.Info, message);
    }

    public void Warn(string message)
    {
        WriteLog(LogLevel.Warn, message);
    }

    public void Error(string message)
    {
        WriteLog(LogLevel.Error, message);
    }

    public void Fatal(string message)
    {
        WriteLog(LogLevel.Fatal, message);
    }
}