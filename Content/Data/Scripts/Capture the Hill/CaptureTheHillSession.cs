using System;
using System.Collections.Generic;
using System.Text;
using CaptureTheHill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.logging;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using IMyEntity = VRage.ModAPI.IMyEntity;

namespace CaptureTheHill
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class CaptureTheHillSession : MySessionComponentBase
    {
        private static bool _isInitialized;
        private static bool _isServer;
        private static uint _ticks;
        
        public static CaptureTheHillSession Instance { get; private set; }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            
            _isServer = (MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Multiplayer.IsServer) ||
                        !MyAPIGateway.Multiplayer.MultiplayerActive;

            if (_isServer)
            {
                ModConfiguration.LoadConfiguration();
            }
            
            if (MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkConstants.JoinFactionToCaptureMessage, HandleCaptureMessage);
            }
            
            Logger.Info("Capture the Hill Session started. isServer: " + _isServer);
        }

        public override void LoadData()
        {
            Instance = this;
            CaptureTheHillGameState.LoadState();
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isServer || _isInitialized)
            {
                return;
            }
            
            Logger.Info("Initializing Capture the Hill Session...");
            _isInitialized = true;
            
            var planets = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(planets, e => e is MyPlanet);

            try
            {
                CaptureBaseSpawner.CheckAndCreateBasesIfNeeded(planets);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking and creating capture bases: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkConstants.JoinFactionToCaptureMessage, HandleCaptureMessage);
            }
            
            try
            {
                ModConfiguration.SaveConfiguration();
                CaptureTheHillGameState.SaveState();
                Logger.Info("Unloaded Capture the Hill Session...");
                Logger.CloseLogger();
            }
            catch (Exception ex)
            {
                MyLog.Default.Error("Error during Capture the Hill session unload: " + ex.Message);
                MyLog.Default.Error(ex.StackTrace);
            }
            finally
            {
                Instance = null;
            }
        }
        
        private void HandleCaptureMessage(ushort id, byte[] data, ulong senderSteamId, bool fromServer)
        {
            string msg = Encoding.UTF8.GetString(data);
            MyAPIGateway.Utilities.ShowNotification(msg, 5000);
        }
    }
}