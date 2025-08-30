using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using CaptureTheHill.logging;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill
{
    public static class CaptureBaseCaptureManager
    {
        public static void Update(Dictionary<string, List<CaptureBaseData>> basesPerPlanet)
        {
            var allBases = GetAllBases(basesPerPlanet);
            UpdateBaseCaptureProgress(allBases);
            UpdatePoints(basesPerPlanet);
        }

        private static void UpdateBaseCaptureProgress(List<CaptureBaseData> captureBases)
        {
            foreach (var cp in captureBases)
            {
                if (cp == null)
                {
                    return;
                }
                
                var baseCaptureTime = GetBaseCaptureTime(cp.CaptureBaseType);
                
                // Nobody is capturing this base
                if (cp.CurrentDominatingFaction == 0)
                {
                    continue;
                }

                // Base is already fully captured by the dominating faction
                if (cp.CurrentOwningFaction == cp.CurrentDominatingFaction && cp.CaptureProgress == baseCaptureTime)
                {
                    continue;
                }
                
                // Base is being captured by a different faction than the owning faction, defending
                if (cp.CurrentOwningFaction != 0 && cp.CurrentOwningFaction != cp.CurrentDominatingFaction && cp.FightMode == CaptureBaseFightMode.Defending && cp.CaptureProgress > 0)
                {
                    cp.CaptureProgress -= 1;
                    Logger.Info($"{cp.BaseName} is being defended by faction {cp.CurrentOwningFaction}. Capture progress: {cp.CaptureProgress}/{baseCaptureTime}");
                    if (cp.CaptureProgress == 0)
                    {
                        cp.FightMode = CaptureBaseFightMode.Attacking;
                        cp.CurrentOwningFaction = 0;
                        Logger.Info($"{cp.BaseName} is now neutral");
                        continue;
                    }
                }
                
                // Base is being captured
                if (cp.FightMode == CaptureBaseFightMode.Attacking && cp.CaptureProgress < baseCaptureTime)
                {
                    cp.CaptureProgress += 1;
                    Logger.Info($"{cp.BaseName} is being attacked by faction {cp.CurrentDominatingFaction}. Capture progress: {cp.CaptureProgress}/{baseCaptureTime}");
                    if (cp.CaptureProgress >= baseCaptureTime)
                    {
                        cp.CurrentOwningFaction = cp.CurrentDominatingFaction;
                        cp.FightMode = CaptureBaseFightMode.Defending;
                        Logger.Info($"{cp.BaseName} has been captured by faction {cp.CurrentOwningFaction}");
                    }
                }
                
                
            }
        }

        private static void UpdatePoints(Dictionary<string, List<CaptureBaseData>> basesPerPlanet)
        {
            foreach (var basesOfPlanetEntry in basesPerPlanet)
            {
                var planetOwnedByFaction = IsPlanetOwnedBySingleFaction(basesOfPlanetEntry.Value);
                if (planetOwnedByFaction != 0)
                {
                    GameStateAccessor.AddPointsToFaction(planetOwnedByFaction,
                        ModConfiguration.Instance.PointsPerOwnedPlanet);
                    return;
                }
                
                var dominatingFaction = GetDominatingFaction(basesOfPlanetEntry.Value);
                if (dominatingFaction != 0)
                {
                    GameStateAccessor.AddPointsToFaction(dominatingFaction, ModConfiguration.Instance.PointsForPlanetDominance);
                }
            }
        }
        
        private static long IsPlanetOwnedBySingleFaction(List<CaptureBaseData> basesOfPlanet)
        {
            long owningFactionId = 0;
            if (basesOfPlanet.Count == 0)
            {
                return owningFactionId;
            }

            var firstOwningFaction = basesOfPlanet[0].CurrentOwningFaction;
            if (firstOwningFaction == 0)
            {
                return owningFactionId;
            }

            foreach (var baseLogic in basesOfPlanet)
            {
                if (baseLogic.CurrentOwningFaction != firstOwningFaction)
                {
                    return owningFactionId;
                }
            }

            owningFactionId = firstOwningFaction;
            return owningFactionId;
        }

        private static int GetBaseCaptureTime(CaptureBaseType type)
        {
            switch (type)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseCaptureTimeInSeconds;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseCaptureTimeInSeconds;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseCaptureTimeInSeconds;
                default:
                    Logger.Error($"Could not determine base capture time for base type {type}, returning default of 100 seconds");
                    return 100;
            }
        }
        
        private static long GetDominatingFaction(List<CaptureBaseData> basesOfPlanet)
        {
            Dictionary<long, int> factionBaseCount = new Dictionary<long, int>();
            foreach (var baseLogic in basesOfPlanet)
            {
                if (baseLogic.CurrentOwningFaction != 0)
                {
                    if (!factionBaseCount.ContainsKey(baseLogic.CurrentOwningFaction))
                    {
                        factionBaseCount[baseLogic.CurrentOwningFaction] = 0;
                    }
                    factionBaseCount[baseLogic.CurrentOwningFaction]++;
                }
            }
            
            if (factionBaseCount.Count == 0)
            {
                return 0;
            }
            var dominatingFaction = factionBaseCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            return dominatingFaction;
        }

        private static List<CaptureBaseData> GetAllBases(Dictionary<string, List<CaptureBaseData>> basesPerPlanet)
        {
            var allBases = new List<CaptureBaseData>();
            foreach (var baseList in basesPerPlanet.Values)
            {
                allBases.AddRange(baseList);
            }
            return allBases;
        }
    }
}