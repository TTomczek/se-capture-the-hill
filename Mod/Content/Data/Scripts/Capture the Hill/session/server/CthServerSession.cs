using System;
using System.Collections.Generic;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.messaging.server;
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
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CthServerSession : MySessionComponentBase
    {
        public bool IsServer { get; private set; }

        public static CthServerSession Instance { get; private set; }

        public override void LoadData()
        {
            IsServer = MyAPIGateway.Multiplayer.IsServer || !MyAPIGateway.Multiplayer.MultiplayerActive ||
                       MyAPIGateway.Utilities.IsDedicated;
            Instance = this;
            CthLogger.Info($"Server session LoadData, isServer: {IsServer}");
            CthLogger.StartLogging();

            if (!IsServer)
            {
                return;
            }

            CthLogger.Info("Loading server session...");

            ModConfiguration.LoadConfiguration();
            GameStateAccessor.LoadState();
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkChannels.ClientToServer,
                ServerMessageHandler.HandleMessage);
        }

        public override void BeforeStart()
        {
            if (!IsServer)
            {
                return;
            }

            CthLogger.Info("Setting up server session...");

            var planets = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(planets, e => e is MyPlanet);

            try
            {
                CaptureBaseSpawner.CheckAndCreateBasesIfNeeded(planets);
            }
            catch (Exception ex)
            {
                CthLogger.Error($"Error checking and creating capture bases: {ex.Message}");
                CthLogger.Error(ex.StackTrace);
            }
        }

        protected override void UnloadData()
        {
            if (!IsServer)
            {
                return;
            }

            try
            {
                ModConfiguration.SaveConfiguration();
                GameStateAccessor.SaveState();
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkChannels.ClientToServer,
                    ServerMessageHandler.HandleMessage);
                CthLogger.Info("Unloaded Capture the Hill Session...");
                CthLogger.CloseLogger();
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
    }
}