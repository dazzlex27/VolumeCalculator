using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WmsEmulator
{
	internal class WmsEmulator
	{
		private readonly HttpClient _httpClient;
		private readonly string _address;

		public WmsEmulator(string ip, int port)
		{
			_address = $"http://{ip}:{port}/";

			_httpClient = new HttpClient();
			Task.Run(() =>
			{
				try
				{
					RunTask();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					throw;
				}
			});
		}

		private void RunTask()
		{
			const string startCommand = "calculate";
			var startRequest = $"{_address}{startCommand}";

			while (true)
			{
				try
				{
					Console.WriteLine($"Press 0 to send \"{startRequest}\" (1 - include photo)...");

					var text = Console.ReadLine();
					if (text != "0" && text != "1")
						continue;

					var values = new Dictionary<string, string>
					{
						{ "thing1", "hello" },
						{ "thing2", "world" }
					};

					if (text == "1")
						startRequest += "_ph";

					var content = new FormUrlEncodedContent(values);
					var response = _httpClient.PostAsync(startRequest, content).Result;
					var responceString = response.Content.ReadAsStringAsync().Result;
					Console.WriteLine(responceString);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Exception occured! {ex}");
					Console.WriteLine("WMS emulation service crashed, restarting... ");
					Thread.Sleep(1000);
				}
			}
		}
	}
}