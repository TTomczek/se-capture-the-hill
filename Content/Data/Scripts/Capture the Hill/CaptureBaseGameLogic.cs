using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.state;
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

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid), false, "CTH_Capture_Base")]
    public class CaptureBaseGameLogic : MyGameLogicComponent
    {
        private BoundingSphereD _captureSphere;
        private BoundingSphereD _discoverySphere;
        private int _run = 0;
        private CaptureBaseData _captureBaseData;
        
        public MyCubeGrid CaptureBaseGrid { get; private set; }

        public override void Init(MyObjectBuilder_EntityBase captureBaseGrid)
        {
            base.Init(captureBaseGrid);
            CaptureBaseGrid = (MyCubeGrid)Entity;
            
            GameStateAccessor.GetBaseDataByBaseName(CaptureBaseGrid.Name, ref _captureBaseData);
            if (_captureBaseData == null)
            {
                Logger.Info($"CaptureBaseData is null for {CaptureBaseGrid.Name} in Init, will retry in next frame.");
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                return;
            }
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }
        
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            
            _captureSphere = new BoundingSphereD(CaptureBaseGrid.PositionComp.GetPosition(), GetCaptureRadius());
            _discoverySphere = new BoundingSphereD(CaptureBaseGrid.PositionComp.GetPosition(), GetDiscoveryRadius());
            
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            Logger.Debug("CaptureBaseGameLogic initialized for " + CaptureBaseGrid.DisplayName);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            
            GameStateAccessor.GetBaseDataByBaseName(CaptureBaseGrid.Name, ref _captureBaseData);
            
            if (_captureBaseData == null)
            {
                return;
            }
            Logger.Info($"Successfully fetched CaptureBaseData for {CaptureBaseGrid.Name} in UpdateBeforeSimulation. Setting NeedsUpdate.");
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            _run++;
            if (_captureBaseData.CaptureProgress != 0 && _run % 6 == 0)
            {
                if (_captureBaseData.CurrentOwningFaction == 0)
                {
                    CaptureBaseGrid.DisplayName = $"{_captureBaseData.BaseDisplayName} - Capturing: {_captureBaseData.CaptureProgress}";
                }
                else
                {
                    CaptureBaseGrid.DisplayName = $"[{FactionUtils.GetFactionTagById(_captureBaseData.CurrentOwningFaction)}] - {_captureBaseData.BaseDisplayName} - Capturing: {_captureBaseData.CaptureProgress}";
                }
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
        
        private float GetCaptureRadius()
        {
            switch (_captureBaseData.CaptureBaseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseCaptureRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseCaptureRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
                default:
                    Logger.Error($"Unknown capture base type: {_captureBaseData.CaptureBaseType}");
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
            }
        }

        private float GetDiscoveryRadius()
        {
            switch (_captureBaseData.CaptureBaseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseDiscoveryRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseDiscoveryRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseDiscoveryRadius;
                default:
                    Logger.Error($"Unknown capture base type: {_captureBaseData.CaptureBaseType}");
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
                .Except(GameStateAccessor.GetPlayersWhoDiscoveredBase(CaptureBaseGrid.Name)).ToList();
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
            GameStateAccessor.AddPlayersToBaseDiscovery(CaptureBaseGrid.Name, playerIdsInSphere);
        }
        
        private void BroadcastBaseDiscoveryToFaction(HashSet<IMyFaction> factions)
        {
            foreach (var faction in factions)
            {
                var playersInFaction = faction.Members.Select(member => member.Key).ToList();
                var playersWhoKnowsThisBase = GameStateAccessor.GetPlayersWhoDiscoveredBase(CaptureBaseGrid.DisplayName);
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
            if (dominatingFaction != 0 && dominatingFaction != _captureBaseData.CurrentOwningFaction)
            {
                Logger.Debug($"{CaptureBaseGrid.DisplayName} is being captured by faction {dominatingFaction}");
                _captureBaseData.CurrentDominatingFaction = dominatingFaction;
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

            if (factionCount.Count == 0)
            {
                Logger.Debug($"No factions present in {CaptureBaseGrid.DisplayName}");
                return 0;
            }
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