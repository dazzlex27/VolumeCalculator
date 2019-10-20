using FluentFTP;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Authentication;

namespace FTPSender
{
	internal class FtpSender
	{
		public FtpSender(string host, ushort port, string login, string password, bool useEncryption)
		{
			try
			{
				var credentials = new NetworkCredential(login, password);
				using (var client = new FtpClient(host, port, credentials)
				{
					EncryptionMode = useEncryption ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None,
					DataConnectionEncryption = useEncryption,
					SslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12
				})
				{
					client.ValidateCertificate += Client_ValidateCertificate;
					client.Connect();
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

						var result = client.UploadAsync(memoryStream, remoteFileName, FtpExists.Overwrite, true).GetAwaiter().GetResult();
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

							var result = client.UploadAsync(memoryStream, remoteFileName, FtpExists.Overwrite, true).GetAwaiter().GetResult();
						}
					}

					Console.WriteLine("Saved the image file file");

					client.ValidateCertificate -= Client_ValidateCertificate;
					client.Disconnect();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to upload test files: {ex}");
			}
		}

		private void Client_ValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
		{
			e.Accept = true;
		}
	}
}