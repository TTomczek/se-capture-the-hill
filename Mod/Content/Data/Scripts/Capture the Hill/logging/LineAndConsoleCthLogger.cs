using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using VRage.Utils;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.logging
{
    public class LineAndConsoleCthLogger : ICthLogger
    {
        public void Info(string message)
        {
            Log(message);
        }

        public void Warning(string message)
        {
            Log(message);
        }

        public void Error(string message)
        {
            Log(message);
        }

        public void Debug(string message)
        {
            if (ModConfiguration.Instance.EnableDebugLogging)
            {
                Log(message);
            }
        }

        public void StartLogging()
        {
        }

        public void CloseLogger()
        {
        }

        private void Log(string message)
        {
            MyLog.Default.WriteLineAndConsole(message);
        }
    }
}