using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HttpSender
{
	internal class Program
	{
		private static readonly Random Rd = new Random();

		private static void Main()
		{
			const string defaultIp = "sha.zappstore.pro";
			const ushort defaultPort = 443;
			const string defaultUrl = "package/tools/packup-calculate";
			const string defaultLogin = "is";
			const string defaultPassword = "AKVc8ceDwUpu83ZRPp5EcVUC8GesWHgC";

			string ip = defaultIp;
			ushort port = defaultPort;
			string url = defaultUrl;
			string login = defaultLogin;
			string password = defaultPassword;

			var useHttpClient = true;

			var address = $"https://{ip}:45/{url}";

			try
			{
				if (useHttpClient)
					SendRequestViaHttpClient(port, login, password, address);
				else
					SendRequestViaWebClient(login, password, address);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			Console.WriteLine("Application finished");
		}

		private static void SendRequestViaHttpClient(ushort port, string login, string password, string address)
		{
			using (var client = new HttpClient())
			{
				var builder = new UriBuilder(address);

				var query = HttpUtility.ParseQueryString(builder.Query);
				query["wt"] = "2";
				query["l"] = "1";
				query["w"] = "1";
				query["h"] = "1";
				query["bar"] = "12345";

				builder.Query = query.ToString();
				string dstUrl = builder.ToString();

				var asciiCredentialsData = Encoding.ASCII.GetBytes($"{login}:{password}");
				var base64CredentialsData = Convert.ToBase64String(asciiCredentialsData);

				using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, dstUrl))
				{
					requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64CredentialsData);
					var dataStr = requestMessage.Headers.Authorization.ToString();

					var response = Task.Run(() => client.SendAsync(requestMessage)).Result;
				}
			}
		}

		private static void SendRequestViaWebClient(string login, string password, string address)
		{
			var webClient = new MyWebClient();
			try
			{

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
				var actualUri = ex.Response.ResponseUri;
				var credentialCache = new CredentialCache
							{
								{
									new Uri(address), "Basic", new NetworkCredential(login, password)
								}
							};

				webClient.UseDefaultCredentials = true;
				webClient.Credentials = credentialCache;
				try
				{
					var response = webClient.DownloadString(actualUri);

				}
				catch (Exception ex2)
				{
					Console.WriteLine(ex2);
				}
				Console.WriteLine(ex.Status);
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.ToString());
			}
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
