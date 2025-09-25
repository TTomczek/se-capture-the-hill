using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.commands;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging;
using CaptureTheHill.logging;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.session.client
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CthClientSession : MySessionComponentBase
    {
        private bool _isClient;

        private readonly List<IChatCommand> _chatCommands = new List<IChatCommand>
        {
            new LeaderboardCommand()
        };

        public override void LoadData()
        {
            _isClient = !MyAPIGateway.Utilities.IsDedicated;
            CthLogger.Info($"Client session LoadData, isClient: {_isClient}");

            if (!_isClient)
            {
                return;
            }

            CthLogger.Info("Loading client session...");

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkChannels.ServerToClient,
                ClientMessageHandler.HandleMessage);

            MyAPIGateway.Utilities.MessageEnteredSender += HandleChatCommand;
            CthLogger.Info("Capture the Hill Client Session started. isClient: " + _isClient);
        }

        public override void BeforeStart()
        {
            var welcomeMessage =
                "\nWelcome to Capture the Hill!\nJoin or create faction and explore some planets to start!\nHappy hunting!\nFor a list of commands, type /cth help";
            MyAPIGateway.Utilities.ShowMessage("CTH", welcomeMessage);
        }

        protected override void UnloadData()
        {
            if (!_isClient)
            {
                return;
            }

            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkChannels.ServerToClient,
                ClientMessageHandler.HandleMessage);
            MyAPIGateway.Utilities.MessageEnteredSender -= HandleChatCommand;
        }

        private void HandleChatCommand(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return;
            }

            if (!messageText.StartsWith("/cth"))
            {
                return;
            }

            var split = messageText.Split(' ');

            if (split.Length < 2)
            {
                MyAPIGateway.Utilities.ShowNotification("Invalid command. Use /cth help for a list of commands.", 5000);
                return;
            }

            var commandText = messageText.Substring(5).Trim().ToLower();

            if (HelpCommand.IsCommandResponsible(commandText))
            {
                HelpCommand.Execute(_chatCommands);
            }
            else
            {
                foreach (var command in _chatCommands.Where(command => command.IsCommandResponsible(commandText)))
                {
                    CthLogger.Debug($"Executing command: {command.Name}");
                    command.Execute(commandText);
                    break;
                }
            }

            sendToOthers = false;
        }
    }
}