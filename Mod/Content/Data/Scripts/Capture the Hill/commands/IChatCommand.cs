namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.commands
{
    public interface IChatCommand
    {
        string Name { get; }
        bool IsCommandResponsible(string messageText);
        void Execute(string messageText);
        string GetHelp();
    }
}