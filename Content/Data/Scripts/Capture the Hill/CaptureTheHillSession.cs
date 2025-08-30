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
using VRage.Utils;
using IMyEntity = VRage.ModAPI.IMyEntity;

namespace CaptureTheHill
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class CaptureTheHillSession : MySessionComponentBase
    {
        private bool _isInitialized;
        private bool _isServer;
        private uint _ticks;
        
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            
            _isServer = (MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Multiplayer.IsServer) ||
                        !MyAPIGateway.Multiplayer.MultiplayerActive;
            
            if (MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkConstants.JoinFactionToCaptureMessage, HandleCaptureMessage);
            }
            
        }

        public override void LoadData()
        {
            Logger.Info("Capture the Hill Session started. isServer: " + _isServer);
            ModConfiguration.LoadConfiguration();
            GameStateAccessor.LoadState();
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

        public override void UpdateAfterSimulation()
        {
            if (!_isServer || !_isInitialized)
            {
                return;
            }

            _ticks++;
            if (_ticks % 60 == 0)
            {
                _ticks = 0;
                try
                {
                    var allBasesPerPlanet = GameStateAccessor.GetAllBasesPerPlanet();
                    CaptureBaseCaptureManager.Update(allBasesPerPlanet);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error updating capture bases: {ex.Message}");
                    Logger.Error(ex.StackTrace);
                }
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
                GameStateAccessor.SaveState();
                Logger.Info("Unloaded Capture the Hill Session...");
                Logger.CloseLogger();
            }
            catch (Exception ex)
            {
                MyLog.Default.Error("Error during Capture the Hill session unload: " + ex.Message);
                MyLog.Default.Error(ex.StackTrace);
            }
        }
        
        private void HandleCaptureMessage(ushort id, byte[] data, ulong senderSteamId, bool fromServer)
        {
            string msg = Encoding.UTF8.GetString(data);
            MyAPIGateway.Utilities.ShowNotification(msg, 5000);
        }
        
        private void PlanetAdded(IMyEntity entity)
        {
            if (!_isInitialized)
            {
                return;
            }
            
            if (entity is MyPlanet)
            {
                var planet = entity as MyPlanet;
                Logger.Info($"Planet added: {planet.Name}");
                try
                {
                    CaptureBaseSpawner.CheckAndCreateBasesIfNeeded(new HashSet<IMyEntity> { planet });
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error checking and creating capture bases for planet {planet.Name}: {ex.Message}");
                    Logger.Error(ex.StackTrace);
                }
            }
        }
        
        private void PlanetRemoved(IMyEntity entity)
        {
            if (!_isInitialized)
            {
                return;
            }
            
            if (entity is MyPlanet)
            {
                var planet = entity as MyPlanet;
                GameStateAccessor.RemoveBasesOfPlanet(planet.Name);
                Logger.Info($"Planet removed: {planet.Name}");
            }
        }
    }
}