using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Utils;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.logging
{
    public class WorldStorageCthLogger : ICthLogger
    {
        private static readonly string LogFileNamePattern = "CaptureTheHill-{0:yyyy-MM-dd_HH-mm-ss}.log";
        private static readonly string LoggingPattern = "{0:yyyy-MM-dd HH:mm:ss.fff} [CTH] [{1}] {2}";
        private static TextWriter _currentLogFileWriter;
        private static bool _isDebugEnabled = true;
        private static readonly Queue<string> LogQueue = new Queue<string>();
        private static bool _loggerActivated = true;

        public void StartLogging()
        {
            var logFileName = string.Format(LogFileNamePattern, DateTime.Now);
            _currentLogFileWriter =
                MyAPIGateway.Utilities.WriteFileInWorldStorage(logFileName, typeof(WorldStorageCthLogger));
            _loggerActivated = true;

            if (_currentLogFileWriter == null)
            {
                MyLog.Default.WriteLineAndConsole("Failed to create log file writer.");
                return;
            }

            MyAPIGateway.Parallel.StartBackground(() =>
            {
                while (_loggerActivated)
                {
                    if (LogQueue.Count > 0 && _currentLogFileWriter != null)
                    {
                        try
                        {
                            var logEntry = LogQueue.Dequeue();
                            _currentLogFileWriter.WriteLine(logEntry);
                            _currentLogFileWriter.Flush();
                        }
                        catch (Exception ex)
                        {
                            MyLog.Default.WriteLineAndConsole($"Error writing log line because: {ex.Message}");
                            MyLog.Default.WriteLineAndConsole(ex.StackTrace);
                        }
                    }
                }
            });
        }

        public void Info(string message)
        {
            FormatLog(message);
        }

        public void Warning(string message)
        {
            FormatLog(message, LogLevel.Warning);
        }

        public void Error(string message)
        {
            FormatLog(message, LogLevel.Error);
        }

        public void Debug(string message)
        {
            FormatLog(message, LogLevel.Debug);
        }

        public void CloseLogger()
        {
            if (_currentLogFileWriter != null)
            {
                try
                {
                    Debug("Start closing logger...");
                    while (LogQueue.Count > 0)
                    {
                        // Wait for the queue to be processed
                    }

                    _loggerActivated = false;
                    _currentLogFileWriter.Flush();
                    _currentLogFileWriter.Close();
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole($"Error closing log file: {ex.Message}");
                    MyLog.Default.WriteLineAndConsole(ex.StackTrace);
                }
                finally
                {
                    _currentLogFileWriter = null;
                }
            }
        }

        private void FormatLog(string message, LogLevel level = LogLevel.Info)
        {
            if (level == LogLevel.Debug && !_isDebugEnabled)
            {
                return;
            }

            var formattedMessage = string.Format(LoggingPattern, DateTime.Now, level, message);
            LogQueue.Enqueue(formattedMessage);
        }
    }
}