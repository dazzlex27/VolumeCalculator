using System;

namespace WebSocketHandler
{
	internal class WebSocketHandlerApp
	{
		private static void Main()
		{
			const string defaultIp = "0.0.0.0";
			const int defaultPort = 8081;

			string ip;
			ushort port;

			try
			{
				Console.WriteLine("IS web interface backend emulator");
				Console.WriteLine("Enter IP to listen (leave blank to listen to all addresses):");
				ip = Console.ReadLine();
				if (ip == "")
					ip = defaultIp;
				Console.WriteLine($"Enter connection port (leave blank for {defaultPort}):");
				var portOk = ushort.TryParse(Console.ReadLine(), out port);
				if (!portOk)
					port = defaultPort;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to start the service: {e}");
				return;
			}

			using (var service = new WebSocketHandler(ip, port))
			{
				while (Console.ReadLine() != "exit")
				{
					Console.WriteLine("Enter \"exit\" to stop the program");
				}

				Console.WriteLine("Application finished");
			}
		}
	}
}