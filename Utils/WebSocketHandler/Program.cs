using ExtIntegration.RequestHandlers;
using Primitives.Logging;
using Primitives.Settings.Integration;
using System;

namespace WebSocketHandler
{
	internal class Program
	{
		private static void Main()
		{
			const string defaultIp = "0.0.0.0";
			const int defaultPort = 8081;

			string ip;
			ushort port;

			var logger = new ConsoleLogger();

			try
			{
				logger.LogInfo("IS web interface backend emulator");
				logger.LogInfo("Enter IP to listen (leave blank to listen to all addresses):");
				ip = Console.ReadLine();
				if (ip == "")
					ip = defaultIp;
				logger.LogInfo($"Enter connection port (leave blank for {defaultPort}):");
				var portOk = ushort.TryParse(Console.ReadLine(), out port);
				if (!portOk)
					port = defaultPort;
			}
			catch (Exception e)
			{
				logger.LogInfo($"Failed to start the service: {e}");
				return;
			}

			var settings = new WebClientHandlerSettings(true, ip, port);
			using var service = new WebClientHandler(logger, settings);

			while (Console.ReadLine() != "exit")
				logger.LogInfo("Enter \"exit\" to stop the program");

			logger.LogInfo("Application finished");
		}
	}
}
