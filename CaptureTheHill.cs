using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMyEntity = VRage.ModAPI.IMyEntity;

namespace CaptureTheHill
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class CaptureTheHillSession : MySessionComponentBase
    {
        private bool _initialized;
        private bool _printedOnce;
        private int _ticks;
        private bool _debugMode = true;

        public override void BeforeStart()
        {
            // Add Message listener
            MyAPIGateway.Utilities.MessageEntered += HandleChatCommands;
        }

        public override void UpdateAfterSimulation()
        {
            if (!_initialized)
            {
                _initialized = true;
                _ticks = 0;
            }

            if (!_printedOnce)
            {
                _ticks++;
                if (_ticks == 300) // ~5 Sekunden bei 60 FPS
                {
                    var planets = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(planets, e => e is MyPlanet);

                    CheckAndCreateBasesIfNeeded(planets);

                    // PrintPlanetsAndGravity(planets);
                    // CreateGpsMarker(planets);

                    _printedOnce = true;
                }
            }
        }

        private void HandleChatCommands(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/cth ", StringComparison.OrdinalIgnoreCase))
            {
                var command = messageText.Substring(5).Trim();
                switch (command.ToLowerInvariant())
                {
                    case "debug":
                        _debugMode = !_debugMode;
                        Show($"Debug-Modus ist jetzt {(_debugMode ? "aktiviert" : "deaktiviert")}.");
                        break;
                    default:
                        Show("Unbekannter Befehl. Verfügbare Befehle: /cth debug");
                        break;
                }

                sendToOthers = false;
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
                Show("Überprüfe Planeten: " + planet.Name + " mit Radius: " + planet.AverageRadius);

                var basesOfPlanet = existingBases.Where(e => e.Name.StartsWith(planet.Name)).ToList();
                var basesOfPlanetCount = basesOfPlanet.Count();
                Show($"Planet: {planet.Name}, Basen: {basesOfPlanetCount}");
                var expectedPlanetBaseCount = GetExpectedPlanetBaseCount(planet.MaximumRadius / 1000);
                Show("Planet: " + planet.Name + ", Erwartete Basenanzahl: " + expectedPlanetBaseCount);

                if (basesOfPlanetCount == expectedPlanetBaseCount || basesOfPlanetCount > expectedPlanetBaseCount)
                {
                    Show("Bereits die erwartete Anzahl oder mehr Basen sind vorhanden für " + planet.Name);
                    continue;
                }

                if (expectedPlanetBaseCount >= 1 && !basesOfPlanet.Any(e => e.Name.EndsWith("ground")))
                {
                    Show("Create ground base for " + planet.Name);
                    var groundBasePosition = planet.PositionComp.GetPosition() + new Vector3D(0, 0, planet.AverageRadius + 1);
                    var groundBaseOrientation = Vector3D.Forward;
                    SpawnPrefab("CaptureTheHillGroundBase", groundBasePosition, groundBaseOrientation);
                        
                }
                
                if (expectedPlanetBaseCount >= 2 && !basesOfPlanet.Any(e => e.Name.EndsWith("atmosphere")))
                {
                    Show("Create atmosphere base for " + planet.Name);
                }
                
                if (expectedPlanetBaseCount == 3 && !basesOfPlanet.Any(e => e.Name.EndsWith("space")))
                {
                    Show("Create space base for " + planet.Name);
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
            string planetName, BaseType baseType, Vector3D position, Vector3D orientation)
        {
            if (string.IsNullOrEmpty(planetName) || position.IsZero() || orientation.IsZero())
            {
                Show("Ungültige Parameter für Capture Base.");
                return null;
            }

            var captureBaseObjectBuilder = LoadGridFromBlueprint("CaptureBase");
            if (captureBaseObjectBuilder == null)
            {
                Show($"Fehler beim Laden des Capture Base Blueprints für {planetName}.");
                return null;
            }
            captureBaseObjectBuilder.Name = $"{planetName}-capture-base-{baseType.ToString().ToLower()}";
            captureBaseObjectBuilder.PositionAndOrientation = new MyPositionAndOrientation(position, orientation, Vector3D.Up);
            captureBaseObjectBuilder.GridSizeEnum = MyCubeSize.Large;
            captureBaseObjectBuilder.IsStatic = true;
            captureBaseObjectBuilder.Immune = true;
            captureBaseObjectBuilder.DestructibleBlocks = false;
            captureBaseObjectBuilder.Editable = false;
            
            var grid = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(captureBaseObjectBuilder);

            if (grid == null)
            {
                Show($"Fehler beim Erstellen der Capture Base: {planetName}");
                return null;
            }

            Show($"Capture Base '{grid.Name}' erstellt.");
            return (MyCubeGrid)grid;
        }
        
        private MyObjectBuilder_CubeGrid LoadGridFromBlueprint(string blueprintName)
        {
            if (string.IsNullOrEmpty(blueprintName))
            {
                Show("Ungültiger Blueprint-Name.");
                return null;
            }

            var modContentPath = MyAPIGateway.Utilities.GamePaths.ContentPath;
            var blueprintPath = $"{modContentPath}/Data/Blueprints/{blueprintName}.sbc";

            var blueprintXmlReader = MyAPIGateway.Utilities.ReadFileInModLocation(blueprintPath, ModContext.ModItem);
            var blueprintXml = blueprintXmlReader.ReadToEnd();
            blueprintXmlReader.Close();
            
            if (string.IsNullOrEmpty(blueprintXml))
            {
                Show($"Blueprint '{blueprintName}' nicht gefunden oder leer.");
                return null;
            }

            var objectBuilder = MyAPIGateway.Utilities.SerializeFromXML<MyObjectBuilder_CubeGrid>(blueprintXml);
            if (objectBuilder == null)
            {
                Show($"Fehler beim Laden des Blueprints '{blueprintName}'.");
                return null;
            }

            return objectBuilder;
        }

        private void PrintPlanetsAndGravity(HashSet<IMyEntity> planets = null)
        {
            if (planets == null)
            {
                planets = new HashSet<IMyEntity>();
            }

            try
            {
                if (planets.Count == 0)
                {
                    Show("Keine Planeten in dieser Welt gefunden.");
                    return;
                }

                Show("Planeten & Oberflächen-Gravitation (G):");

                foreach (var planetEntity in planets)
                {
                    var planet = planetEntity as MyPlanet;
                    if (planet == null)
                        continue;

                    var name = planet.Name;
                    var center = planet.PositionComp.GetPosition();

                    double radius = planet.AverageRadius;
                    var samplePoint = center + new Vector3D(radius + 2.0, 0, 0);

                    float gravityInterference;
                    Vector3D gVec =
                        MyAPIGateway.Physics.CalculateNaturalGravityAt(samplePoint, out gravityInterference);
                    double g = gVec.Length();
                    double gravityInGs = g / 9.81;

                    Show($"• {name}: {gravityInGs:0.00} G");
                }
            }
            catch (Exception ex)
            {
                Show($"Fehler bei Gravitationsermittlung: {ex.Message}");
            }
        }

        private void CreateGpsMarker(HashSet<IMyEntity> planets)
        {
            if (planets == null || planets.Count == 0)
            {
                Show("Keine Planeten gefunden, GPS Marker wird nicht erstellt.");
                return;
            }

            try
            {
                foreach (var planetEntity in planets)
                {
                    var planet = planetEntity as MyPlanet;
                    if (planet == null)
                        continue;

                    var gps = MyAPIGateway.Session.GPS.Create(
                        $"{planet.Name} Capture Base",
                        $"Hill base",
                        // Position slightly above the planet's surface
                        planet.PositionComp.GetPosition() + new Vector3D(0, 0, planet.AverageRadius + 0.5),
                        true,
                        false);

                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
                }

                Show("GPS Marker für Planeten erstellt.");
            }
            catch (Exception ex)
            {
                Show($"Fehler beim Erstellen des GPS Markers: {ex.Message}");
            }
        }

        private void Show(string text)
        {
            // Lokale Chat-/HUD-Nachricht
            if (_debugMode)
            {
                MyAPIGateway.Utilities.ShowMessage("CTH", text);
            }
        }
    }
}

public enum BaseType
{
    GROUND,
    ATMOSPHERE,
    SPACE
}