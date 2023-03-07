using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HttpService
{
	internal class HttpService
	{
		private readonly string _prefix;

		public HttpService(string ip, ushort port)
		{
			_prefix = $"http://{ip}:{port}/";
			Task.Factory.StartNew((o) =>
			{
				try
				{
					RunTask();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"ERROR: request handler failed! {Environment.NewLine}{ex}");
				}
			}, TaskCreationOptions.LongRunning);
		}

		private void RunTask()
		{
			const string startCommand = "calculate";

			var listener = new HttpListener();
			listener.Prefixes.Add(_prefix);
			listener.Start();

			while (true)
			{
				try
				{
					Console.WriteLine($"Waiting for commands from {_prefix}...");

					var context = listener.GetContext();
					var request = context.Request;
					var inputContent = ReadInputContent(request);
					var url = request.RawUrl;
					var command = url.Contains("?")
						? url.Substring(1, url.IndexOf("?", StringComparison.Ordinal))
						: url.Substring(1);

					Console.WriteLine($"Client accepted! url=\"{request.RawUrl}\", method=\"{request.HttpMethod}\"");

					if (command != startCommand)
					{
						Console.WriteLine($"The command was not \"{startCommand}\", skipping the command...");
						continue;
					}

					Console.WriteLine("Generating a sample response...");
					var responseString = GenerateResponceText();
					var responseBuffer = Encoding.UTF8.GetBytes(responseString);

					Console.WriteLine("Sending the response...");
					var response = context.Response;
					response.ContentLength64 = responseBuffer.Length;
					var output = response.OutputStream;
					output.Write(responseBuffer, 0, responseBuffer.Length);

					output.Close();
					Console.WriteLine($"Response sent to {_prefix}");
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					Thread.Sleep(1000);
				}
			}
		}

		private string ReadInputContent(HttpListenerRequest request)
		{
			string documentContents;
			using (Stream receiveStream = request.InputStream)
			{
				using var readStream = new StreamReader(receiveStream, Encoding.UTF8);
				documentContents = readStream.ReadToEnd();
			}
			return documentContents;
		}

		private string GenerateResponceText()
		{
			var doc = new XDocument(new XDeclaration("1.0", "us-utf8", null),
				new XElement("calculationResult",
					new XElement("barcode", 1234567890),
					new XElement("weight", 0.365),
					new XElement("length", 51),
					new XElement("width", 49),
					new XElement("height", 50),
					new XElement("units", 1),
					new XElement("comment", "this is a sample calculation result")
				));

			using var writer = new StringWriter();
			doc.Save(writer);
			return writer.ToString();
		}
	}
}
