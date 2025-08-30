using System.Collections.Generic;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill;
using ProtoBuf;

namespace CaptureTheHill.config
{
    [ProtoContract]
    public class GameStateStore
    {
        
        // Dictionary containing all bases per planet
        [ProtoMember(1)]
        public Dictionary<string, List<CaptureBaseData>> BasesPerPlanet = new Dictionary<string, List<CaptureBaseData>>();
        
        // Dictionary containing points per faction
        [ProtoMember(2)]
        public Dictionary<long, int> PointsPerFaction = new Dictionary<long, int>();
        
        // Dictionary containing the players that discovered a base
        [ProtoMember(3)]
        public Dictionary<string, List<long>> BasePlayerDiscovered = new Dictionary<string, List<long>>();
        
    }
}