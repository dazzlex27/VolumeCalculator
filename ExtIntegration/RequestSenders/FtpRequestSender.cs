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
		private readonly bool _includeObjectPhoto;

		public FtpRequestSender(ILogger logger, FtpRequestSettings settings)
		{
			_logger = logger;
			_baseDirectory = settings.BaseDirectory;
			_includeObjectPhoto = settings.IncludeObjectPhotos;

			_sessionOptions = new SessionOptions
			{
				Protocol = Protocol.Ftp,
				HostName = settings.Host,
				UserName = settings.Login,
				PortNumber = settings.Port,
				Password = settings.Password,
				FtpSecure = settings.IsSecure ? FtpSecure.Explicit : FtpSecure.None
			};

			if (_sessionOptions.Protocol != Protocol.Ftp)
				_sessionOptions.TlsHostCertificateFingerprint = settings.HostCertificateFingerprint;
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

			var result = resultData.Result;
			var timeString = result.CalculationTime.ToString("yyyyMMddHHmmss");
			string fileName = $"{result.Barcode}_{timeString}";
			var infoFileName = $"{fileName}.txt";
			var photoFileName = $"{fileName}.png";

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

					var weightUnitsString = "";
					switch (result.WeightUnits)
					{
						case WeightUnits.Gr:
							weightUnitsString = "Gr";
							break;
						case WeightUnits.Kg:
							weightUnitsString = "Kg";
							break;
					}

					using (var resultFile = File.AppendText(infoFileName))
					{
						resultFile.WriteLine($"barcode={result.Barcode}");
						resultFile.WriteLine($"weight{weightUnitsString}={result.ObjectWeight}");
						resultFile.WriteLine($"lengthMm={result.ObjectLengthMm}");
						resultFile.WriteLine($"widthMm={result.ObjectWidthMm}");
						resultFile.WriteLine($"heightMm={result.ObjectHeightMm}");
						resultFile.WriteLine($"units={result.UnitCount}");
						resultFile.WriteLine($"comment={result.CalculationComment}");
					}

					_logger.LogInfo($"Uploading file to FTP server at {_sessionOptions.HostName}...");
					var infoRemoteName = string.IsNullOrEmpty(_baseDirectory) 
						? infoFileName 
						: $"{_baseDirectory}/{infoFileName}";
					var transferResult = session.PutFiles($"{infoFileName}", infoRemoteName, false, transferOptions);
					transferResult.Check();

					if (_includeObjectPhoto)
					{
						ImageUtils.SaveImageDataToFile(resultData.ObjectPhoto, photoFileName);
						var photoRemoteName = string.IsNullOrEmpty(_baseDirectory)
							? photoFileName
							: $"{_baseDirectory}/{photoFileName}";
						var transferResult1 = session.PutFiles($"{photoFileName}", photoRemoteName, false, transferOptions);
						transferResult1.Check();
					}
				}

				_logger.LogInfo($"Uploaded files to FTP server at {_sessionOptions.HostName}");

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to upload data to FTP server at {_sessionOptions.HostName}", ex);
				return false;
			}
			finally
			{
				try
				{
					if (File.Exists(infoFileName))
						File.Delete(infoFileName);

					if (File.Exists(photoFileName))
						File.Delete(photoFileName);
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}
	}
}