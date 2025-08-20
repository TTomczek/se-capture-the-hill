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
        private static TextWriter CurrentLogFileWriter;
        private static bool _isDebugEnabled;
        
        static Logger()
        {
            Init();
            MyAPIGateway.Utilities.MessageEnteredSender += LoggingMessageEntered;
        }
        
        public static void Info(string message)
        {
            Log(message, LogLevel.Info);
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
        
        private static void Log(string message, LogLevel logLevel = LogLevel.Info)
        {
            if ((logLevel == LogLevel.Debug && !_isDebugEnabled) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            if (CurrentLogFileWriter == null)
            {
                Init();
            }

            try
            {
                string formattedMessage = string.Format(LoggingPattern, DateTime.Now, logLevel, message);
                CurrentLogFileWriter.WriteLine(formattedMessage);
                CurrentLogFileWriter.Flush();
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
            string logFileName = string.Format(LogFileNamePattern, DateTime.Now);
            CurrentLogFileWriter = MyAPIGateway.Utilities.WriteFileInWorldStorage(logFileName, typeof(Logger));
        }
        
    }
    
    
}