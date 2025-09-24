using System;
using CaptureTheHill.logging;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config
{
    public class ModConfiguration
    {
        private static readonly string ConfigFileName = "CaptureTheHillConfig.xml";
        public static ModConfiguration Instance;

        public int GroundBaseCaptureTimeInSeconds = 600;
        public int AtmosphereBaseCaptureTimeInSeconds = 600;
        public int SpaceBaseCaptureTimeInSeconds = 600;

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

        public int PointsForFactionToWin = 2500;

        public int PointsForPlanetDominance = 1;
        public int PointsPerOwnedPlanet = 5;

        public bool BroadcastBaseDiscoveryToFaction = true;

        public int DominanceStrengthSmallGrid = 1;
        public int DominanceStrengthLargeGrid = 2;


        public static void LoadConfiguration(bool forceReload = false)
        {
            CthLogger.Info("Loading configuration...");
            try
            {
                if (Instance != null && !forceReload)
                {
                    CthLogger.Info("Mod configuration already loaded, skipping reload.");
                    return;
                }

                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(ConfigFileName, typeof(ModConfiguration)))
                {
                    CthLogger.Warning("Mod configuration file not found, creating default configuration.");
                    Instance = new ModConfiguration();
                }
                else
                {
                    using (var textReader =
                           MyAPIGateway.Utilities.ReadFileInWorldStorage(ConfigFileName, typeof(ModConfiguration)))
                    {
                        var xml = textReader.ReadToEnd();
                        if (string.IsNullOrEmpty(xml))
                        {
                            CthLogger.Warning("Mod configuration file is empty, creating default configuration.");
                            Instance = new ModConfiguration();
                            return;
                        }

                        Instance = MyAPIGateway.Utilities.SerializeFromXML<ModConfiguration>(xml);
                        CthLogger.Info("Mod configuration loaded successfully.");
                    }
                }
            }
            catch (Exception e)
            {
                CthLogger.Error("Error loading mod configuration: " + e.Message);
                CthLogger.Error(e.StackTrace);
                CthLogger.Error("Creating default configuration.");
                Instance = new ModConfiguration();
            }
        }

        public static void SaveConfiguration()
        {
            CthLogger.Info("Saving configuration...");
            try
            {
                if (Instance == null)
                {
                    CthLogger.Warning("No instance of ModConfiguration to save, creating new instance.");
                    Instance = new ModConfiguration();
                }

                using (var writer =
                       MyAPIGateway.Utilities.WriteFileInWorldStorage(ConfigFileName, typeof(ModConfiguration)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(Instance));
                    CthLogger.Info("Mod configuration saved successfully.");
                }
            }
            catch (Exception e)
            {
                CthLogger.Error("Error saving mod configuration: " + e.Message);
                CthLogger.Error(e.StackTrace);
            }
        }
    }
}