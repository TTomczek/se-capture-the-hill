using System;
using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.logging;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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
            var gravityAtTestPoint =
                MyAPIGateway.Physics.CalculateNaturalGravityAt(testPoint, out gravityInterference).Length() / 9.81f;
            var vectorIncrease = testPoint / (testPoint * 100);
            while (Math.Abs(gravityAtTestPoint - targetGravityG) > tolerance)
            {
                testPoint += vectorIncrease;
                gravityAtTestPoint = MyAPIGateway.Physics.CalculateNaturalGravityAt(testPoint, out gravityInterference)
                    .Length() / 9.81f;
                if (gravityAtTestPoint < targetGravityG) break;
            }

            return testPoint;
        }

        public static Vector3D AdjustPositionForGroundContact(MyPlanet planet, string prefabSubtypeId,
            Vector3D position)
        {
            if (planet == null)
            {
                CthLogger.Warning("No planet found, cannot adjust position for ground contact.");
                return position;
            }

            var def = MyDefinitionManager.Static.GetPrefabDefinition(prefabSubtypeId);
            if (def == null || def.CubeGrids == null || def.CubeGrids.Length == 0)
            {
                CthLogger.Warning($"Prefab '{prefabSubtypeId}'not found or doesn't contain any grids.");
                return position;
            }

            // Wir verwenden das erste Grid als Referenz (bei Mehrfach-Grids nach Bedarf erweitern)
            var gridOb = def.CubeGrids[0];

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
                CthLogger.Warning($"Grid in prefab '{prefabSubtypeId}' has no blocks?");
                return position;
            }

            // Oberfläche und Normale anvisieren
            Vector3D surface = planet.GetClosestSurfacePointGlobal(position);

            return surface;
        }
    }
}