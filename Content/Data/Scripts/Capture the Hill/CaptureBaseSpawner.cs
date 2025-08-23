using System;
using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.logging;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace CaptureTheHill
{
    public class CaptureBaseSpawner
    {
        public static void CheckAndCreateBasesIfNeeded(HashSet<IMyEntity> planets)
        {
            if (planets == null || planets.Count == 0)
            {
                Logger.Info("Keine Planeten gefunden, Capture Base wird nicht erstellt.");
                return;
            }

            var existingBases = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(existingBases, e => e is MyCubeGrid && e.Name.Contains("-capture-base"));

            foreach (var planetEntity in planets)
            {
                var planet = planetEntity as MyPlanet;
                if (planet == null)
                {
                    continue;
                }

                var basesOfPlanet = existingBases.Where(e => e.Name.StartsWith(planet.Name)).ToList();
                var basesOfPlanetCount = basesOfPlanet.Count();
                var expectedPlanetBaseCount = GetExpectedPlanetBaseCount(planet.MaximumRadius / 1000);

                if (basesOfPlanetCount == expectedPlanetBaseCount || basesOfPlanetCount > expectedPlanetBaseCount)
                {
                    Logger.Info("Bereits die erwartete Anzahl oder mehr Basen sind vorhanden für " + planet.Name);
                    continue;
                }

                var existingPlanetBasePositions = basesOfPlanet
                    .Select(e => e.GetPosition())
                    .ToList();
                var planetBasePositionOnGround = PositionTools.GenerateMaxDistanceSurfacePoints(planet.PositionComp.GetPosition(),
                    planet.MaximumRadius, existingPlanetBasePositions, expectedPlanetBaseCount);
                
                var planetCenter = planet.PositionComp.GetPosition();

                if (expectedPlanetBaseCount >= 1 && !basesOfPlanet.Any(e => e.Name.EndsWith("ground")))
                {
                    if (planetBasePositionOnGround.Count > 0)
                    {
                        var groundBasePosition = planetBasePositionOnGround.Pop();
                        groundBasePosition = PositionTools.AdjustPositionForGroundContact(planet, "CTH_Capture_Base", groundBasePosition);
                        CreateCaptureBase(planet.Name, CaptureBaseType.Ground, groundBasePosition,
                            planetCenter, "CTH_Capture_Base");
                        Logger.Info("Created ground base for " + planet.Name);
                    }
                    else
                    {
                        Logger.Error("Kein freier Platz gefunden für Capture Base Ground auf " + planet.Name + ".");
                    }
                }

                if (expectedPlanetBaseCount >= 2 && !basesOfPlanet.Any(e => e.Name.EndsWith("atmosphere")))
                {
                    if (planetBasePositionOnGround.Count > 0)
                    {
                        var atmosphereBasePositionOnGround = planetBasePositionOnGround.Pop();
                        float gravityInterference;
                        var planetGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(atmosphereBasePositionOnGround, out gravityInterference).Length() / 9.81f;
                        var atmosphereBasePosition =
                            PositionTools.FindCorrectHeightForPositionForDesiredGravity(atmosphereBasePositionOnGround, planetGravity / 2);
                        CreateCaptureBase(planet.Name, CaptureBaseType.Atmosphere, atmosphereBasePosition,
                            planetCenter, "CTH_Capture_Base");
                        Logger.Info("Created atmosphere base for " + planet.Name);
                    }
                    else
                    {
                        Logger.Error("Kein freier Platz gefunden für Capture Base Atmosphere auf " + planet.Name + ".");
                    }
                }

                if (expectedPlanetBaseCount == 3 && !basesOfPlanet.Any(e => e.Name.EndsWith("space")))
                {
                    if (planetBasePositionOnGround.Count > 0)
                    {
                        var spaceBasePositionOnGround = planetBasePositionOnGround.Pop();
                        var spaceBasePosition =
                            PositionTools.FindCorrectHeightForPositionForDesiredGravity(spaceBasePositionOnGround, 0.0f);
                        var higherSpaceBasePosition = spaceBasePosition + Vector3D.Up * 1000;
                        CreateCaptureBase(planet.Name, CaptureBaseType.Space, higherSpaceBasePosition,
                            planetCenter, "CTH_Capture_Base");
                        Logger.Info("Created space base for " + planet.Name);
                    }
                    else
                    {
                        Logger.Error("Kein freier Platz gefunden für Capture Base Space auf " + planet.Name + ".");
                    }
                }
            }
        }

        private static int GetExpectedPlanetBaseCount(float planetRadius)
        {
            if (planetRadius > 60)
            {
                return 3;
            }

            return planetRadius > 20 ? 2 : 1;
        }

        private static void CreateCaptureBase(
            string planetName, CaptureBaseType baseType, Vector3D position, Vector3D planetCenter, String prefabSubtypeId)
        {
            if (string.IsNullOrEmpty(planetName) || position.IsZero())
            {
                Logger.Error($"Ungültige Parameter für {planetName}-capture-base-{baseType}.");
                return;
            }

            var freePosition = MyEntities.FindFreePlace(position, 5, 20, 5, 0.1f);
            if (freePosition == null)
            {
                Logger.Error($"Kein freier Platz gefunden für Capture Base {baseType.ToString()} auf {planetName}.");
                return;
            }

            var orientation = PositionTools.GetSurfaceOrientation(position, planetCenter);

            try
            {
                var captureBasePrefab = MyDefinitionManager.Static.GetPrefabDefinition(prefabSubtypeId);
                if (captureBasePrefab == null || captureBasePrefab.CubeGrids == null ||
                    captureBasePrefab.CubeGrids.Length == 0)
                {
                    Logger.Error($"Fehler beim Laden des Capture Base Prefabs für {planetName}.");
                    return;
                }

                var spawnedGrids = new List<IMyCubeGrid>();
                MyAPIGateway.PrefabManager.SpawnPrefab(
                    spawnedGrids,
                    prefabSubtypeId,
                    freePosition.Value,
                    orientation[0],
                    orientation[1],
                    Vector3.Zero,
                    Vector3.Zero,
                    null,
                    SpawningOptions.None,
                    0,
                    true,
                    () => { HandleBaseSpawned(planetName, baseType, spawnedGrids); }
                );
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
                throw;
            }
        }

        private static void HandleBaseSpawned(String planetName, CaptureBaseType baseType, List<IMyCubeGrid> spawnedGrids)
        {
            foreach (var spawnedGrid in spawnedGrids)
            {
                spawnedGrid.Name = $"{planetName}-capture-base-{baseType.ToString().ToLower()}";
                spawnedGrid.DisplayName = $"{planetName} Capture Base ({baseType})";
                spawnedGrid.IsStatic = true;
            }
        }
    }
}