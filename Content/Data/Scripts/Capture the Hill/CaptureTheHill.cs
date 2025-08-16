using System;
using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.logging;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using IMyEntity = VRage.ModAPI.IMyEntity;

namespace CaptureTheHill
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class CaptureTheHillSession : MySessionComponentBase
    {
        private static bool _initialized;
        private static bool _isServer;
        private bool _printedOnce;
        private int _ticks;
        private bool _debugMode = true;

        public override void BeforeStart()
        {
            MyAPIGateway.Entities.OnEntityAdd += HandleEntityAdd;
            _isServer = (MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Multiplayer.IsServer) || !MyAPIGateway.Multiplayer.MultiplayerActive;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_initialized)
            {
                _initialized = true;
                _ticks = 0;
            }
            
            if (!_isServer)
            {
                Show("Nicht Server");
                return; // Nur auf dem Server ausführen
            }
            
            if (!_printedOnce)
            {
                _ticks++;
                if (_ticks == 300) // ~5 Sekunden bei 60 FPS
                {
                    var planets = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(planets, e => e is MyPlanet && e.Name.ToLower().Contains("earth"));

                    try
                    {
                        CheckAndCreateBasesIfNeeded(planets);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Fehler beim Überprüfen und Erstellen von Capture Bases: {ex.Message}");
                        _printedOnce = true;
                    }

                    _printedOnce = true;
                }
            }
        }
        
        private void HandleEntityAdd(IMyEntity entity)
        {
            if (entity is IMyCubeGrid && entity.Name.Contains("-capture-base"))
            {
                Show($"Entity hinzugefügt: {entity.Name}");
            }
        }

        private void CheckAndCreateBasesIfNeeded(HashSet<IMyEntity> planets)
        {
            if (planets == null || planets.Count == 0)
            {
                Show("Keine Planeten gefunden, Capture Base wird nicht erstellt.");
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

                // Show("Überprüfe Planeten: " + planet.Name + " mit Radius: " + planet.AverageRadius);

                var basesOfPlanet = existingBases.Where(e => e.Name.StartsWith(planet.Name)).ToList();
                var basesOfPlanetCount = basesOfPlanet.Count();
                // Show($"Planet: {planet.Name}, Basen: {basesOfPlanetCount}");
                var expectedPlanetBaseCount = GetExpectedPlanetBaseCount(planet.MaximumRadius / 1000);
                // Show("Planet: " + planet.Name + ", Erwartete Basenanzahl: " + expectedPlanetBaseCount);

                if (basesOfPlanetCount == expectedPlanetBaseCount || basesOfPlanetCount > expectedPlanetBaseCount)
                {
                    Show("Bereits die erwartete Anzahl oder mehr Basen sind vorhanden für " + planet.Name);
                    continue;
                }

                if (expectedPlanetBaseCount >= 1 && !basesOfPlanet.Any(e => e.Name.EndsWith("ground")))
                {
                    var groundBasePosition =
                        planet.PositionComp.GetPosition() + new Vector3D(0, 0, planet.AverageRadius + 10);
                    var groundBaseOrientation = Vector3D.Up;
                    CreateCaptureBase(planet.Name, CaptureBaseType.GROUND, groundBasePosition, groundBaseOrientation);
                    Show("Created ground base for " + planet.Name);
                }

                if (expectedPlanetBaseCount >= 2 && !basesOfPlanet.Any(e => e.Name.EndsWith("atmosphere")))
                {
                    var atmosphereBasePosition =
                        planet.PositionComp.GetPosition() + new Vector3D(0, 0, planet.AverageRadius + 10000);
                    var atmosphereBaseOrientation = Vector3D.Up;
                    CreateCaptureBase(planet.Name, CaptureBaseType.ATMOSPHERE, atmosphereBasePosition,
                        atmosphereBaseOrientation);
                    Show("Created atmosphere base for " + planet.Name);
                }

                if (expectedPlanetBaseCount == 3 && !basesOfPlanet.Any(e => e.Name.EndsWith("space")))
                {
                    var spaceBasePosition = planet.PositionComp.GetPosition() +
                                            new Vector3D(0, 0, planet.AverageRadius + 25000);
                    var spaceBaseOrientation = Vector3D.Up;
                    CreateCaptureBase(planet.Name, CaptureBaseType.SPACE, spaceBasePosition, spaceBaseOrientation);
                    Show("Created space base for " + planet.Name);
                }
            }
        }

        private int GetExpectedPlanetBaseCount(float planetRadius)
        {

            if (planetRadius > 60)
            {
                return 3;
            }

            return planetRadius > 20 ? 2 : 1;
        }

        public IMyCubeGrid CreateCaptureBase(
            string planetName, CaptureBaseType baseType, Vector3D position, Vector3D orientation)
        {
            if (string.IsNullOrEmpty(planetName) || position.IsZero() || orientation.IsZero())
            {
                Show("Ungültige Parameter für Capture Base.");
                return null;
            }

            var worldMatrix = MatrixD.CreateWorld(position, Vector3.Forward, Vector3.Up);
            var positionInWorld = worldMatrix.Translation;
            var freePosition = MyEntities.FindFreePlace(positionInWorld, 50);
            if (freePosition == null)
            {
                Show($"Kein freier Platz gefunden für Capture Base {baseType.ToString()} auf {planetName}.");
                return null;
            }
            Show("Freier Platz gefunden: " + freePosition.Value);

            try
            {

                var captureBasePrefab = MyDefinitionManager.Static.GetPrefabDefinition("CTH_Capture_Base");
                if (captureBasePrefab == null)
                {
                    Show($"Fehler beim Laden des Capture Base Prefabs für {planetName}.");
                    return null;
                }
                
                if (captureBasePrefab.CubeGrids.Length != 1)
                {
                    Show($"Fehler: Capture Base Prefab enthält falsche Anzahl an Grids ({captureBasePrefab.CubeGrids.Length}).");
                    return null;
                }
                
                captureBasePrefab.CubeGrids[0].Name = $"{planetName}-capture-base-{baseType.ToString().ToLower()}";
                captureBasePrefab.CubeGrids[0].DisplayName = $"{planetName} Capture Base ({baseType})";
                captureBasePrefab.CubeGrids[0].DestructibleBlocks = false;
                captureBasePrefab.CubeGrids[0].Editable = false;
                captureBasePrefab.CubeGrids[0].Immune = true;
                captureBasePrefab.CubeGrids[0].IsNpcSpawnedGrid = true;
                captureBasePrefab.CubeGrids[0].IsStatic = true;
                captureBasePrefab.CubeGrids[0].PositionAndOrientation = new MyPositionAndOrientation(
                    freePosition.Value,
                    orientation,
                    Vector3D.CalculatePerpendicularVector(orientation)
                );
                
                var spawnedGrids = new List<IMyCubeGrid>();
                MyAPIGateway.PrefabManager.SpawnPrefab(
                    spawnedGrids,
                    "CTH_Capture_Base",
                    positionInWorld,
                    orientation,
                    Vector3.Up, 
                    Vector3.Zero,
                    Vector3.Zero,
                    $"{planetName}-capture-base-{baseType.ToString().ToLower()}",
                    SpawningOptions.UseOnlyWorldMatrix,
                    0,
                    true,
                    () =>
                    {
                        Show($"{freePosition.Value}");
                        Show($"{spawnedGrids[0].GetPosition()}");
                    }
                );
                CreateGps(freePosition.Value, $"{planetName} Capture Base ({baseType})");

                return null;
            }
            catch (Exception e)
            {
                Show(e.Message);
                Show(e.StackTrace);
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
                throw;
            }
        }

        private void CreateGps(Vector3D position, string name)
        {
            var gpsPoint = MyAPIGateway.Session.GPS.Create(
                name,
                name,
                position,
                true
            );
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (var player in players)
            {
                MyAPIGateway.Session.GPS.AddGps(player.IdentityId, gpsPoint);
            }
        }

        private void Show(string text)
        {
            if (_debugMode)
            {
                MyAPIGateway.Utilities.ShowMessage("CTH", text);
            }
        }
    }
}