using System;
using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.logging;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using IMyEntity = VRage.ModAPI.IMyEntity;

namespace CaptureTheHill
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class CaptureTheHillSession : MySessionComponentBase
    {
        private static bool _initialized;
        private static bool _isServer;
        private int _ticks;
        private Dictionary<String, List<MyCubeGrid>> basesPerPlanet = new Dictionary<String, List<MyCubeGrid>>();

        public override void BeforeStart()
        {
            MyAPIGateway.Entities.OnEntityAdd += HandleEntityAdd;
            _isServer = (MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Multiplayer.IsServer) ||
                        !MyAPIGateway.Multiplayer.MultiplayerActive;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isServer)
            {
                Show("Nicht Server");
                return;
            }

            if (!_initialized)
            {
                _initialized = true;
                _ticks = 0;
            }

            _ticks++;
            if (_ticks == 300)
            {
                var planets = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(planets, e => e is MyPlanet);

                try
                {
                    CheckAndCreateBasesIfNeeded(planets);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Fehler beim Überprüfen und Erstellen von Capture Bases: {ex.Message}");
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

                var basesOfPlanet = existingBases.Where(e => e.Name.StartsWith(planet.Name)).ToList();
                var basesOfPlanetCount = basesOfPlanet.Count();
                var expectedPlanetBaseCount = GetExpectedPlanetBaseCount(planet.MaximumRadius / 1000);

                if (basesOfPlanetCount == expectedPlanetBaseCount || basesOfPlanetCount > expectedPlanetBaseCount)
                {
                    Show("Bereits die erwartete Anzahl oder mehr Basen sind vorhanden für " + planet.Name);
                    continue;
                }

                var existingPlanetBasePositions = basesOfPlanet
                    .Select(e => e.GetPosition())
                    .ToList();
                var planetBasePositionOnGround = GenerateMaxDistanceSurfacePoints(planet.PositionComp.GetPosition(),
                    planet.MaximumRadius, existingPlanetBasePositions, expectedPlanetBaseCount);
                
                var planetCenter = planet.PositionComp.GetPosition();

                if (expectedPlanetBaseCount >= 1 && !basesOfPlanet.Any(e => e.Name.EndsWith("ground")))
                {
                    if (planetBasePositionOnGround.Count > 0)
                    {
                        var groundBasePosition = planetBasePositionOnGround.Pop();
                        groundBasePosition = AdjustPositionForGroundContact(planet, "CTH_Capture_Base", groundBasePosition);
                        CreateCaptureBase(planet.Name, CaptureBaseType.Ground, groundBasePosition,
                            planetCenter, "CTH_Capture_Base");
                        Show("Created ground base for " + planet.Name);
                    }
                    else
                    {
                        Show("Kein freier Platz gefunden für Capture Base Ground auf " + planet.Name + ".");
                    }
                }

                if (expectedPlanetBaseCount >= 2 && !basesOfPlanet.Any(e => e.Name.EndsWith("atmosphere")))
                {
                    if (planetBasePositionOnGround.Count > 0)
                    {
                        var atmosphereBasePositionOnGround = planetBasePositionOnGround.Pop();
                        float gravityInterference;
                        var planetGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(atmosphereBasePositionOnGround, out gravityInterference).Length() / 9.81f;
                        Show($"Planet {planet.Name} Gravity: {planetGravity} G, Half Gravity: {planetGravity / 2} G");
                        var atmosphereBasePosition =
                            FindCorrectHeightForPositionForDesiredGravity(atmosphereBasePositionOnGround, planetGravity / 2);
                        CreateCaptureBase(planet.Name, CaptureBaseType.Atmosphere, atmosphereBasePosition,
                            planetCenter, "CTH_Capture_Base");
                        Show("Created atmosphere base for " + planet.Name);
                    }
                    else
                    {
                        Show("Kein freier Platz gefunden für Capture Base Atmosphere auf " + planet.Name + ".");
                    }
                }

                if (expectedPlanetBaseCount == 3 && !basesOfPlanet.Any(e => e.Name.EndsWith("space")))
                {
                    if (planetBasePositionOnGround.Count > 0)
                    {
                        var spaceBasePositionOnGround = planetBasePositionOnGround.Pop();
                        var spaceBasePosition =
                            FindCorrectHeightForPositionForDesiredGravity(spaceBasePositionOnGround, 0.0f);
                        var higherSpaceBasePosition = spaceBasePosition + Vector3D.Up * 1000;
                        CreateCaptureBase(planet.Name, CaptureBaseType.Space, higherSpaceBasePosition,
                            planetCenter, "CTH_Capture_Base");
                        Show("Created space base for " + planet.Name);
                    }
                    else
                    {
                        Show("Kein freier Platz gefunden für Capture Base Space auf " + planet.Name + ".");
                    }
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

        private void CreateCaptureBase(
            string planetName, CaptureBaseType baseType, Vector3D position, Vector3D planetCenter, String prefabSubtypeId)
        {
            if (string.IsNullOrEmpty(planetName) || position.IsZero())
            {
                Show($"Ungültige Parameter für {planetName}-capture-base-{baseType}.");
                return;
            }

            var freePosition = MyEntities.FindFreePlace(position, 5, 20, 5, 0.1f);
            if (freePosition == null)
            {
                Show($"Kein freier Platz gefunden für Capture Base {baseType.ToString()} auf {planetName}.");
                return;
            }

            var orientation = GetSurfaceOrientation(position, planetCenter);

            try
            {
                var captureBasePrefab = MyDefinitionManager.Static.GetPrefabDefinition(prefabSubtypeId);
                if (captureBasePrefab == null || captureBasePrefab.CubeGrids == null ||
                    captureBasePrefab.CubeGrids.Length == 0)
                {
                    Show($"Fehler beim Laden des Capture Base Prefabs für {planetName}.");
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
                CreateGps(freePosition.Value, $"{planetName} Capture Base ({baseType})");
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

        private void HandleBaseSpawned(String planetName, CaptureBaseType baseType, List<IMyCubeGrid> spawnedGrids)
        {
            foreach (var spawnedGrid in spawnedGrids)
            {
                spawnedGrid.Name = $"{planetName}-capture-base-{baseType.ToString().ToLower()}";
                spawnedGrid.DisplayName = $"{planetName} Capture Base ({baseType})";
                spawnedGrid.IsStatic = true;
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
        
        public static List<Vector3D> GenerateMaxDistanceSurfacePoints(
            Vector3D planetCenter,
            double planetRadius,
            List<Vector3D> existingPoints,
            int desiredCount)
        {
            List<Vector3D> result = new List<Vector3D>();

            List<Vector3D> projectedExisting = existingPoints
                .Select(p => planetCenter + Vector3D.Normalize(p - planetCenter) * planetRadius)
                .ToList();

            double offset = 2.0 / desiredCount;
            double increment = Math.PI * (3.0 - Math.Sqrt(5.0));

            for (int i = existingPoints.Count; i < desiredCount; i++)
            {
                double y = ((i * offset) - 1) + (offset / 2);
                double r = Math.Sqrt(1 - y * y);
                double phi = i * increment;

                double x = Math.Cos(phi) * r;
                double z = Math.Sin(phi) * r;

                Vector3D unit = new Vector3D(x, y, z);
                Vector3D candidate = planetCenter + unit * planetRadius;
                
                double minDist = projectedExisting
                    .Select(p => Vector3D.Distance(p, candidate))
                    .DefaultIfEmpty(double.MaxValue)
                    .Min();
                
                double minNewDist = result
                    .Select(p => Vector3D.Distance(p, candidate))
                    .DefaultIfEmpty(double.MaxValue)
                    .Min();

                double score = Math.Min(minDist, minNewDist);

                if (score > planetRadius * 0.5)
                {
                    result.Add(candidate);
                    if (result.Count + existingPoints.Count >= desiredCount)
                        break;
                }
            }

            return result;
        }
        
        public List<Vector3D> GetSurfaceOrientation(Vector3D surfacePoint, Vector3D planetCenter)
        {
            Vector3D up = Vector3D.Normalize(surfacePoint - planetCenter);

            Vector3D reference = Math.Abs(Vector3D.Dot(up, Vector3D.Up)) > 0.99
                ? Vector3D.Right
                : Vector3D.Up;

            Vector3D forward = Vector3D.Normalize(Vector3D.Cross(up, reference));
            Vector3D right = Vector3D.Normalize(Vector3D.Cross(forward, up));

            return new List<Vector3D>
            {
                forward,
                up,
                right
            };
        }

        private Vector3D FindCorrectHeightForPositionForDesiredGravity(
            Vector3D surfacePoint,
            double targetGravityG,
            double tolerance = 0.01f
            )
        {
            if (targetGravityG < 0)
                return surfacePoint;

            Vector3D testPoint = surfacePoint;
            float gravityInterference;
            var gravityAtTestPoint = MyAPIGateway.Physics.CalculateNaturalGravityAt(testPoint, out gravityInterference).Length() / 9.81f;
            var vectorIncrease = testPoint / (testPoint * 100);
            while (Math.Abs(gravityAtTestPoint - targetGravityG) > tolerance)
            {
                testPoint += vectorIncrease;
                gravityAtTestPoint = MyAPIGateway.Physics.CalculateNaturalGravityAt(testPoint, out gravityInterference).Length() / 9.81f;
                if (gravityAtTestPoint < targetGravityG) break;
            }

            return testPoint;
        }

        private void Show(string text)
        {
            MyAPIGateway.Utilities.ShowMessage("CTH", text);
        }
        
        private Vector3D AdjustPositionForGroundContact(MyPlanet planet, string prefabSubtypeId, Vector3D position)
        {
            if (planet == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Spawn", "Kein Planet übergeben.");
                return position;
            }

            var def = MyDefinitionManager.Static.GetPrefabDefinition(prefabSubtypeId);
            if (def == null || def.CubeGrids == null || def.CubeGrids.Length == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Spawn", $"Prefab '{prefabSubtypeId}' nicht gefunden oder leer.");
                return position;
            }

            // Wir verwenden das erste Grid als Referenz (bei Mehrfach-Grids nach Bedarf erweitern)
            var gridOb = def.CubeGrids[0];

            // Grid-CubeSize in Metern (Large=2.5, Small=0.5)
            double cellSize = gridOb.GridSizeEnum == MyCubeSize.Large ? 2.5 : 0.5;

            // Unterste Zellenlage (lokal, in Cell-Koordinaten) bestimmen:
            // MyObjectBuilder_CubeBlock.Min ist die minimale Zelle eines (ggf. mehrzelligen) Blocks.
            int minCellY = int.MaxValue;
            foreach (var b in gridOb.CubeBlocks)
            {
                if (b.Min.Y < minCellY)
                    minCellY = b.Min.Y;
            }
            if (minCellY == int.MaxValue)
            {
                MyAPIGateway.Utilities.ShowMessage("Spawn", "Prefab enthält keine Blöcke.");
                return position;
            }

            // Oberfläche und Normale anvisieren
            Vector3D surface = planet.GetClosestSurfacePointGlobal(position);
            Vector3D center = planet.PositionComp.GetPosition();
            Vector3D up = Vector3D.Normalize(surface - center);
            
            // Die Y-Achse des Grids wird mit 'up' ausgerichtet.
            // Ziel: Zentrum der untersten Zelle liegt GENAU auf der Oberfläche -> Block halb im Boden.
            // Zentrum der untersten Zelle liegt bei (minCellY * cellSize) relativ zur Grid-Origine entlang Up.
            Vector3D originAtSurface = surface - up * ((minCellY - cellSize) * cellSize);

            // Minimaler numerischer Epsilon-Versatz in den Boden, um Voxel-Schnitt robust zu garantieren (optional)
            originAtSurface -= up * 0.1;
            
            return originAtSurface;
        }
    }
}