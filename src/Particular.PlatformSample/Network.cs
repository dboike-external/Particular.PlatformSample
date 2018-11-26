﻿namespace Particular
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;

    static class Network
    {
        public static bool IsPortAvailable(int port)
        {
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProps.GetActiveTcpListeners();

            foreach (var ipEndpoint in listeners)
            {
                if (ipEndpoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        public static int[] FindAvailablePorts(int startingPort, int count)
        {
            var results = new int[count];
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();

            var portsInUse = ipGlobalProps.GetActiveTcpListeners()
                .Select(ipEndpoint => ipEndpoint.Port);

            var hashSet = new HashSet<int>(portsInUse);

            for (var i = 0; i < count; i++)
            {
                while (hashSet.Contains(startingPort))
                {
                    startingPort++;
                }

                results[i] = startingPort;
                startingPort++;
            }

            return results;
        }

        public static void WaitForHttpOk(string url, int timeoutMilliseconds = 1000)
        {
            HttpStatusCode status;

            do
            {
                Thread.Sleep(timeoutMilliseconds);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                try
                {
                    var response = (HttpWebResponse)request.GetResponse();
                    status = response.StatusCode;
                }
                catch (WebException wx)
                {
                    status = ((HttpWebResponse)wx.Response).StatusCode;
                }
            }
            while (status != HttpStatusCode.OK);
        }
    }
}
