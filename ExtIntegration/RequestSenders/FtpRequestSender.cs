using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using FluentFTP;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	public class FtpRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly string _hostname;
		private readonly int _port;
		private readonly string _baseDirectory;
		private readonly bool _includeObjectPhoto;
		private readonly NetworkCredential _credentials;
		private readonly FtpClient _client;

		public FtpRequestSender(ILogger logger, FtpRequestSettings settings)
		{
			logger.LogInfo($"Creating an FTP request handler for host {settings.Host}");
			_logger = logger;

			_hostname = settings.Host;
			_port = settings.Port;
			 _baseDirectory = settings.BaseDirectory;
			_includeObjectPhoto = settings.IncludeObjectPhotos;
			_credentials = new NetworkCredential(settings.Login, settings.Password);

			var useSecureConnection = settings.IsSecure;
			_client = new FtpClient(_hostname, _port, _credentials)
			{
				EncryptionMode = useSecureConnection ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None,
				DataConnectionEncryption = useSecureConnection,
				SslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12
			};
			_client.ValidateCertificate += OnValidateCertificate;
		}

		private void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
		{
			e.Accept = true;
		}

		public void Dispose()
		{
			_client.ValidateCertificate -= OnValidateCertificate;
			_client.Dispose();
		}

		public void Connect()
		{
			_client.Connect();
		}

		public bool Send(CalculationResultData resultData)
		{
			if (resultData.Status != CalculationStatus.Sucessful)
			{
				_logger.LogInfo($"The result was not successful ({resultData.Status}), will not send FTP request");

				return false;
			}

			try
			{
				_logger.LogInfo($"Sending files via FTP to {_hostname}...");

				var timeString = resultData.Result.CalculationTime.ToString("yyyyMMddHHmmss");
				var fileName = $"{resultData.Result.Barcode}_{timeString}";

				using (var memoryStream = GetTextFileMemoryStream(resultData.Result))
				{
					var remoteFileName = Path.Combine(_baseDirectory, $"{fileName}.txt");

					var result = _client.UploadAsync(memoryStream, remoteFileName, FtpExists.Overwrite, true).GetAwaiter().GetResult();
					if (result)
						_logger.LogInfo("Uploaded text file to FTP");
				}

				if (_includeObjectPhoto)
				{
					using (var memoryStream = GetPhotoFileMemoryStream(resultData))
					{
						var remoteFileName = Path.Combine(_baseDirectory, $"{fileName}.png");

						var result = _client.UploadAsync(memoryStream, remoteFileName, FtpExists.Overwrite, true).GetAwaiter().GetResult();
						if (result)
							_logger.LogInfo("Uploaded photo file to FTP");
					}
				}

				_logger.LogInfo($"Uploaded files to FTP server at {_hostname}");

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException($"Failed to upload data to FTP server at {_hostname}", ex);
				return false;
			}
		}

		private static MemoryStream GetPhotoFileMemoryStream(CalculationResultData resultData)
		{
			var stream = new MemoryStream();
			using (var bmp = ImageUtils.GetBitmapFromImageData(resultData.ObjectPhoto))
			{
				bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
			}

			return stream;
		}

		private static MemoryStream GetTextFileMemoryStream(CalculationResult result)
		{
			var weightUnitsString = GetWeightUnitsString(result);

			var stream = new MemoryStream();
			using (var writer = new StreamWriter(stream))
			{
				writer.WriteLine($"barcode={result.Barcode}");
				writer.WriteLine($"weight{weightUnitsString}={result.ObjectWeight}");
				writer.WriteLine($"lengthMm={result.ObjectLengthMm}");
				writer.WriteLine($"widthMm={result.ObjectWidthMm}");
				writer.WriteLine($"heightMm={result.ObjectHeightMm}");
				writer.WriteLine($"unitCount={result.UnitCount}");
				writer.WriteLine($"comment={result.CalculationComment}");

				writer.Flush();
				stream.Seek(0, SeekOrigin.Begin);
			}

			return stream;
		}

		private static string GetWeightUnitsString(CalculationResult result)
		{
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

			return weightUnitsString;
		}
	}
}