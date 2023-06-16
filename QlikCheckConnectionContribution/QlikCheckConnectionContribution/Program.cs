using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Qlik.Sense.RestClient;
using Qlik.Sense.RestClient.Qrs;

namespace QlikCheckConnectionContribution
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string user = null;
            Guid appId = Guid.Empty;

            try
            {
                user = args[0];
                appId = new Guid(args[1]);
            }
            catch
            {
                WriteLine("Error parsing arguments.");
                PrintUsage();
                Environment.Exit(1);
            }

            WriteLine($"Running test as user: {user}");
            WriteLine($"Connection to app:    {appId}");

            var certs = RestClient.LoadCertificateFromStore();
            var factory = new ClientFactory("https://localhost", certs, false);
            var client = factory.GetClient(new User(user));

            Write("Connecting to server... ");
            try
            {
                var rsp = factory.AdminClient.Get<JObject>("/qrs/about");
                WriteLine("Success! Connected to repository version: " + rsp["buildVersion"]);
            }
            catch (Exception ex)
            {
                WriteLine("Failed!");
                WriteLine("Exception: " + ex);
                Environment.Exit(1);
            }
            WriteLine();

            WriteLine("**** Test 1 ***");
            ClearRuleCache(factory);
            var tOpen1 = Measure(client, $"/qrs/app/{appId}/open/full");
            WriteLine();

            WriteLine("**** Test 2 ***");
            ClearRuleCache(factory);
            var tConnections = Measure(client, $"/qrs/dataconnection/count");
            var tOpen2 = Measure(client, $"/qrs/app/{appId}/open/full");
            WriteLine();

            WriteLine("**** Test Result Summary ***");
            WriteLine($"Time to open app test 1:   {tOpen1}");
            WriteLine($"Time to open app test 2:   {tOpen2} (diff from test1: {tOpen2 - tOpen1})");
            WriteLine($"Time to count connections: {tConnections}");
            WriteLine($"Estimated contribution from connection rules: {ComputePercentage(tOpen1, tOpen2):f1}%");
        }

        private static void PrintUsage()
        {
            WriteLine("Usage:   .\\QlikCheckConnectionContributions {USERDIR}\\{userid} {appId}");
            WriteLine("Example: .\\QlikCheckConnectionContributions INTERNAL\\sa_api 4a03b166-af2c-4784-b17b-1b22dcf5ed4c");
        }

        private static double ComputePercentage(TimeSpan tOpen1, TimeSpan tOpen2)
        {
            var t0 = (double) tOpen1.Ticks;
            var t1 = (double) tOpen2.Ticks;
            return (1 - t1/t0) * 100.0;
        }

        private static TimeSpan Measure(IRestClient client, string endpoint)
        {
            var sw = new Stopwatch();
            Console.Write($"Calling endpoint {endpoint}... ");
            sw.Start();
            client.Get(endpoint);
            sw.Stop();
            Console.WriteLine($"Done! ({sw.Elapsed})");
            return sw.Elapsed;
        }

        private static void ClearRuleCache(ClientFactory factory)
        {
            Write("Clearing rule cache... ");
            factory.ClearRuleCache();
            WriteLine("Done!");
        }

        private static void WriteLine(string msg = "")
        {
            Write(msg + Environment.NewLine);
        }

        private static void Write(string msg)
        {
            Console.Write(msg);
        }
    }
}
