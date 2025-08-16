using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Utils;

namespace CaptureTheHill.logging
{
    public static class Logger
    {
        
        private static readonly string FileName = $"CaptureTheHill-{DateTime.Now:yy-MM-dd}.log";
        
        public static void Info(string message)
        {
            Log(message, MyLogSeverity.Info);
        }
        
        public static void Warn(string message)
        {
            Log(message, MyLogSeverity.Warning);
        }
        
        public static void Error(string message)
        {
            Log(message, MyLogSeverity.Error);
        }
        
        public static void Debug(string message)
        {
            Log(message, MyLogSeverity.Debug);
        }

        private static void Log(string message, MyLogSeverity severity)
        {
            // MyLog.Default.Log(severity, message);
            using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(Logger)))
            {
                writer.WriteLine($"[{DateTime.Now:HH:mm:ss:zzz}] [{severity.ToString().ToUpper()}] {message}");
                writer.Flush();
            }
        }
    }
    
    
}