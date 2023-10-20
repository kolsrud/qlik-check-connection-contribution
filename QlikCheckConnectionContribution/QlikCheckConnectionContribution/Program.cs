using System;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using Qlik.Sense.RestClient;
using Qlik.Sense.RestClient.Qrs;

namespace QlikCheckConnectionContribution
{
    internal class Program
    {
        private enum Mode
        {
	        TestRule,
            TestOptionalConnection
        }

        private class Config
        {
	        public string User = null;
	        public Guid AppId = Guid.Empty;
	        public Mode Mode { get; set; }
		}

		static void Main(string[] args)
        {
	        var config = GetConfiguration(args);

            WriteLine($"Running test as user: {config.User}");
            WriteLine($"Connection to app:    {config.AppId}");

            var certs = RestClient.LoadCertificateFromStore();
            var factory = new ClientFactory("https://localhost", certs, false);
            var client = factory.GetClient(new User(config.User));

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

            switch (config.Mode)
            {
	            case Mode.TestRule:
		            TestRule(config, factory, client);
                    break;
	            case Mode.TestOptionalConnection:
                    TestOptionalConnection(config, factory, client);
		            break;
	            default:
		            throw new ArgumentOutOfRangeException();
            }

        }

		private static void TestRule(Config config, ClientFactory factory, IRestClient client)
		{
			WriteLine("**** Test 1 ***");
			ClearRuleCache(factory);
			var tOpen1 = Measure(client, $"/qrs/app/{config.AppId}/open/full");
			WriteLine();

			WriteLine("**** Test 2 ***");
			ClearRuleCache(factory);
			var tConnections = Measure(client, $"/qrs/dataconnection/count");
			var tOpen2 = Measure(client, $"/qrs/app/{config.AppId}/open/full");
			WriteLine();

			WriteLine("**** Test Result Summary ***");
			WriteLine($"Time to open app test 1:   {tOpen1}");
			WriteLine($"Time to open app test 2:   {tOpen2} (diff from test1: {tOpen2 - tOpen1})");
			WriteLine($"Time to count connections: {tConnections}");
			WriteLine($"Estimated contribution from connection rules: {ComputePercentage(tOpen1, tOpen2):f1}%");
		}

		private static void TestOptionalConnection(Config config, ClientFactory factory, IRestClient client)
		{
			var endpoint = $"/qrs/app/{config.AppId}/open/full?includeDataConnections=true";
			var testCnt = 6;
			WriteLine($" *** Testing {testCnt} times: {endpoint}");
			var tTotal0 = new TimeSpan();
			foreach (var i in Enumerable.Range(0, testCnt))
			{
				var t = Measure(client, endpoint);
				if (i > 0)
				{
					tTotal0 += t;
				}
			}

			endpoint = $"/qrs/app/{config.AppId}/open/full?includeDataConnections=false";
			WriteLine($" *** Testing {testCnt} times: {endpoint}");
			var tTotal1 = new TimeSpan();
			foreach (var i in Enumerable.Range(0, testCnt))
			{
				var t = Measure(client, endpoint);
				if (i > 0)
				{
					tTotal1 += t;
				}
			}

            var avg0 = TimeSpan.FromTicks(tTotal0.Ticks/(testCnt-1));
            var avg1 = TimeSpan.FromTicks(tTotal1.Ticks / (testCnt - 1));
            WriteLine($"Time with data connections:    {tTotal0} (avg: {avg0})");
            WriteLine($"Time without data connections: {tTotal1} (avg: {avg1})");
            WriteLine($"Time diff:                     {tTotal0 - tTotal1}");
            WriteLine($"Avg diff:                      {avg0 - avg1}");
		}

		private static Config GetConfiguration(string[] args)
        {
            var config = new Config();
			try
			{
				config.User = args[0];
				config.AppId = new Guid(args[1]);
                config.Mode = (Mode) Enum.Parse(typeof(Mode), args[2]);
			}
			catch
			{
				WriteLine("Error parsing arguments.");
				PrintUsage();
				Environment.Exit(1);
			}

            return config;
		}

		private static void PrintUsage()
        {
            WriteLine("Usage:   .\\QlikCheckConnectionContributions {USERDIR}\\{userid} {appId} <mode>");
            WriteLine("Mode ::= TestRule | TestOptionalConnection");
            WriteLine("Example: .\\QlikCheckConnectionContributions INTERNAL\\sa_api 4a03b166-af2c-4784-b17b-1b22dcf5ed4c TestOptionalConnection");
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
