using System;
using System.Collections.Generic;
using System.Linq;
using CaptureTheHill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
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
        private MyCubeGrid _captureBaseGrid;
        private string _currentOwningFaction;
        private BoundingSphereD _captureSphere;
        private BoundingSphereD _discoverySphere;
        private CaptureBaseType _captureBaseType;
        
        public override void Init(MyObjectBuilder_EntityBase captureBaseGrid)
        {
            base.Init(captureBaseGrid);
            _captureBaseGrid = (MyCubeGrid)Entity;
            _captureBaseType = GetCaptureBaseType(_captureBaseGrid.Name);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _captureSphere = new BoundingSphereD(_captureBaseGrid.PositionComp.GetPosition(), GetCaptureRadius());
            _discoverySphere = new BoundingSphereD(_captureBaseGrid.PositionComp.GetPosition(), GetDiscoveryRadius());
            
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            Logger.Debug("CaptureBaseGameLogic initialized for " + _captureBaseGrid.DisplayName);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            
            CheckPlayerDiscovery();
            // CheckCapturing();
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

        private void CheckPlayerDiscovery()
        {
            var entitiesInSphere = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInSphere(ref _discoverySphere, entitiesInSphere, MyEntityQueryType.Dynamic);
            var playerIdsInSphere = entitiesInSphere.OfType<IMyCharacter>().Where(character => character.IsPlayer && !character.IsDead).Select(character => character.ControllerInfo.ControllingIdentityId).ToList();
            var playersNotKnowingThisBase = playerIdsInSphere
                .Except(CaptureTheHillGameState.GetPlayersWhoDiscoveredBase(_captureBaseGrid.Name)).ToList();
            Logger.Info($"{_captureBaseGrid.DisplayName} - Players in Sphere: {playerIdsInSphere.Count}, Players not knowing this base: {playersNotKnowingThisBase.Count}");
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
                        CreateGps(_captureBaseGrid.PositionComp.GetPosition(), _captureBaseGrid.DisplayName, playerId);
                    }
                }
                BroadcastBaseDiscoveryToFaction(factionsOfPlayers);
            }
            else
            {
                CreateGps(_captureBaseGrid.PositionComp.GetPosition(), _captureBaseGrid.DisplayName, playerIdsInSphere);
            }

            Logger.Info($"Adding {playerIdsInSphere.Count} players to base discovery for {_captureBaseGrid.DisplayName}");
            CaptureTheHillGameState.AddPlayersToBaseDiscovery(_captureBaseGrid.Name, playerIdsInSphere);
        }

        private void CheckCapturing()
        {
            var entitiesInSphere = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInSphere(ref _captureSphere, entitiesInSphere, MyEntityQueryType.Dynamic);
            if (entitiesInSphere.Count > 0)
            {
                MyAPIGateway.Utilities.ShowMessage("CTH", $"{_captureBaseGrid.DisplayName} - Entities in Sphere: {entitiesInSphere.Count}");
            }
        }
        
        private void BroadcastBaseDiscoveryToFaction(HashSet<IMyFaction> factions)
        {
            foreach (var faction in factions)
            {
                var playersInFaction = faction.Members.Select(member => member.Key).ToList();
                var playersWhoKnowsThisBase = CaptureTheHillGameState.GetPlayersWhoDiscoveredBase(_captureBaseGrid.DisplayName);
                var playersWhoDontKnowThisBase = playersInFaction.Except(playersWhoKnowsThisBase).ToList();
                CreateGps(_captureBaseGrid.PositionComp.GetPosition(), _captureBaseGrid.DisplayName, playersWhoDontKnowThisBase);
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
            
            Logger.Debug($"Creating GPS for player {playerId} at {position} with name {name}, because they discovered {_captureBaseGrid.DisplayName}");
            MyAPIGateway.Session.GPS.AddGps(playerId, gpsPoint);
        }

        private float GetCaptureRadius()
        {
            switch (_captureBaseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseCaptureRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseCaptureRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
                default:
                    Logger.Error("Unknown capture base type: " + _captureBaseType);
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
            }
        }

        private float GetDiscoveryRadius()
        {
            switch (_captureBaseType)
            {
               case CaptureBaseType.Ground:
                   return ModConfiguration.Instance.GroundBaseDiscoveryRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseDiscoveryRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseDiscoveryRadius;
                default:
                    Logger.Error("Unknown capture base type: " + _captureBaseType);
                    return ModConfiguration.Instance.SpaceBaseDiscoveryRadius;
            }
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