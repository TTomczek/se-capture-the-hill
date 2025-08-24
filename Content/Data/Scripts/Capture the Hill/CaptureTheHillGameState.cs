using System.Collections.Generic;
using System.Xml.Serialization;
using CaptureTheHill.logging;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Serialization;

namespace CaptureTheHill.config
{
    [ProtoContract]
    public class CaptureTheHillGameState
    {
        
        public static CaptureTheHillGameState Instance { get; private set; }
        
        // Dictionary containing all bases per planet
        [ProtoMember(1)]
        private static SerializableDictionary<string, List<MyCubeGrid>> _basesPerPlanet = new SerializableDictionary<string, List<MyCubeGrid>>();
        
        private static readonly string SaveFileName = "CaptureTheHillGameState.txt";
        
        // Dictionary containing points per faction
        [ProtoMember(2)]
        private static SerializableDictionary<string, int> _pointsPerFaction = new SerializableDictionary<string, int>();
        
        // Dictionary containing the players that discovered a base
        [ProtoMember(3)]
        private static SerializableDictionary<string, List<long>> _basePlayerDiscovered = new SerializableDictionary<string, List<long>>();
        
        public static void AddBaseToPlanet(string planetName, MyCubeGrid baseGrid)
        {
            if (!_basesPerPlanet.Dictionary.ContainsKey(planetName))
            {
                _basesPerPlanet[planetName] = new List<MyCubeGrid>();
            }
            _basesPerPlanet[planetName].Add(baseGrid);
        }
        
        public static List<MyCubeGrid> GetBasesForPlanet(string planetName)
        {
            if (_basesPerPlanet.Dictionary.ContainsKey(planetName))
            {
                return _basesPerPlanet[planetName];
            }
            return new List<MyCubeGrid>();
        }
        
        public static void AddPointsToFaction(string factionName, int points)
        {
            if (!_pointsPerFaction.Dictionary.ContainsKey(factionName))
            {
                _pointsPerFaction[factionName] = 0;
            }
            _pointsPerFaction[factionName] += points;
        }
        
        public static int GetPointsForFaction(string factionName)
        {
            if (_pointsPerFaction.Dictionary.ContainsKey(factionName))
            {
                return _pointsPerFaction[factionName];
            }
            return 0;
        }
        
        public static void AddPlayerToBaseDiscovery(string baseName, long playerId)
        {
            Logger.Debug($"Adding player {playerId} to base discovery for {baseName}");
            if (!_basePlayerDiscovered.Dictionary.ContainsKey(baseName))
            {
                _basePlayerDiscovered[baseName] = new List<long>();
            }
            if (!_basePlayerDiscovered[baseName].Contains(playerId))
            {
                _basePlayerDiscovered[baseName].Add(playerId);
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
            if (_basePlayerDiscovered.Dictionary.ContainsKey(baseName))
            {
                return _basePlayerDiscovered[baseName];
            }
            return new List<long>();
        }
        
        public static void SaveState()
        {
            using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(SaveFileName, typeof(CaptureTheHillGameState)))
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(Instance);
                writer.Write(bytes);
            }
            Logger.Info($"Game state saved to {SaveFileName}");
        }
        
        public static void LoadState()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(SaveFileName, typeof(CaptureTheHillGameState)))
            {
                using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(SaveFileName, typeof(CaptureTheHillGameState)))
                {
                    var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                    Instance = MyAPIGateway.Utilities.SerializeFromBinary<CaptureTheHillGameState>(bytes);
                    Logger.Info($"Game state loaded from {SaveFileName}");
                }
            }
        }
    }
}