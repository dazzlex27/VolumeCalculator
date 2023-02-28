using FluentFTP;
using System;
using System.Drawing;
using System.IO;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace FTPSender
{
	internal class FtpSender : IDisposable
	{
		private readonly AsyncFtpClient _client;

		public FtpSender(string host, ushort port, string login, string password, bool useEncryption)
		{
			var config = new FtpConfig()
			{
				EncryptionMode = useEncryption ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None,
				DataConnectionEncryption = useEncryption,
				SslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12,
				ValidateAnyCertificate = true
			};
			_client = new AsyncFtpClient(host, login, password, port, config);
		}

		public async Task UploadAsync()
		{
			try
			{
				await _client.Connect();
				Console.WriteLine("Connected");
				Console.ReadKey();

				using (var memoryStream = new MemoryStream())
				using (var writer = new StreamWriter(memoryStream))
				{
					writer.WriteLine("test:");
					writer.WriteLine(DateTime.Now.ToString());
					writer.Flush();
					memoryStream.Seek(0, SeekOrigin.Begin);

					var filename = "test.txt";
					var remotePath = "isTest";

					var remoteFileName = Path.Combine(remotePath, filename);

					var result = await _client.UploadBytes(memoryStream.ToArray(), remoteFileName, FtpRemoteExists.Overwrite, true);
				}

				Console.WriteLine("Saved the text file");

				using (var memoryStream = new MemoryStream())
				{
					using (var bitmap = new Bitmap(40, 30))
					{
						bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

						var filename = "test.png";
						var remotePath = "isTest";

						var remoteFileName = Path.Combine(remotePath, filename);

						var result = await _client.UploadBytes(memoryStream.ToArray(), remoteFileName, FtpRemoteExists.Overwrite, true);
					}
				}

				Console.WriteLine("Saved the image file file");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to upload test files: {ex}");
			}
			finally
			{
				await _client.Disconnect();
			}
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}
