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
        private static SerializableDictionary<string, List<CaptureBaseGameLogic>> _basesPerPlanet = new SerializableDictionary<string, List<CaptureBaseGameLogic>>();
        
        private static readonly string SaveFileName = "CaptureTheHillGameState.txt";
        
        // Dictionary containing points per faction
        [ProtoMember(2)]
        private static SerializableDictionary<long, int> _pointsPerFaction = new SerializableDictionary<long, int>();
        
        // Dictionary containing the players that discovered a base
        [ProtoMember(3)]
        private static SerializableDictionary<string, List<long>> _basePlayerDiscovered = new SerializableDictionary<string, List<long>>();
        
        public static void AddBaseToPlanet(string planetName, CaptureBaseGameLogic logic)
        {
            if (!_basesPerPlanet.Dictionary.ContainsKey(planetName))
            {
                _basesPerPlanet[planetName] = new List<CaptureBaseGameLogic>();
            }
            _basesPerPlanet[planetName].Add(logic);
        }
        
        public static Dictionary<string, List<CaptureBaseGameLogic>> GetAllBasesPerPlanet()
        {
            return _basesPerPlanet.Dictionary;
        }
        
        public static List<CaptureBaseGameLogic> GetAllBases()
        {
            List<CaptureBaseGameLogic> allBases = new List<CaptureBaseGameLogic>();
            foreach (var baseList in _basesPerPlanet.Dictionary.Values)
            {
                allBases.AddRange(baseList);
            }
            return allBases;
        }
        
        public static void AddPointsToFaction(long factionId, int points)
        {
            if (!_pointsPerFaction.Dictionary.ContainsKey(factionId))
            {
                _pointsPerFaction[factionId] = 0;
            }
            _pointsPerFaction[factionId] += points;
        }
        
        public static int GetPointsForFaction(long factionId)
        {
            if (_pointsPerFaction.Dictionary.ContainsKey(factionId))
            {
                return _pointsPerFaction[factionId];
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