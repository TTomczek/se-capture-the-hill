namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.logging
{
    public interface ICthLogger
    {
        void Info(string message);

        void Warning(string message);

        void Error(string message);

        void Debug(string message);

        void StartLogging();

        void CloseLogger();
    }
}