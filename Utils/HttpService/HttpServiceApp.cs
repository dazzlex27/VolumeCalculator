using System;

namespace HttpService
{
	internal class HttpServiceApp
	{
		private static void Main()
		{
			string ip;
			ushort port;

			try
			{
				Console.WriteLine("IS data sending emulator");
				Console.WriteLine("Enter WMS IP (leave blank for localhost):");
				ip = Console.ReadLine();
				if (ip == "")
					ip = "localhost";
				Console.WriteLine("Enter WMS connection port (leave blank for 8080):");
				var portOk = ushort.TryParse(Console.ReadLine(), out port);
				if (!portOk)
					port = 8080;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to start the service: {e}");
				return;
			}

			var httpService = new HttpService(ip, port);
			while (Console.ReadLine() != "exit")
			{
			}

			Console.WriteLine("Application finished");
		}
	}
}
