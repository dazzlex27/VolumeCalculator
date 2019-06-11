using System;
using System.IO;
using System.Net;

namespace HttpListenerApp
{
	internal class Program
	{
		// This example requires the System and System.Net namespaces.
		public static void SimpleListenerExample(string ip, string port)
		{
			var prefix = $"http://{ip}:{port}/";

			if (!HttpListener.IsSupported)
			{
				Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
				return;
			}
			//// URI prefixes are required,
			//// for example "http://contoso.com:8080/index/".
			//if (prefixes == null || prefixes.Length == 0)
			//	throw new ArgumentException("prefixes");

			// Note: The GetContext method blocks while waiting for a request. 
			while (true)
			{
				Console.WriteLine("Listening 2...");
				// Create a listener.
				var listener = new HttpListener();
				// Add the prefixes.
				//foreach (string s in prefixes)
				//{
					listener.Prefixes.Add(prefix);
				//}
				listener.Start();

				var context = listener.GetContext();
				var request = context.Request;
				Console.WriteLine($"Accepted! {request.RawUrl}");
				// Obtain a response object.
				var response = context.Response;
				// Construct a response.
				string xmlMessage = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n" +
				                    "construct your xml request message as required by that method along with parameters";
				string url = "http://XXXX.YYYY/ZZZZ/ABCD.aspx";

				byte[] requestInFormOfBytes = System.Text.Encoding.ASCII.GetBytes(xmlMessage);

				byte[] buffer = System.Text.Encoding.UTF8.GetBytes(xmlMessage);
				// Get a response stream and write the response to it.
				response.ContentLength64 = buffer.Length;
				var output = response.OutputStream;
				output.Write(buffer, 0, buffer.Length);
				// You must close the output stream.
				output.Close();

				listener.Stop();
			}
		}

		static void Main(string[] args)
		{
			Console.WriteLine("ip:");
			var ip = Console.ReadLine();
			Console.WriteLine("port:");
			var port = Console.ReadLine();
			SimpleListenerExample(ip, port);
			Console.ReadKey();
		}
	}
}
