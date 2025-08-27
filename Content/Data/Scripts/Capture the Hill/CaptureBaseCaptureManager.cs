using System;
using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using CaptureTheHill.logging;
using Sandbox.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill
{
    public static class CaptureBaseCaptureManager
    {
        public static void Update()
        {
            var allBases = CaptureTheHillGameState.GetAllBases();
            UpdateBaseCaptureProgress(allBases);
            var allBasesPerPlanet =
                CaptureTheHillGameState.GetAllBasesPerPlanet();
            UpdatePoints(allBasesPerPlanet);
        }

        private static void UpdateBaseCaptureProgress(List<CaptureBaseGameLogic> captureBases)
        {
            foreach (var cp in captureBases)
            {
                if (cp == null)
                {
                    return;
                }
                
                var baseCaptureTime = GetBaseCaptureTime(cp.CaptureBaseType);

                // Dominated by owning faction
                if (cp.CurrentDominatingFaction == cp.CurrentOwningFaction && cp.CurrentOwningFaction != 0)
                {
                    // Still capturing
                    if (cp.CaptureProgress < baseCaptureTime)
                    {
                        cp.CaptureProgress++;
                    }
                }
                else
                {
                    if (cp.CurrentDominatingFaction == cp.PreviousDominatingFaction && cp.CurrentDominatingFaction != 0)
                    {
                        // Capturing base owned by faction a by faction b
                        if (cp.CaptureProgress > 0 && cp.FightMode == CaptureBaseFightMode.Attacking)
                        {
                            cp.FightMode = CaptureBaseFightMode.Attacking;
                            cp.CaptureProgress--;
                        }
                        else
                        {
                            if (cp.FightMode == CaptureBaseFightMode.Attacking )
                            {
                                cp.CurrentOwningFaction = 0;
                                cp.FightMode = CaptureBaseFightMode.Defending;
                            } 
                            else
                            {
                                if (cp.CaptureProgress < baseCaptureTime)
                                {
                                    cp.CaptureProgress++;
                                }
                                else
                                {
                                    cp.CurrentOwningFaction = cp.CurrentDominatingFaction;
                                    cp.FightMode = CaptureBaseFightMode.Defending;
                                }
                            }
                        }
                    }
                }
                cp.PreviousDominatingFaction = cp.CurrentDominatingFaction;
                cp.CurrentDominatingFaction = 0;
            }
        }

        private static void UpdatePoints(Dictionary<string, List<CaptureBaseGameLogic>> basesPerPlanet)
        {
            foreach (var basesOfPlanetEntry in basesPerPlanet)
            {
                var planetOwnedByFaction = IsPlanetOwnedBySingleFaction(basesOfPlanetEntry.Value);
                if (planetOwnedByFaction != 0)
                {
                    CaptureTheHillGameState.AddPointsToFaction(planetOwnedByFaction,
                        ModConfiguration.Instance.PointsPerOwnedPlanet);
                    return;
                }
                
                var dominatingFaction = GetDominatingFaction(basesOfPlanetEntry.Value);
                if (dominatingFaction != 0)
                {
                    CaptureTheHillGameState.AddPointsToFaction(dominatingFaction, ModConfiguration.Instance.PointsForPlanetDominance);
                }
            }
        }
        
        private static long IsPlanetOwnedBySingleFaction(List<CaptureBaseGameLogic> basesOfPlanet)
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
        
        private static long GetDominatingFaction(List<CaptureBaseGameLogic> basesOfPlanet)
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
            
            var dominatingFaction = factionBaseCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            return dominatingFaction;
        }
    }
}