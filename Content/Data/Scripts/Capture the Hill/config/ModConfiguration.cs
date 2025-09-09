using System;
using CaptureTheHill.logging;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config
{
    public class ModConfiguration
    {
        private static readonly string ConfigFileName = "CaptureTheHillConfig.xml";
        public static ModConfiguration Instance;
        
        public int GroundBaseCaptureTimeInSeconds = 10;
        public int AtmosphereBaseCaptureTimeInSeconds= 10;
        public int SpaceBaseCaptureTimeInSeconds= 10;
        
        public int GroundBaseCaptureRadius = 150;
        public int AtmosphereBaseCaptureRadius = 250;
        public int SpaceBaseCaptureRadius = 250;
        
        public int GroundBaseDiscoveryRadius = 10000;
        public int AtmosphereBaseDiscoveryRadius = 10000;
        public int SpaceBaseDiscoveryRadius = 15000;
        
        // Not yet implemented
        public bool CanCaptureFriendlyBases = false;
        
        // Not yet implemented
        public bool CanCaptureAlreadyClaimedBases = true;

        // Not yet implemented
        public bool CanCaptureBasesFromAlreadyCapturedPlanet = true;

        public int PointsForFactionToWin = 100;

        public int PointsForPlanetDominance = 1;
        public int PointsPerOwnedPlanet = 5;
        
        public bool BroadcastBaseDiscoveryToFaction = true;

        public int DominanceStrengthSmallGrid = 1;
        public int DominanceStrengthLargeGrid = 2;
        
        
        public static void LoadConfiguration(bool forceReload = false)
        {
            Logger.Info("Loading configuration...");
            try
            {
                if (Instance != null && !forceReload)
                {
                    Logger.Info("Mod configuration already loaded, skipping reload.");
                    return;
                }

                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(ConfigFileName, typeof(ModConfiguration)))
                {
                    Logger.Warning("Mod configuration file not found, creating default configuration.");
                    Instance = new ModConfiguration();
                }
                else
                {
                    using (var textReader = MyAPIGateway.Utilities.ReadFileInWorldStorage(ConfigFileName, typeof(ModConfiguration)))
                    {
                        var xml = textReader.ReadToEnd();
                        if (string.IsNullOrEmpty(xml))
                        {
                            Logger.Warning("Mod configuration file is empty, creating default configuration.");
                            Instance = new ModConfiguration();
                            return;
                        }
                        Instance = MyAPIGateway.Utilities.SerializeFromXML<ModConfiguration>(xml);
                        Logger.Info("Mod configuration loaded successfully.");
                    }
                }
            } catch (Exception e)
            {
                Logger.Error("Error loading mod configuration: " + e.Message);
                Logger.Error(e.StackTrace);
                Logger.Error("Creating default configuration.");
                Instance = new ModConfiguration();
            }
        }

        public static void SaveConfiguration()
        {
            Logger.Info("Saving configuration...");
            try
            {
                if (Instance == null)
                {
                    Logger.Warning("No instance of ModConfiguration to save, creating new instance.");
                    Instance = new ModConfiguration();
                }

                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ConfigFileName, typeof(ModConfiguration)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(Instance));
                    Logger.Info("Mod configuration saved successfully.");
                }
            } catch (Exception e)
            {
                Logger.Error("Error saving mod configuration: " + e.Message);
                Logger.Error(e.StackTrace);
            }
        }

    }
}