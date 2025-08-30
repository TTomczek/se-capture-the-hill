using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.spawner
{
    public static class PositionTools
    {
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
        
        public static List<Vector3D> GetSurfaceOrientation(Vector3D surfacePoint, Vector3D planetCenter)
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

        public static Vector3D FindCorrectHeightForPositionForDesiredGravity(
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
        
        public static Vector3D AdjustPositionForGroundContact(MyPlanet planet, string prefabSubtypeId, Vector3D position)
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