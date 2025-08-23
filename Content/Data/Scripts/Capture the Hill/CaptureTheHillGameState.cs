using System.Collections.Generic;
using CaptureTheHill.logging;
using Sandbox.Game.Entities;

namespace CaptureTheHill.config
{
    public static class CaptureTheHillGameState
    {
        // Dictionary containing all bases per planet
        private static Dictionary<string, List<MyCubeGrid>> _basesPerPlanet = new Dictionary<string, List<MyCubeGrid>>();
        
        // Dictionary containing points per faction  
        private static Dictionary<string, int> _pointsPerFaction = new Dictionary<string, int>();
        
        // Dictionary containing the players that discovered a base 
        private static Dictionary<string, List<long>> _basePlayerDiscovered = new Dictionary<string, List<long>>();
        
        public static void AddBaseToPlanet(string planetName, MyCubeGrid baseGrid)
        {
            if (!_basesPerPlanet.ContainsKey(planetName))
            {
                _basesPerPlanet[planetName] = new List<MyCubeGrid>();
            }
            _basesPerPlanet[planetName].Add(baseGrid);
        }
        
        public static List<MyCubeGrid> GetBasesForPlanet(string planetName)
        {
            if (_basesPerPlanet.ContainsKey(planetName))
            {
                return _basesPerPlanet[planetName];
            }
            return new List<MyCubeGrid>();
        }
        
        public static void AddPointsToFaction(string factionName, int points)
        {
            if (!_pointsPerFaction.ContainsKey(factionName))
            {
                _pointsPerFaction[factionName] = 0;
            }
            _pointsPerFaction[factionName] += points;
        }
        
        public static int GetPointsForFaction(string factionName)
        {
            if (_pointsPerFaction.ContainsKey(factionName))
            {
                return _pointsPerFaction[factionName];
            }
            return 0;
        }
        
        public static void AddPlayerToBaseDiscovery(string baseName, long playerId)
        {
            Logger.Debug($"Adding player {playerId} to base discovery for {baseName}");
            if (!_basePlayerDiscovered.ContainsKey(baseName))
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
            if (_basePlayerDiscovered.ContainsKey(baseName))
            {
                return _basePlayerDiscovered[baseName];
            }
            return new List<long>();
        }
    }
}