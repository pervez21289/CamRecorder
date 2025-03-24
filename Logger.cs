using System;
using System.IO;

public static class Logger
{
    private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service.log");
    private static readonly long maxLogFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly int maxBackupFiles = 5;

    public static void Log(string message)
    {
        try
        {
            RotateLogFileIfNeeded();

            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during logging
            Console.WriteLine($"Failed to write log: {ex.Message}");
        }
    }

    private static void RotateLogFileIfNeeded()
    {
        FileInfo logFileInfo = new FileInfo(logFilePath);

        if (logFileInfo.Exists && logFileInfo.Length > maxLogFileSize)
        {
            for (int i = maxBackupFiles - 1; i >= 0; i--)
            {
                string backupFilePath = $"{logFilePath}.{i}";

                if (File.Exists(backupFilePath))
                {
                    if (i == maxBackupFiles - 1)
                    {
                        File.Delete(backupFilePath);
                    }
                    else
                    {
                        string nextBackupFilePath = $"{logFilePath}.{i + 1}";
                        File.Move(backupFilePath, nextBackupFilePath);
                    }
                }
            }

            File.Move(logFilePath, $"{logFilePath}.0");
        }
    }
}