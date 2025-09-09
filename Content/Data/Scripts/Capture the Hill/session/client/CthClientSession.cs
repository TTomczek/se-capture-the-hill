using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.commands;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
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
            Logger.Info($"Client session LoadData, isClient: {_isClient}");
            
            if (!_isClient)
            {
                return;
            }
            Logger.Info("Loading client session...");
            
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkMessageConstants.JoinFactionToCapture, HandleCaptureMessage);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkMessageConstants.GetLeaderboardResponse, HandleLeaderboardResponse);
            
            MyAPIGateway.Utilities.MessageEnteredSender += HandleChatCommand;
            Logger.Info("Capture the Hill Client Session started. isClient: " + _isClient);
        }

        protected override void UnloadData()
        {
            if (!_isClient)
            {
                return;
            }

            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkMessageConstants.JoinFactionToCapture, HandleCaptureMessage);
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkMessageConstants.GetLeaderboardResponse, HandleLeaderboardResponse);
            MyAPIGateway.Utilities.MessageEnteredSender -= HandleChatCommand;
        }
        
        private void HandleCaptureMessage(ushort id, byte[] data, ulong senderSteamId, bool fromServer)
        {
            string msg = Encoding.UTF8.GetString(data);
            MyAPIGateway.Utilities.ShowNotification(msg, 5000);
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
                    Logger.Debug($"Executing command: {command.Name}");
                    command.Execute(commandText);
                    break;
                }
            }
            
            sendToOthers = false;
        }
        
        private void HandleLeaderboardResponse(ushort handlerId, byte[] data, ulong senderPlayerId, bool isArrivedFromServer)
        {
            Logger.Debug($"Leaderboard response: {Encoding.UTF8.GetString(data)}");
            try
            {
                var leaderboardString = Encoding.UTF8.GetString(data);
                MyAPIGateway.Utilities.ShowMessage("CTH", leaderboardString);
            }
            catch (Exception ex)
            {
                Logger.Error("Error handling leaderboard request: " + ex.Message);
                Logger.Error(ex.StackTrace);
            }
        }
    }
}