using System.Text;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using CaptureTheHill.logging;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.session.client
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class CthClientSession : MySessionComponentBase
    {
        private bool _isClient;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            _isClient = !MyAPIGateway.Multiplayer.IsServer;

        }

        public override void LoadData()
        {
            if (!_isClient)
            {
                return;
            }
            
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkConstants.JoinFactionToCaptureMessage, HandleCaptureMessage);
            Logger.Info("Capture the Hill Client Session started. isClient: " + _isClient);
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isClient)
            {
                return;
            }

            Logger.Info("Initializing Capture the Hill Client Session...");

        }

        protected override void UnloadData()
        {
            if (!_isClient)
            {
                return;
            }

            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkConstants.JoinFactionToCaptureMessage, HandleCaptureMessage);
        }
        
        private void HandleCaptureMessage(ushort id, byte[] data, ulong senderSteamId, bool fromServer)
        {
            string msg = Encoding.UTF8.GetString(data);
            MyAPIGateway.Utilities.ShowNotification(msg, 5000);
        }
    }
}