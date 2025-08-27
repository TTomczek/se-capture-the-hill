using System;
using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using CaptureTheHill.logging;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace CaptureTheHill
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid), false, "CTH_Capture_Base")]
    public class CaptureBaseGameLogic : MyGameLogicComponent
    {
        private BoundingSphereD _captureSphere;
        private BoundingSphereD _discoverySphere;
        private int _run = 0;
        
        public MyCubeGrid CaptureBaseGrid { get; private set; }
        public CaptureBaseType CaptureBaseType { get; private set; }
        public long CurrentOwningFaction = 0;
        public long CurrentDominatingFaction;
        public int CaptureProgress = 0;
        public CaptureBaseFightMode FightMode = CaptureBaseFightMode.Attacking;

        public CaptureBaseGameLogic()
        {
        }

        public CaptureBaseGameLogic(MyCubeGrid captureBaseGrid,
            BoundingSphereD captureSphere = default(BoundingSphereD),
            BoundingSphereD discoverySphere = default(BoundingSphereD), int run = 0, long currentOwningFaction = 0,
            long currentDominatingFaction = 0, long previousDominatingFaction = 0, int captureProgress = 0,
            CaptureBaseFightMode fightMode = CaptureBaseFightMode.Attacking,
            CaptureBaseType captureBaseType = CaptureBaseType.Ground)
        {
            if (captureBaseGrid == null) throw new ArgumentNullException(nameof(captureBaseGrid));
            CaptureBaseGrid = captureBaseGrid;
            _captureSphere = captureSphere;
            _discoverySphere = discoverySphere;
            _run = run;
            CurrentOwningFaction = currentOwningFaction;
            CurrentDominatingFaction = currentDominatingFaction;
            CaptureProgress = captureProgress;
            FightMode = fightMode;
            CaptureBaseType = captureBaseType;
        }

        public override void Init(MyObjectBuilder_EntityBase captureBaseGrid)
        {
            base.Init(captureBaseGrid);
            CaptureBaseGrid = (MyCubeGrid)Entity;
            CaptureBaseType = GetCaptureBaseType(CaptureBaseGrid.Name);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            CaptureTheHillGameState.AddBaseToPlanet(CaptureBaseGrid.Name.Split('-')[0], this);
            _captureSphere = new BoundingSphereD(CaptureBaseGrid.PositionComp.GetPosition(), GetCaptureRadius());
            _discoverySphere = new BoundingSphereD(CaptureBaseGrid.PositionComp.GetPosition(), GetDiscoveryRadius());
            
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            Logger.Debug("CaptureBaseGameLogic initialized for " + CaptureBaseGrid.DisplayName);
        }
        
        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            _run++;
            if (CaptureProgress != 0 && _run % 6 == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("CTH", $"{CaptureBaseGrid.DisplayName} progess {CaptureProgress}");
                _run = 0;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            
            CheckPlayerDiscovery();
            CheckCapturing();
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override void MarkForClose()
        {
            base.MarkForClose();
            CaptureTheHillGameState.RemoveBaseFromPlanet(CaptureBaseGrid.Name.Split('-')[0], this);
        }

        private CaptureBaseType GetCaptureBaseType(string name)
        {
            var nameParts = name.Split('-');
            if (nameParts.Length != 4)
            {
                Logger.Error("Invalid capture base name format: " + name + ", using Type Space");
                return CaptureBaseType.Space;
            }
            CaptureBaseType type;
            Enum.TryParse(nameParts[3], true, out type);
            if (!Enum.IsDefined(typeof(CaptureBaseType), type))
            {
                Logger.Error("Invalid capture base type: " + nameParts[3] + ", using Type Space");
                return CaptureBaseType.Space;
            }
            return type;
        }
        
        private float GetCaptureRadius()
        {
            switch (CaptureBaseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseCaptureRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseCaptureRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
                default:
                    Logger.Error("Unknown capture base type: " + CaptureBaseType);
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
            }
        }

        private float GetDiscoveryRadius()
        {
            switch (CaptureBaseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseDiscoveryRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseDiscoveryRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseDiscoveryRadius;
                default:
                    Logger.Error("Unknown capture base type: " + CaptureBaseType);
                    return ModConfiguration.Instance.SpaceBaseDiscoveryRadius;
            }
        }

        // Base discovery logic
        
        private void CheckPlayerDiscovery()
        {
            var entitiesInSphere = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInSphere(ref _discoverySphere, entitiesInSphere, MyEntityQueryType.Dynamic);
            var playerIdsInSphere = entitiesInSphere.OfType<IMyCharacter>().Where(character => character.IsPlayer && !character.IsDead).Select(character => character.ControllerInfo.ControllingIdentityId).ToList();
            var playersNotKnowingThisBase = playerIdsInSphere
                .Except(CaptureTheHillGameState.GetPlayersWhoDiscoveredBase(CaptureBaseGrid.Name)).ToList();
            HandleBaseDiscovery(playersNotKnowingThisBase);
        }
        
        private void HandleBaseDiscovery(List<long> playerIdsInSphere)
        {
            if (ModConfiguration.Instance.BroadcastBaseDiscoveryToFaction)
            {
                HashSet<IMyFaction> factionsOfPlayers = new HashSet<IMyFaction>();
                foreach (var playerId in playerIdsInSphere)
                {
                    var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
                    if (playerFaction != null)
                    {
                        factionsOfPlayers.Add(playerFaction);
                    }
                    else
                    {
                        Logger.Info($"Player {playerId} has no faction, sending individual GPS");
                        CreateGps(CaptureBaseGrid.PositionComp.GetPosition(), CaptureBaseGrid.DisplayName, playerId);
                    }
                }
                BroadcastBaseDiscoveryToFaction(factionsOfPlayers);
            }
            else
            {
                Logger.Info($"Player faction broadcast disabled, sending individual GPS to {playerIdsInSphere.Count} players");
                CreateGps(CaptureBaseGrid.PositionComp.GetPosition(), CaptureBaseGrid.DisplayName, playerIdsInSphere);
            }

            Logger.Debug($"Adding {playerIdsInSphere.Count} players to base discovery for {CaptureBaseGrid.Name}");
            CaptureTheHillGameState.AddPlayersToBaseDiscovery(CaptureBaseGrid.Name, playerIdsInSphere);
        }
        
        private void BroadcastBaseDiscoveryToFaction(HashSet<IMyFaction> factions)
        {
            foreach (var faction in factions)
            {
                var playersInFaction = faction.Members.Select(member => member.Key).ToList();
                var playersWhoKnowsThisBase = CaptureTheHillGameState.GetPlayersWhoDiscoveredBase(CaptureBaseGrid.DisplayName);
                var playersWhoDontKnowThisBase = playersInFaction.Except(playersWhoKnowsThisBase).ToList();
                CreateGps(CaptureBaseGrid.PositionComp.GetPosition(), CaptureBaseGrid.DisplayName, playersWhoDontKnowThisBase);
            }
        }
        
        private void CreateGps(Vector3D position, string name, List<long> playerIds)
        {
            foreach (var playerId in playerIds)
            {
                CreateGps(position, name, playerId);
            }
        }
        
        private void CreateGps(Vector3D position, string name, long playerId)
        {
            var gpsPoint = MyAPIGateway.Session.GPS.Create(
                name,
                name,
                position,
                true
            );
            
            Logger.Debug($"Creating GPS for player {playerId} at {position} with name {name}, because they discovered {CaptureBaseGrid.DisplayName}");
            MyAPIGateway.Session.GPS.AddGps(playerId, gpsPoint);
        }
        
        // Capturing logic

        private void CheckCapturing()
        {
            var entitiesInSphere = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInSphere(ref _captureSphere, entitiesInSphere,
                MyEntityQueryType.Dynamic);
            var vehiclesInSphere = FilterForMainGrids(entitiesInSphere);
            var dominatingFaction = GetDominatingFaction(vehiclesInSphere);
            if (dominatingFaction != 0 && dominatingFaction != CurrentOwningFaction)
            {
                Logger.Debug($"{CaptureBaseGrid.DisplayName} is being captured by faction {dominatingFaction}");
                CurrentDominatingFaction = dominatingFaction;
            }
        }

        private List<MyCubeGrid> FilterForMainGrids(List<MyEntity> entities)
        {
            var gridsInSphere = entities.OfType<MyCubeGrid>().Where(grid => !grid.IsStatic).ToList();
            var vehiclesInSphere = gridsInSphere.Where(IsMainGrid).ToList();
            return vehiclesInSphere;
        }
        
        private bool IsMainGrid(MyCubeGrid grid)
        {
            List<IMyMechanicalConnectionBlock> allMechBlocks = new List<IMyMechanicalConnectionBlock>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid).GetBlocksOfType(allMechBlocks);

            foreach (var mechBlock in allMechBlocks)
            {
                if (mechBlock.TopGrid == grid)
                {
                    return false;
                }
            }

            return true;
        }
        
        private long GetDominatingFaction(List<MyCubeGrid> vehiclesInSphere)
        {
            if (vehiclesInSphere.Count == 0)
            {
                return 0;
            }
            
            var factionCount = new Dictionary<long, int>();
            foreach (var vehicle in vehiclesInSphere)
            {
                var ownerId = vehicle.BigOwners.FirstOrDefault();
                Logger.Debug($"Vehicle {vehicle.DisplayName} is owned by: {ownerId}");
                if (ownerId == 0)
                {
                    continue;
                }
                
                var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
                Logger.Debug($"Owner {ownerId} faction: {(playerFaction != null ? playerFaction.Name : "None")}");
                if (playerFaction == null)
                {
                    NotifyPlayerToJoinFaction(ownerId);
                    continue;
                }

                if (!factionCount.ContainsKey(playerFaction.FactionId))
                {
                    factionCount[playerFaction.FactionId] = 0;
                }
                factionCount[playerFaction.FactionId] += vehicle.GridSizeEnum == MyCubeSize.Large ? ModConfiguration.Instance.DominanceStrengthLargeGrid : ModConfiguration.Instance.DominanceStrengthSmallGrid;
            }

            if (factionCount.Count == 0)
            {
                return 0;
            }
            
            Logger.Debug($"Faction counts in {CaptureBaseGrid.DisplayName}: " + string.Join(", ", factionCount.Select(kv => $"{kv.Key}: {kv.Value}")));

            var dominatingFaction = factionCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            Logger.Debug($"Dominating faction in {CaptureBaseGrid.DisplayName} is {dominatingFaction} with {factionCount[dominatingFaction]} vehicles");
            return dominatingFaction;
        }

        private void NotifyPlayerToJoinFaction(long playerId)
        {
            var player = MyAPIGateway.Players.TryGetSteamId(playerId);
            var messageContent = $"You need to join a faction to capture this.";
            var encodedMessage = MyAPIGateway.Utilities.SerializeToBinary(messageContent);
            MyAPIGateway.Multiplayer.SendMessageTo(NetworkConstants.JoinFactionToCaptureMessage, encodedMessage, player);
        }
    }
}