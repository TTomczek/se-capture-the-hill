using System;
using System.Collections.Generic;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.spawner;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.state;
using CaptureTheHill.logging;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using IMyEntity = VRage.ModAPI.IMyEntity;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.session.server
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class CthServerSession : MySessionComponentBase
    {
        private bool _isInitialized;
        private bool _isServer;
        private uint _ticks;

        public override void LoadData()
        {
            _isServer = MyAPIGateway.Multiplayer.IsServer || !MyAPIGateway.Multiplayer.MultiplayerActive ||
                        MyAPIGateway.Utilities.IsDedicated;
            Logger.Info($"Server session LoadData, isServer: {_isServer}");
            // Logger.StartLogging();

            if (!_isServer)
            {
                return;
            }

            Logger.Info("Loading server session...");

            ModConfiguration.LoadConfiguration();
            GameStateAccessor.LoadState();
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkChannels.ClientToServer,
                ServerMessageHandler.HandleMessage);
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isServer || _isInitialized)
            {
                return;
            }

            Logger.Info("Setting up server session...");
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
            if (!_isServer)
            {
                return;
            }

            try
            {
                ModConfiguration.SaveConfiguration();
                GameStateAccessor.SaveState();
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkChannels.ClientToServer,
                    ServerMessageHandler.HandleMessage);
                Logger.Info("Unloaded Capture the Hill Session...");
                // Logger.CloseLogger();
            }
            catch (Exception ex)
            {
                MyLog.Default.Error("Error during Capture the Hill session unload: " + ex.Message);
                MyLog.Default.Error(ex.StackTrace);
            }
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