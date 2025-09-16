using System;
using CaptureTheHill.logging;
using VRage.Game.Components;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.session.server
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class CthServerTimedActionsSession : MySessionComponentBase
    {
        private uint _ticks;

        public override void UpdateAfterSimulation()
        {
            if (!CthServerSession.Instance.IsServer)
            {
                return;
            }

            if (_ticks == 0)
            {
                _ticks++;
                return;
            }

            RunEverySecond(_ticks);
            RunEveryMinute(_ticks);
            RunEvery30Minutes(_ticks);

            // Reset ticks every 60 minutes (216000 ticks)
            if (_ticks == 216000)
            {
                _ticks = 0;
                Logger.Debug("Resetting tick counter to 0");
            }
            else
            {
                _ticks++;
            }
        }

        private void RunEverySecond(uint ticks)
        {
            if (ticks % 60 != 0)
            {
                return;
            }

            Logger.Debug($"Running 1 second checks at tick {ticks}");

            try
            {
                CaptureBaseCaptureManager.UpdateBaseCaptureProgress();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving game state: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }

        private void RunEveryMinute(uint ticks)
        {
            if (ticks % 3600 != 0)
            {
                return;
            }

            Logger.Debug($"Running 1 minute checks at tick {ticks}");

            try
            {
                CaptureBaseCaptureManager.UpdatePoints();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating capture bases: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }

        private void RunEvery30Minutes(uint ticks)
        {
            if (ticks % 108000 != 0)
            {
                return;
            }

            Logger.Debug($"Running 30 minute checks at tick {ticks}");

            try
            {
                CaptureBaseCaptureManager.PrintLeaderboard();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking for win condition: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }
    }
}