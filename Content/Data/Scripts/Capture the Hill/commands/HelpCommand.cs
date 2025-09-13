using System.Collections.Generic;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.commands
{
    public static class HelpCommand
    {
        public static bool IsCommandResponsible(string messageText)
        {
            return messageText.Equals("help") || messageText.Equals("?");
        }

        public static void Execute(List<IChatCommand> chatCommands)
        {
            var chatString = "\nAvailable commands:\n";
            foreach (var command in chatCommands)
            {
                chatString += $"{command.GetHelp()}\n";
            }

            MyAPIGateway.Utilities.ShowMessage("CTH", chatString);
        }
    }
}