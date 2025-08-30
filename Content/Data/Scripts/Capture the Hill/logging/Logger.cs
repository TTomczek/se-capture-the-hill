using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Utils;

namespace CaptureTheHill.logging
{
    public static class Logger
    {
        
        private static readonly string LogFileNamePattern = "CaptureTheHill-{0:yyyy-MM-dd_HH-mm-ss}.log";
        private static readonly string LoggingPattern = "{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}";
        private static TextWriter _currentLogFileWriter;
        private static bool _isDebugEnabled = true;
        
        static Logger()
        {
            MyAPIGateway.Utilities.MessageEnteredSender += LoggingMessageEntered;
        }
        
        public static void Info(string message)
        {
            Log(message);
        }
        
        public static void Warning(string message)
        {
            Log(message, LogLevel.Warning);
        }
        
        public static void Error(string message)
        {
            Log(message, LogLevel.Error);
        }
        
        public static void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public static void CloseLogger()
        {
            if (_currentLogFileWriter != null)
            {
                try
                {
                    Debug("Closing logger and flushing current log file.");
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
        
        private static void Log(string message, LogLevel logLevel = LogLevel.Info)
        {
            if ((logLevel == LogLevel.Debug && !_isDebugEnabled) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            if (_currentLogFileWriter == null)
            {
                Init();
            }

            try
            {
                var formattedMessage = string.Format(LoggingPattern, DateTime.Now, logLevel, message);
                _currentLogFileWriter?.WriteLine(formattedMessage);
                _currentLogFileWriter?.Flush();
                MyLog.Default.WriteLineAndConsole(formattedMessage);
            } catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error writing log line [{message}] because: {ex.Message}");
                MyLog.Default.WriteLineAndConsole(ex.StackTrace);
            }
        }
        
        private static void LoggingMessageEntered(ulong sender,string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return;
            }
            
            if (!messageText.StartsWith("/log", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            
            sendToOthers = false;
            messageText = messageText.Substring(4).Trim();

            switch (messageText)
            {
                case "debug":
                    _isDebugEnabled = !_isDebugEnabled;
                    var logText = _isDebugEnabled ? "Debug logging enabled." : "Debug logging disabled.";
                    Log(logText);
                    break;
            }
        }

        private static void Init()
        {
            var logFileName = string.Format(LogFileNamePattern, DateTime.Now);
            _currentLogFileWriter = MyAPIGateway.Utilities.WriteFileInWorldStorage(logFileName, typeof(Logger));
        }
    }
}