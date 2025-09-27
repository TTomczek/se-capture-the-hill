using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.logging;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.faction
{
    public static class FactionUtils
    {
        public static string GetFactionTagById(long factionId)
        {
            var faction = GetFactionById(factionId);
            return faction?.Tag ?? "N/A";
        }

        public static string GetFactionNameById(long factionId)
        {
            var faction = GetFactionById(factionId);
            return faction?.Name ?? "N/A";
        }

        public static List<ulong> GetSteamIdsOfPlayersInFaction(long factionId)
        {
            var faction = GetFactionById(factionId);
            if (faction == null)
            {
                CthLogger.Warning($"Could not find faction with Id {factionId}");
                return new List<ulong>();
            }

            var playerIds = faction.Members.Keys.ToList();

            return playerIds.Select(playerId => MyAPIGateway.Players.TryGetSteamId(playerId)).ToList();
        }

        public static long GetOneFactionLeaderId(long factionId)
        {
            var faction = GetFactionById(factionId);
            return faction?.Members.FirstOrDefault(m => m.Value.IsLeader).Key ?? 0;
        }

        private static IMyFaction GetFactionById(long factionId)
        {
            return MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
        }
    }
}