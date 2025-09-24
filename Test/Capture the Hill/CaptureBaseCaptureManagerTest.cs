using System;
using System.Collections.Generic;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.logging;
using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.state;
using CaptureTheHill.logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

[TestClass]
public sealed class CaptureBaseCaptureManagerTest
{
    [TestMethod]
    public void TestCapturingOfUnownedBase()
    {
        ModConfiguration.Instance = new ModConfiguration();
        var loggerMock = new Mock<ICthLogger>();
        CthLogger.CthLoggerInstance = loggerMock.Object;

        var factionsMock = new Mock<IMyFactionCollection>();
        var faction2 = new Mock<IMyFaction>();
        faction2.Setup(f2 => f2.Name).Returns("TestFaction2");
        factionsMock.Setup(f => f.TryGetFactionById(2)).Returns(faction2.Object);

        var utilitiesMock = new Mock<IMyUtilities>();
        utilitiesMock.Setup(u => u.SerializeToBinary(It.IsAny<object>())).Returns(new byte[0]);
        MyAPIGateway.Utilities = utilitiesMock.Object;

        var playersMock = new Mock<IMyPlayerCollection>();
        var playerList = new List<IMyPlayer>();
        playersMock.Setup(p => p.GetPlayers(It.IsAny<List<IMyPlayer>>(), It.IsAny<Func<IMyPlayer, bool>>()))
            .Callback<List<IMyPlayer>, Func<IMyPlayer, bool>>((list, predicate) =>
            {
                foreach (var player in playerList)
                {
                    if (predicate(player))
                    {
                        list.Add(player);
                    }
                }
            });
        MyAPIGateway.Players = playersMock.Object;

        var multiplayerMock = new Mock<IMyMultiplayer>();
        multiplayerMock.Setup(m =>
            m.SendMessageTo(It.IsAny<ushort>(), It.IsAny<byte[]>(), It.IsAny<ulong>(), It.IsAny<bool>()));
        MyAPIGateway.Multiplayer = multiplayerMock.Object;

        var sessionMock = new Mock<IMySession>();
        sessionMock.Setup(s => s.Factions).Returns(factionsMock.Object);
        MyAPIGateway.Session = sessionMock.Object;

        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Atmosphere,
            CurrentOwningFaction = 0,
            CurrentDominatingFaction = 2,
            PreviousDominatingFaction = 0,
            CaptureProgress = 0,
            FightMode = CaptureBaseFightMode.Attacking,
            LastNotifiedFaction = 0
        };
        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };
        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);
        Assert.AreEqual(1, base1.CaptureProgress); // Capture progress increased by 1
        Assert.AreEqual(0, base1.CurrentOwningFaction); // Still unowned
        Assert.AreEqual(2, base1.PreviousDominatingFaction); // Should be set to current dominating faction
        Assert.AreEqual(2, base1.CurrentDominatingFaction); // Still being captured by faction 2
        Assert.AreEqual(2, base1.LastNotifiedFaction); // Notification sent to faction 2
    }
}