using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;

namespace CaptureTheHill
{

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class CaptureTheHillSession : MySessionComponentBase
    {

        public static CaptureTheHillSession Instance;

        private bool _initialized;
        private bool _printedOnce;
        private int _ticks;

        public override void LoadData()
        {
            Instance = this;
        }

        public override void BeforeStart()
        {
            try
            {
                
            }
            catch { /* ignorieren falls nicht verfügbar */ }
        }

        public override void UpdateAfterSimulation()
        {
            if (!_initialized)
            {
                _initialized = true;
                _ticks = 0;
            }

            // Nach kurzer Wartezeit einmal automatisch ausgeben (damit die Planeten sicher geladen sind)
            if (!_printedOnce)
            {
                _ticks++;
                if (_ticks == 300) // ~5 Sekunden bei 60 FPS
                {
                    PrintPlanetsAndGravity();
                    _printedOnce = true;
                }
            }
        }

        protected override void UnloadData()
        {
            try
            {
                
            }
            catch { /* no-op */ }
        }

        private void PrintPlanetsAndGravity()
        {
            try
            {
                var planets = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(planets, e => e is MyPlanet);

                if (planets.Count == 0)
                {
                    Show("Keine Planeten in dieser Welt gefunden.");
                    return;
                }

                Show("Planeten & Oberflächen-Gravitation (m/s²):");

                foreach (var planetEntity in planets)
                {
                    var planet = planetEntity as MyPlanet;
                    if (planet == null)
                        continue;

                    var name = planet.Name;
                    var center = planet.PositionComp.GetPosition();

                    // Annäherung: Sample-Punkt knapp über der Oberfläche
                    double radius = planet.AverageRadius;
                    var samplePoint = center + new Vector3D(radius + 2.0, 0, 0);

                    // Natürliche Gravitation am Sample-Punkt
                    float gravityInterference;
                    Vector3D gVec = MyAPIGateway.Physics.CalculateNaturalGravityAt(samplePoint, out gravityInterference);
                    double g = gVec.Length();

                    Show($"• {name}: {g:0.00} m/s²");
                }
            }
            catch (Exception ex)
            {
                Show($"Fehler bei Gravitationsermittlung: {ex.Message}");
            }
        }

        private void Show(string text)
        {
            // Lokale Chat-/HUD-Nachricht
            MyAPIGateway.Utilities.ShowMessage("CTH", text);
        }
    }

}
