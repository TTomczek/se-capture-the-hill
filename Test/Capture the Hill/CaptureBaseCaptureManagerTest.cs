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
using VRage.Game;
using VRage.Game.ModAPI;

[TestClass]
public sealed class CaptureBaseCaptureManagerTest
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        ModConfiguration.Instance = new ModConfiguration(groundBaseCaptureTimeInSeconds: 3,
            atmosphereBaseCaptureTimeInSeconds: 3, spaceBaseCaptureTimeInSeconds: 3);
        var loggerMock = new Mock<ICthLogger>();
        CthLogger.CthLoggerInstance = loggerMock.Object;

        var factionsMock = new Mock<IMyFactionCollection>();
        var faction1 = new Mock<IMyFaction>();
        faction1.Setup(f1 => f1.Name).Returns("TestFaction2");
        faction1.Setup(f1 => f1.Members).Returns(new Dictionary<long, MyFactionMember>
            { { 1, new MyFactionMember() }, { 2, new MyFactionMember() } });
        factionsMock.Setup(f => f.TryGetFactionById(2)).Returns(faction1.Object);

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
    }

    [TestMethod]
    public void TestCaptureProgressOfUnownedBase()
    {
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

    [TestMethod]
    public void TestCapturingOfUnownedBase()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Atmosphere,
            CurrentOwningFaction = 0,
            CurrentDominatingFaction = 2,
            PreviousDominatingFaction = 2,
            CaptureProgress = 2,
            FightMode = CaptureBaseFightMode.Attacking,
            LastNotifiedFaction = 2
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);
        Assert.AreEqual(3, base1.CaptureProgress); // Capture progress increased by
        Assert.AreEqual(2, base1.CurrentOwningFaction); // Now owned by faction 2
        Assert.AreEqual(2, base1.PreviousDominatingFaction); // Should remain the same
        Assert.AreEqual(2, base1.CurrentDominatingFaction); // Still being captured
        Assert.AreEqual(CaptureBaseFightMode.Defending, base1.FightMode); // Now in defending mode
    }

    [TestMethod]
    public void TestCaptureProgressOfOwnedBase()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Atmosphere,
            CurrentOwningFaction = 2,
            CurrentDominatingFaction = 1,
            PreviousDominatingFaction = 2,
            CaptureProgress = 3,
            FightMode = CaptureBaseFightMode.Defending,
            LastNotifiedFaction = 2
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);
        Assert.AreEqual(2, base1.CaptureProgress); // Capture progress decreased by 1
        Assert.AreEqual(2, base1.CurrentOwningFaction); // Still owned by faction 2
        Assert.AreEqual(1, base1.PreviousDominatingFaction); // Set to current dominating faction
        Assert.AreEqual(1, base1.CurrentDominatingFaction); // Still being attacked by faction 1
    }

    [TestMethod]
    public void TestCaptureProgressOfOwnedBaseWithNoAttackers()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Atmosphere,
            CurrentOwningFaction = 2,
            CurrentDominatingFaction = 0,
            PreviousDominatingFaction = 2,
            CaptureProgress = 2,
            FightMode = CaptureBaseFightMode.Defending,
            LastNotifiedFaction = 2
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);

        Assert.AreEqual(2, base1.CaptureProgress); // Capture progress remains the same
        Assert.AreEqual(2, base1.CurrentOwningFaction); // Still owned by faction
        Assert.AreEqual(0, base1.PreviousDominatingFaction); // Previous dominating faction reset
        Assert.AreEqual(0, base1.CurrentDominatingFaction); // Still no attackers
    }

    [TestMethod]
    public void TestLossOfOwnedBase()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Atmosphere,
            CurrentOwningFaction = 2,
            CurrentDominatingFaction = 1,
            PreviousDominatingFaction = 1,
            CaptureProgress = 1,
            FightMode = CaptureBaseFightMode.Defending,
            LastNotifiedFaction = 2
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);

        Assert.AreEqual(0, base1.CaptureProgress); // Capture progress decreased to 0
        Assert.AreEqual(0, base1.CurrentOwningFaction); // Now unowned
        Assert.AreEqual(1, base1.PreviousDominatingFaction); // Still set to current dominating faction
        Assert.AreEqual(1, base1.CurrentDominatingFaction); // Still being attacked by faction 1
        Assert.AreEqual(CaptureBaseFightMode.Attacking, base1.FightMode); // Now in attacking mode
    }

    [TestMethod]
    public void TestNoChangeWhenNoOnePresent()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Ground,
            CurrentOwningFaction = 1,
            CurrentDominatingFaction = 0,
            PreviousDominatingFaction = 2,
            CaptureProgress = 1,
            FightMode = CaptureBaseFightMode.Attacking,
            LastNotifiedFaction = 2
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);

        Assert.AreEqual(1, base1.CaptureProgress); // Capture progress remains the same
        Assert.AreEqual(1, base1.CurrentOwningFaction); // Not changed
        Assert.AreEqual(0, base1.PreviousDominatingFaction); // No previous dominating faction
        Assert.AreEqual(0, base1.CurrentDominatingFaction); // Still no attackers
    }

    [TestMethod]
    public void TestResetCaptureProgressWhenAttackersChange()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Atmosphere,
            CurrentOwningFaction = 0,
            CurrentDominatingFaction = 2,
            PreviousDominatingFaction = 1,
            CaptureProgress = 2,
            FightMode = CaptureBaseFightMode.Attacking,
            LastNotifiedFaction = 1
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);

        Assert.AreEqual(0, base1.CaptureProgress); // Capture progress reset to 0
        Assert.AreEqual(0, base1.CurrentOwningFaction); // Still unowned
        Assert.AreEqual(2, base1.PreviousDominatingFaction); // Updated to current dominating faction
        Assert.AreEqual(2, base1.CurrentDominatingFaction); // Still being captured by faction 2
    }

    [TestMethod]
    public void TestShouldHaveZeroCaptureProgressWhenChanging()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Space,
            CurrentOwningFaction = 2,
            CurrentDominatingFaction = 1,
            PreviousDominatingFaction = 1,
            CaptureProgress = 1,
            FightMode = CaptureBaseFightMode.Defending,
            LastNotifiedFaction = 2
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);

        Assert.AreEqual(0, base1.CaptureProgress); // Capture progress decreased to 0
        Assert.AreEqual(0, base1.CurrentOwningFaction); // Now unowned
        Assert.AreEqual(1, base1.PreviousDominatingFaction); // Still set to current dominating faction
        Assert.AreEqual(1, base1.CurrentDominatingFaction); // Still being attacked by faction 1
        Assert.AreEqual(CaptureBaseFightMode.Attacking, base1.FightMode); // Now in attacking mode

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);

        Assert.AreEqual(1, base1.CaptureProgress); // Capture progress increased by 1
        Assert.AreEqual(0, base1.CurrentOwningFaction); // Still unowned
        Assert.AreEqual(1, base1.PreviousDominatingFaction); // Should be set to current dominating faction
        Assert.AreEqual(1, base1.CurrentDominatingFaction); // Still being captured by faction 1
        Assert.AreEqual(1, base1.LastNotifiedFaction); // Notification sent to faction 1
    }

    [TestMethod]
    public void TestNullCaptureBaseData()
    {
        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { null }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);
        // No exception should be thrown
        Assert.IsTrue(true); // If we reach here, the test passes
    }

    [TestMethod]
    public void TestNoCaptureProgressWhenAlreadyFullyCaptured()
    {
        var base1 = new CaptureBaseData
        {
            BaseName = "Base1",
            BaseDisplayName = "Alpha Base",
            PlanetName = "PlanetA",
            CaptureBaseType = CaptureBaseType.Atmosphere,
            CurrentOwningFaction = 2,
            CurrentDominatingFaction = 2,
            PreviousDominatingFaction = 2,
            CaptureProgress = 3,
            FightMode = CaptureBaseFightMode.Defending,
            LastNotifiedFaction = 2
        };

        var basesPerPlanet = new Dictionary<string, List<CaptureBaseData>>
        {
            {
                "PlanetA", new List<CaptureBaseData> { base1 }
            }
        };

        CaptureBaseCaptureManager.UpdateBaseCaptureProgress(basesPerPlanet);

        Assert.AreEqual(3, base1.CaptureProgress); // Capture progress remains the same
        Assert.AreEqual(2, base1.CurrentOwningFaction); // Still owned by faction
        Assert.AreEqual(2, base1.PreviousDominatingFaction); // Should remain the same
        Assert.AreEqual(2, base1.CurrentDominatingFaction); // Still being captured by faction 2
        Assert.AreEqual(CaptureBaseFightMode.Defending, base1.FightMode); // Still in defending mode
    }
}