using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill
{
    public static class FactionUtils
    {
        
        public static string GetFactionTagById(long factionId)
        {
            IMyFaction faction = GetFactionById(factionId);
            return faction?.Tag ?? "N/A";
        }
        
        public static string GetFactionNameById(long factionId)
        {
            IMyFaction faction = GetFactionById(factionId);
            return faction?.Name ?? "N/A";
        }

        private static IMyFaction GetFactionById(long factionId)
        {
            IMyFaction faction =  MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
            return faction;
        }
    }
}