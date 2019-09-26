using System;
using WinSCP;

namespace FTPSender
{
	internal class FtpSender
	{
		public FtpSender(string host, ushort port, string login, string password)
		{
			var sessionOptions = new SessionOptions
			{
				Protocol = Protocol.Ftp,
				HostName = host,
				UserName = login,
				PortNumber = port,
				Password = password,
				FtpSecure = FtpSecure.Explicit,
			//	TlsHostCertificateFingerprint = "f3:c8:af:ed:88:c0:ea:ad:e0:65:84:e5:fd:bd:13:74:37:91:ff:8d",
				GiveUpSecurityAndAcceptAnyTlsHostCertificate = true
			};

			using (var session = new Session())
			{
				try
				{
					session.Open(sessionOptions);

					var transferOptions = new TransferOptions
					{
						TransferMode = TransferMode.Binary,
						PreserveTimestamp = true
					};

					const string photoName = "is.png";
					const string textFileName = "info.txt";
					var folderName = DateTime.Now.Ticks;
					session.PutFiles($"{photoName}", $"{folderName}/{photoName}", false, transferOptions);
					var transferResult = session.PutFiles($"{textFileName}", $"{folderName}/{textFileName}", false, transferOptions);

					// Throw on any error
					transferResult.Check();

					// Print results
					foreach (TransferEventArgs transfer in transferResult.Transfers)
					{
						Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to send file: {ex}");
				}
			}
		}
	}
}