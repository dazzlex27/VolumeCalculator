using System;
using System.IO;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;
using WinSCP;

namespace ExtIntegration.RequestSenders
{
	public class FtpRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly string _baseDirectory;
		private readonly SessionOptions _sessionOptions;

		public FtpRequestSender(ILogger logger, FtpRequestSettings settings)
		{
			_logger = logger;
			_baseDirectory = settings.BaseDirectory;

			_sessionOptions = new SessionOptions
			{
				Protocol = Protocol.Ftp,
				HostName = settings.Host,
				UserName = settings.Login,
				PortNumber = settings.Port,
				Password = settings.Password,
				FtpSecure = settings.IsSecure ? FtpSecure.Explicit : FtpSecure.None,
				TlsHostCertificateFingerprint = settings.HostCertificateFingerprint
			};
		}

		public void Dispose()
		{
		}

		public void Connect()
		{
		}

		public bool Send(CalculationResultData resultData)
		{
			if (resultData.Status != CalculationStatus.Sucessful)
			{
				_logger.LogInfo($"The result was not successful but {resultData.Status}, will not send FTP request");

				return false;
			}

			var folderName = DateTime.Now.Ticks.ToString();

			try
			{
				_logger.LogInfo($"Sending files via FTP to {_sessionOptions.HostName}...");

				using (var session = new Session())
				{
					session.Open(_sessionOptions);

					var transferOptions = new TransferOptions
					{
						TransferMode = TransferMode.Binary,
						PreserveTimestamp = true
					};

					Directory.CreateDirectory(folderName);

					const string objectPhotoName = "object.png";
					var ojectPhotoPath = Path.Combine(folderName, objectPhotoName);
					ImageUtils.SaveImageDataToFile(resultData.ObjectPhoto, ojectPhotoPath);

					var result = resultData.Result;

					const string textFileName = "info.txt";
					var resultFilePath = Path.Combine(folderName, textFileName);
					using (var resultFile = File.AppendText(resultFilePath))
					{
						resultFile.WriteLine(result.ObjectCode);
						resultFile.WriteLine(result.ObjectWeightKg);
						resultFile.WriteLine(result.ObjectLengthMm);
						resultFile.WriteLine(result.ObjectWidthMm);
						resultFile.WriteLine(result.ObjectHeightMm);
					}

					_logger.LogInfo("Uploading ");
					var transferResult1 = session.PutFiles($"{ojectPhotoPath}", $"{_baseDirectory}/{ojectPhotoPath}", false,
						transferOptions);
					transferResult1.Check();
					var transferResult2 = session.PutFiles($"{resultFilePath}", $"{_baseDirectory}/{textFileName}",
						false, transferOptions);
					transferResult2.Check();

					Directory.Delete(folderName, true);

					foreach (TransferEventArgs transfer in transferResult1.Transfers)
						Console.WriteLine("Upload of {0} succeeded", transfer.FileName);

					foreach (TransferEventArgs transfer in transferResult2.Transfers)
						Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
				}

				_logger.LogInfo($"Sent files via FTP to {_sessionOptions.HostName}");

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to send data to FTP server", ex);
				return false;
			}
			finally
			{
				try
				{
					if (Directory.Exists(folderName))
						Directory.Delete(folderName, true);
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}
	}
}