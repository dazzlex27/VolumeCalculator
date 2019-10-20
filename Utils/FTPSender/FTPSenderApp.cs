using System;

namespace FTPSender
{
	internal class FtpSenderApp
	{
		private static void Main()
		{
			const string defaultHost = "87.251.82.99";
			const ushort defaultPort = 10021;
			const string defaultLogin = "is";
			const string defaultPassword = "tPxFWhgnHqcNY6uP";

			string host;
			ushort port;
			string login;
			string password;

			try
			{
				Console.WriteLine("IS FTPS data sending emulator");
				Console.WriteLine($"Enter FTP-server IP (leave blank for {defaultHost}):");
				host = Console.ReadLine();
				if (host == "")
					host = defaultHost;
				Console.WriteLine($"Enter FTP-server connection port (leave blank for {defaultPort}):");
				var portOk = ushort.TryParse(Console.ReadLine(), out port);
				if (!portOk)
					port = defaultPort;
				Console.WriteLine($"Enter FTP-server login (leave blank for {defaultLogin}):");
				login = Console.ReadLine();
				if (login == "")
					login = defaultLogin;

				Console.WriteLine($"Enter FTP-server password (leave blank for {defaultPassword}):");
				password = Console.ReadLine();
				if (password == "")
					password = defaultPassword;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to start the service: {e}");
				return;
			}

			new FtpSender(host, port, login, password, true);

			Console.WriteLine("Application finished");
		}
	}
}
