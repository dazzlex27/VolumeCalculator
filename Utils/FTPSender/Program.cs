using CommonUtils.Logging;
using ExtIntegration.RequestSenders;
using Primitives;
using Primitives.Calculation;
using Primitives.Settings.Integration;
using System;
using System.Threading.Tasks;

namespace FTPSender
{
	internal class Program
	{
		private static async Task Main()
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

			var settings = new FtpRequestSettings(true, host, port, login, password, true, "istest", false, false);
			using var sender = new FtpRequestSender(new ConsoleLogger(), settings);
			try
			{
				var result = new CalculationResult(DateTime.Now, "test", 0, WeightUnits.Kg,
					0, 0, 0, 0, 0, "test", false);
				var imageData = new ImageData(40, 30, 3);
				var resultData = new CalculationResultData(result, CalculationStatus.Successful, imageData);

				await sender.ConnectAsync();
				await sender.SendAsync(resultData);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to upload! {ex}");
			}

			Console.WriteLine("Application finished");
		}
	}
}
