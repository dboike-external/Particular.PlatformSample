﻿namespace Particular.PlatformSample.Tests
{
    using System.Linq;
    using System.Net.NetworkInformation;
    using NUnit.Framework;

    [Explicit]
    [TestFixture]
    public class NetworkTests
    {

        [Test]
        public void CheckPort()
        {
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProps.GetActiveTcpListeners();
            var knownTakenPort = listeners.First().Port;

            var random = Network.IsPortAvailable(98457);
            var knownTaken = Network.IsPortAvailable(knownTakenPort);

            Assert.IsTrue(random);
            Assert.IsFalse(knownTaken);
        }

        [Test]
        public void FindAvailablePorts()
        {
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProps.GetActiveTcpListeners();
            var knownTakenPort = listeners.First().Port;

            var results = Network.FindAvailablePorts(knownTakenPort, 5);
            Assert.AreEqual(5, results.Length);
            Assert.AreEqual(5, results.Distinct().Count());
            Assert.IsTrue(results.All(p => p != knownTakenPort));
        }
    }

}