using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using ProtoBuf;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.state
{
    [ProtoContract]
    public class CaptureBaseData
    {
        [ProtoMember(1)] public string PlanetName;
        [ProtoMember(2)] public string BaseName;
        [ProtoMember(3)] public string BaseDisplayName;
        [ProtoMember(4)] public CaptureBaseType CaptureBaseType;
        [ProtoMember(5)] public long CurrentOwningFaction = 0;
        [ProtoMember(6)] public long CurrentDominatingFaction;
        [ProtoMember(7)] public int CaptureProgress = 0;
        [ProtoMember(8)] public CaptureBaseFightMode FightMode;

        public CaptureBaseData()
        {
        }

        public CaptureBaseData(string planetName, string baseName, string baseDisplayName,
            CaptureBaseType captureBaseType, long currentOwningFaction = 0,
            long currentDominatingFaction = 0, int captureProgress = 0,
            CaptureBaseFightMode fightMode = CaptureBaseFightMode.Attacking)
        {
            PlanetName = planetName;
            BaseName = baseName;
            BaseDisplayName = baseDisplayName;
            CaptureBaseType = captureBaseType;
            CurrentOwningFaction = currentOwningFaction;
            CurrentDominatingFaction = currentDominatingFaction;
            CaptureProgress = captureProgress;
            FightMode = fightMode;
        }

        public override string ToString()
        {
            return
                $"{nameof(PlanetName)}: {PlanetName}, {nameof(BaseName)}: {BaseName}, {nameof(BaseDisplayName)}: {BaseDisplayName}, {nameof(CaptureBaseType)}: {CaptureBaseType}, {nameof(CurrentOwningFaction)}: {CurrentOwningFaction}, {nameof(CurrentDominatingFaction)}: {CurrentDominatingFaction}, {nameof(CaptureProgress)}: {CaptureProgress}, {nameof(FightMode)}: {FightMode}";
        }
    }
}