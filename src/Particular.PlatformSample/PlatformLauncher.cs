﻿namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// stub
    /// </summary>
    public static class PlatformLauncher
    {
        const int PortStartSearch = 33533;

        /// <summary>
        /// Launches Particular Service Platform tools (ServiceControl, ServiceControl Monitoring, and ServicePulse) in a single process
        /// using the Learning Transport in order to demonstrate platform usage in a sample or test project. Not to be used outside of a
        /// test/demo context. In real life, each tool should be installed as a Windows service, and used with a production-ready
        /// message transport.
        /// </summary>
        /// <param name="showPlatformToolConsoleOutput">By default the output of each application is suppressed. Set to true to show tool output in the console window.</param>
        public static void Launch(bool showPlatformToolConsoleOutput = false) => Launch(Console.Out, Console.In, showPlatformToolConsoleOutput);

        /// <summary>
        /// Launches Particular Service Platform tools (ServiceControl, ServiceControl Monitoring, and ServicePulse) in a single process
        /// using the Learning Transport in order to demonstrate platform usage in a sample or test project. Not to be used outside of a
        /// test/demo context. In real life, each tool should be installed as a Windows service, and used with a production-ready
        /// message transport.
        /// </summary>
        /// <param name="output">Select an output stream other than Console.Out.</param>
        /// <param name="input">Select an input stream other than Console.In.</param>
        /// <param name="showPlatformToolConsoleOutput">By default the output of each application is suppressed. Set to true to show tool output in the console window.</param>
        public static void Launch(TextWriter output, TextReader input, bool showPlatformToolConsoleOutput = false)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                output.WriteLine("The Particular Service Platform can currently only be run on the Windows platform.");
                output.WriteLine("Press Enter to exit...");
                input.ReadLine();
                return;
            }

            var wait = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                wait.Set();
            };

            LaunchInternal(output, input, showPlatformToolConsoleOutput, () =>
            {
                output.WriteLine("Press Ctrl+C stop Particular Service Platform tools.");
                wait.WaitOne();

                output.WriteLine();
                output.WriteLine("Waiting for external processes to shut down...");
            });

        }

        static void LaunchInternal(TextWriter output, TextReader input, bool showPlatformToolConsoleOutput, Action interactive)
        {
            var ports = Network.FindAvailablePorts(PortStartSearch, 4);

            var controlPort = ports[0];
            var maintenancePort = ports[1];
            var monitoringPort = ports[2];
            var pulsePort = ports[3];

            output.WriteLine($"Found free port '{controlPort}' for ServiceControl");
            output.WriteLine($"Found free port '{maintenancePort}' for ServiceControl Maintenance");
            output.WriteLine($"Found free port '{monitoringPort}' for ServiceControl Monitoring");
            output.WriteLine($"Found free port '{pulsePort}' for ServicePulse");

            var finder = new Finder();

            output.WriteLine("Solution Folder: " + finder.SolutionRoot);

            output.WriteLine("Creating log folders");
            var monitoringLogs = finder.GetDirectory(@".\.logs\monitoring");
            var controlLogs = finder.GetDirectory(@".\.logs\servicecontrol");
            var controlDB = finder.GetDirectory(@".\.db");

            output.WriteLine("Creating transport folder");
            var transportPath = finder.GetDirectory(@".\.learningtransport");

            using (var launcher = new AppLauncher(showPlatformToolConsoleOutput))
            {
                output.WriteLine("Launching ServiceControl");
                launcher.ServiceControl(controlPort, maintenancePort, controlLogs, controlDB, transportPath);

                output.WriteLine("Launching ServiceControl Monitoring");
                // Monitoring appends `.learningtransport` to the transport path on its own
                launcher.Monitoring(monitoringPort, monitoringLogs, finder.SolutionRoot);

                output.WriteLine("Launching ServicePulse");
                launcher.ServicePulse(pulsePort, controlPort, monitoringPort);

                output.WriteLine("Waiting for ServiceControl to be available...");
                Network.WaitForHttpOk($"http://localhost:{controlPort}/api", httpVerb: "GET");

                var servicePulseUrl = $"http://localhost:{pulsePort}";
                output.WriteLine();
                output.WriteLine($"ServicePulse can now be accessed via: {servicePulseUrl}");
                output.WriteLine("Attempting to launch ServicePulse in a browser window...");
                Process.Start(new ProcessStartInfo(servicePulseUrl) {UseShellExecute = true});

                output.WriteLine();
                interactive();
            }
        }
    }
}