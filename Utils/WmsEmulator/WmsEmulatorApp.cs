using System;

namespace WmsEmulator
{
	internal class WmsEmulatorApp
	{
		private static void Main(string[] args)
		{
			string ip;
			ushort port;

			try
			{
				Console.WriteLine("WMS data request emulator");
				Console.WriteLine("Enter service IP (leave blank for localhost):");
				ip = Console.ReadLine();
				if (ip == "")
					ip = "localhost";
				Console.WriteLine("Enter service connection port (leave blank for 8080):");
				var portOk = ushort.TryParse(Console.ReadLine(), out port);
				if (!portOk)
					port = 8080;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to start the service: {e}");
				return;
			}

			var httpService = new WmsEmulator(ip, port);
			while (true)
			{
			}
		}
	}
}