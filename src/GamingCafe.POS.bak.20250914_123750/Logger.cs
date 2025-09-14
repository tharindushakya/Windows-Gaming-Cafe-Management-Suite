using System;
using System.IO;

namespace GamingCafe.POS;

public static class Logger
{
    private static readonly string LogDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GamingCafePOS", "logs");
    private static readonly string LogFile = Path.Combine(LogDir, $"pos-errors-{DateTime.Now:yyyyMMdd}.log");
    public static void Log(string message)
    {
        Log(message, null);
    }

    public static void Log(string message, string? correlationId)
    {
        try
        {
            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
            var prefix = string.IsNullOrEmpty(correlationId) ? string.Empty : $"[{correlationId}] ";
            File.AppendAllText(LogFile, $"[{DateTime.Now:O}] {prefix}{message}\r\n\r\n");
        }
        catch { /* swallow logging errors */ }
    }

    public static void Log(Exception ex)
    {
        Log(ex, null);
    }

    public static void Log(Exception ex, string? correlationId)
    {
        try
        {
            Log(ex.ToString(), correlationId);
        }
        catch { }
    }

    public static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
