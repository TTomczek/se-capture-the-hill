using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill;
using CaptureTheHill.logging;
using Sandbox.ModAPI;

namespace CaptureTheHill.config
{
    public class GameStateAccessor
    {
        private static readonly string SaveFileName = "CaptureTheHillGameState.bin";

        private static GameStateStore _instance;
        
        
        public static void AddBaseToPlanet(CaptureBaseData baseData)
        {
            if (_instance == null)
            {
                Logger.Error($"GameStateAccessor: _instance is null in AddBaseToPlanet. Cannot add base {baseData.BaseName}");
                return;
            }
            
            if (!_instance.BasesPerPlanet.ContainsKey(baseData.PlanetName))
            {
                _instance.BasesPerPlanet[baseData.PlanetName] = new List<CaptureBaseData>();
            }
            _instance.BasesPerPlanet[baseData.PlanetName].Add(baseData);
        }
        
        public static void RemoveBasesOfPlanet(string planetName)
        {
            _instance?.BasesPerPlanet.Remove(planetName);
        }
        
        public static void GetBaseDataByBaseName(string baseName, ref CaptureBaseData outBaseData)
        {
            if (_instance == null)
            {
                return;
            }
            
            foreach (var planetBases in _instance.BasesPerPlanet.Values)
            {
                var baseData = planetBases.Find(b => b.BaseName == baseName);
                if (baseData != null)
                {
                    outBaseData = baseData;
                }
            }
        }
        
        public static Dictionary<string, List<CaptureBaseData>> GetAllBasesPerPlanet()
        {
            if (_instance == null)
            {
                Logger.Error("GameStateAccessor: _instance is null in GetAllBasesPerPlanet");
                return new Dictionary<string, List<CaptureBaseData>>();
            }
            else
            {
                return _instance.BasesPerPlanet;
            }
        }
        
        public static void AddPointsToFaction(long factionId, int points)
        {
            if (!_instance.PointsPerFaction.ContainsKey(factionId))
            {
                _instance.PointsPerFaction[factionId] = 0;
            }
            _instance.PointsPerFaction[factionId] += points;
        }
        
        public static int GetPointsForFaction(long factionId)
        {
            if (_instance.PointsPerFaction.ContainsKey(factionId))
            {
                return _instance.PointsPerFaction[factionId];
            }
            return 0;
        }
        
        public static void AddPlayerToBaseDiscovery(string baseName, long playerId)
        {
            Logger.Debug($"Adding player {playerId} to base discovery for {baseName}");
            if (!_instance.BasePlayerDiscovered.ContainsKey(baseName))
            {
                _instance.BasePlayerDiscovered[baseName] = new List<long>();
            }
            if (!_instance.BasePlayerDiscovered[baseName].Contains(playerId))
            {
                _instance.BasePlayerDiscovered[baseName].Add(playerId);
            }
        }
        
        public static void AddPlayersToBaseDiscovery(string baseName, List<long> playerIds)
        {
            foreach (var playerId in playerIds)
            {
                AddPlayerToBaseDiscovery(baseName, playerId);
            }
        }
        
        public static List<long> GetPlayersWhoDiscoveredBase(string baseName)
        {
            if (_instance.BasePlayerDiscovered.ContainsKey(baseName))
            {
                return _instance.BasePlayerDiscovered[baseName];
            }
            return new List<long>();
        }
        
        public static void SaveState()
        {
            using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(SaveFileName, typeof(GameStateStore)))
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(_instance);
                writer.Write(bytes);
                writer.Flush();
            }
            LogCurrentStateToDebug();
            Logger.Info($"Game state saved to {SaveFileName}");
        }
        
        public static void LoadState()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(SaveFileName, typeof(GameStateStore)))
            {
                using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(SaveFileName, typeof(GameStateStore)))
                {
                    var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                    _instance = MyAPIGateway.Utilities.SerializeFromBinary<GameStateStore>(bytes);
                    if (_instance == null)
                    {
                        Logger.Warning($"Failed to load game state from {SaveFileName}, initializing new state.");
                        _instance = new GameStateStore();
                    }
                    else
                    {
                        Logger.Info($"Game state loaded successfully from {SaveFileName}");
                    }
                }
            }
            else
            {
                _instance = new GameStateStore();
                Logger.Info($"No existing game state found. Initialized new state.");
            }
            LogCurrentStateToDebug();
        }

        private static void LogCurrentStateToDebug()
        {
            Logger.Debug("Current game state");
            if (_instance == null)
            {
                Logger.Debug("_instance is null");
                return;
            }
            
            Logger.Debug($"BasesPerPlanet Keys: {string.Join(", ", _instance.BasesPerPlanet.Keys.ToList())}");
            foreach (var planet in _instance.BasesPerPlanet.Keys)
            {
                Logger.Debug($"Bases for planet {planet}: {string.Join(", ", _instance.BasesPerPlanet[planet].Select(b => b.BaseName).ToList())}");
            }
            
            Logger.Debug($"PointsPerFaction: {string.Join(", ", _instance.PointsPerFaction.Select(kv => kv.Key + "=" + kv.Value).ToList())}");
            
            Logger.Debug($"BasePlayerDiscovered Keys: {string.Join(", ", _instance.BasePlayerDiscovered.Keys.ToList())}");
            foreach (var baseName in _instance.BasePlayerDiscovered.Keys)
            {
                Logger.Debug($"Players who discovered base {baseName}: {string.Join(", ", _instance.BasePlayerDiscovered[baseName].ToList())}");
            }
        }
    }
}