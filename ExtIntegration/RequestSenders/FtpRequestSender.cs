using System;
using System.IO;
using System.Security.Authentication;
using System.Threading.Tasks;
using FluentFTP;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;
using ProcessingUtils;

namespace ExtIntegration.RequestSenders
{
	public sealed class FtpRequestSender : IRequestSender
	{
		private readonly ILogger _logger;
		private readonly string _baseDirectory;
		private readonly bool _includeObjectPhoto;
		private readonly AsyncFtpClient _client;
		private readonly bool _useSeparateFolders;

		public FtpRequestSender(ILogger logger, FtpRequestSettings settings)
		{
			logger.LogInfo($"Creating an FTP request handler for host {settings.Host}");
			_logger = logger;
			_baseDirectory = settings.BaseDirectory;
			_includeObjectPhoto = settings.IncludeObjectPhotos;
			_useSeparateFolders = settings.UseSeparateFolders;

			var useEncryption = settings.IsSecure;
			var config = new FtpConfig()
			{
				EncryptionMode = useEncryption ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None,
				DataConnectionEncryption = useEncryption,
				SslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12,
				ValidateAnyCertificate = true
			};
			_client = new AsyncFtpClient(settings.Host, settings.Login, settings.Password, settings.Port, config);
		}

		public void Dispose()
		{
			_client.Dispose();
		}

		public async Task ConnectAsync()
		{
			await _client.Connect();
		}

		public async Task<bool> SendAsync(CalculationResultData resultData)
		{
			if (resultData.Status != CalculationStatus.Successful)
			{
				await _logger.LogInfo($"The result was not successful ({resultData.Status}), will not send FTP request");

				return false;
			}

			try
			{
				await _logger.LogInfo($"Sending files via FTP to {_client.Host}:{_client.Port}...");

				var calculationResult = resultData.Result;

				var timeString = resultData.Result.CalculationTime.ToString("yyyyMMddHHmmss");
				var baseFileName = $"{calculationResult.Barcode}_{timeString}";
				var fileName = _useSeparateFolders ? Path.Combine(baseFileName, baseFileName) : baseFileName;

				using (var memoryStream = new MemoryStream())
				using (var writer = new StreamWriter(memoryStream))
				{
					writer.WriteLine($"barcode={calculationResult.Barcode}");
					writer.WriteLine($"weight{GetWeightUnitsString(calculationResult)}={calculationResult.ObjectWeight}");
					writer.WriteLine($"lengthMm={calculationResult.ObjectLengthMm}");
					writer.WriteLine($"widthMm={calculationResult.ObjectWidthMm}");
					writer.WriteLine($"heightMm={calculationResult.ObjectHeightMm}");
					writer.WriteLine($"unitCount={calculationResult.UnitCount}");
					writer.WriteLine($"comment={calculationResult.CalculationComment}");

					writer.Flush();
					memoryStream.Seek(0, SeekOrigin.Begin);

					var remoteFileName = Path.Combine(_baseDirectory, $"{fileName}.txt");

					var result = await _client.UploadBytes(memoryStream.ToArray(), remoteFileName, FtpRemoteExists.Overwrite, true);
					if (result == FtpStatus.Success)
						await _logger.LogInfo("Uploaded text file to FTP");
				}

				if (_includeObjectPhoto)
				{
					using (var memoryStream = new MemoryStream())
					using (var bitmap = ImageUtils.GetBitmapFromImageData(resultData.ObjectPhoto))
					{
						bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

						var remoteFileName = Path.Combine(_baseDirectory, $"{fileName}.png");

						var result = await _client.UploadBytes(memoryStream.ToArray(), remoteFileName, FtpRemoteExists.Overwrite, true);
						if (result == FtpStatus.Success)
							await _logger.LogInfo("Uploaded photo file to FTP");
					}
				}

				await _logger.LogInfo($"Uploaded files to FTP server at {_client.Host}");

				return true;
			}
			catch (Exception ex)
			{
				await _logger.LogException($"Failed to upload data to FTP server at {_client.Host}", ex);
				return false;
			}
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
