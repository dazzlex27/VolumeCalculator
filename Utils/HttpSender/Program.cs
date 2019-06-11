using System;
using System.IO;
using System.Net;

namespace HttpSender
{
	internal class Program
	{
		private static readonly Random Rd = new Random();

		private static void Main()
		{
			const string defaultIp = "192.168.200.116";
			const ushort defaultPort = 80;
			const string defaultUrl = "WMSA/hs/IS_Measurement/put";
			const string defaultLogin = "WorkHTTP";
			const string defaultPassword = "204fnehy";

			string ip;
			ushort port;
			string url;
			string login;
			string password;

			try
			{
				Console.WriteLine("IS HTTP data sender emulator");
				Console.WriteLine($"Enter Service IP (leave blank for {defaultIp}):");
				ip = Console.ReadLine();
				if (ip == "")
					ip = defaultIp;

				Console.WriteLine($"Enter Service connection port (leave blank for {defaultPort}:");
				var portOk = ushort.TryParse(Console.ReadLine(), out port);
				if (!portOk)
					port = defaultPort;

				Console.WriteLine($"Enter Service url (leave blank for {defaultUrl}:");
				url = Console.ReadLine();
				if (url == "")
					url = defaultUrl;

				Console.WriteLine($"Enter Service login (leave blank for {defaultLogin}:");
				login = Console.ReadLine();
				if (login == "")
					login = defaultLogin;

				Console.WriteLine($"Enter Service url (leave blank for {defaultPassword}:");
				password = Console.ReadLine();
				if (password == "")
					password = defaultPassword;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to start the sender: {e}");
				return;
			}

			var address = $"http://{ip}:{port}/{url}";

			while (true)
			{
				try
				{
					Console.WriteLine($"Press any key to send request to {address}");
					var input = Console.ReadLine();
					if (input == "exit")
						break;

					//var response = httpClient.PostAsync(address, new StringContent("")).Result;
					//var responceString = response.Content.ReadAsStringAsync().Result;
					//Console.WriteLine($"Request sent, response received: {responceString}");
					//Console.WriteLine($"Request sent, response received: {response}");

					//var webClient = new WebClient();
					//webClient.QueryString.Add("bar", "0");
					//webClient.QueryString.Add("wt", "0");
					//webClient.QueryString.Add("l", "0");
					//webClient.QueryString.Add("w", "0");
					//webClient.QueryString.Add("h", "0");
					//var response = webClient.DownloadString(address);
					//Console.WriteLine($"Request sent, response received: {response}");


					try
					{
						var webClient = new WebClient();

						var authenticationRequired = !string.IsNullOrEmpty(login);
						if (authenticationRequired)
						{
							var credentialCache = new CredentialCache
							{
								{
									new Uri(address), "Basic", new NetworkCredential(login, password)
								}
							};

							webClient.UseDefaultCredentials = true;
							webClient.Credentials = credentialCache;
						}

						webClient.QueryString.Add("bar", Rd.Next(1000000, 9999999).ToString());
						webClient.QueryString.Add("wt", Rd.Next(100, 1500).ToString());
						webClient.QueryString.Add("l", Rd.Next(1, 200).ToString());
						webClient.QueryString.Add("w", Rd.Next(1, 200).ToString());
						webClient.QueryString.Add("h", Rd.Next(1, 200).ToString());
						var response = webClient.DownloadString(address);
						Console.WriteLine($"Request sent, response received: {response}");
					}
					catch (WebException ex)
					{
						Console.WriteLine(ex.Status);
						Console.WriteLine(ex.Message);
						Console.WriteLine(ex.ToString());
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}

			Console.WriteLine("Application finished");
		}
	}

	public class MyWebClient : WebClient
	{
		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = base.GetWebRequest(address);

			if (request != null)
				request.PreAuthenticate = true;

			if (!(request is HttpWebRequest))
				return request;

			var myWebRequest = request as HttpWebRequest;
			myWebRequest.UnsafeAuthenticatedConnectionSharing = true;
			myWebRequest.KeepAlive = true;

			return request;
		}
	}
}
