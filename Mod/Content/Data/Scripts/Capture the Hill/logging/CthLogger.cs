using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.logging;

namespace CaptureTheHill.logging
{
    public static class CthLogger
    {
        public static ICthLogger CthLoggerInstance { get; set; } = new LineAndConsoleCthLogger();

        public static void Info(string message)
        {
            CthLoggerInstance?.Info(message);
        }

        public static void Warning(string message)
        {
            CthLoggerInstance?.Warning(message);
        }

        public static void Error(string message)
        {
            CthLoggerInstance?.Error(message);
        }

        public static void Debug(string message)
        {
            CthLoggerInstance?.Debug(message);
        }

        public static void StartLogging()
        {
            CthLoggerInstance?.StartLogging();
        }

        public static void CloseLogger()
        {
            CthLoggerInstance?.CloseLogger();
        }
    }
}