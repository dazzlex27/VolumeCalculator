using System;
using System.Globalization;
using System.IO;
using System.Text;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;

namespace ExtIntegration
{
	public class CalculationResultFileProcessor
	{
		private readonly ILogger _logger;
		private readonly object _fileWriteLock;
		private string _outputFolderPath;

		public string FullOutputPath { get; }

		public CalculationResultFileProcessor(ILogger logger, string outputFolderPath)
		{
			_logger = logger;
			FullOutputPath = Path.Combine(outputFolderPath, GlobalConstants.ResultFileName);

			_fileWriteLock = new object();

			InitializeOutputFile(outputFolderPath);
		}

		public void UpdateSettings(ApplicationSettings settings)
		{
			lock (_fileWriteLock)
			{
				InitializeOutputFile(settings.GeneralSettings.OutputPath);
			}
		}

		public bool IsResultFileAccessible()
		{
			try
			{
				if (!File.Exists(FullOutputPath))
				{
					CreateFileIfNecessary();
					return true;
				}

				using (Stream stream = new FileStream(FullOutputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				{
					stream.ReadByte();
				}

				return true;
			}
			catch (IOException)
			{
				return false;
			}
		}

		public void WriteCalculationResult(CalculationResultData resultData)
		{
			if (resultData.Status != CalculationStatus.Successful)
				return;

			lock (_fileWriteLock)
			{
				try
				{
					var result = resultData.Result;

					var calculationIndex = IoUtils.GetCurrentUniversalObjectCounter();
					IoUtils.IncrementUniversalObjectCounter();
					_logger.LogInfo($"Global object ID incremented to {calculationIndex + 1}");

					var safeName = result.Barcode;
					if (!string.IsNullOrEmpty(safeName))
					{
						var nameWithoutReturns = result.Barcode.Replace(Environment.NewLine, " ");
						safeName = nameWithoutReturns.Replace(GlobalConstants.CsvSeparator, " ");
					}

					var safeWeight = result.ObjectWeight.ToString(CultureInfo.InvariantCulture);

					Directory.CreateDirectory(_outputFolderPath);
					using (var resultFile = new StreamWriter(FullOutputPath, true, Encoding.Default))
					{
						var resultString = new StringBuilder();
						resultString.Append(calculationIndex);
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.CalculationTime.ToShortDateString()}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.CalculationTime.ToShortTimeString()}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{safeName}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{safeWeight}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.UnitCount}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.ObjectLengthMm}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.ObjectWidthMm}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.ObjectHeightMm}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.ObjectVolumeMm}");
						resultString.Append($@"{GlobalConstants.CsvSeparator}{result.CalculationComment}");
						if (result.PalletSubtractionEnabled)
							resultString.Append($@"{GlobalConstants.CsvSeparator}pallet");	
						resultFile.WriteLine(resultString);
						resultFile.Flush();
					}
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to write data to csv...", ex);
				}
			}
		}
		
		private void CreateFileIfNecessary()
		{
			lock (_fileWriteLock)
			{
				Directory.CreateDirectory(_outputFolderPath);
				if (File.Exists(FullOutputPath))
					return;

				using (var resultFile = new StreamWriter(FullOutputPath, true, Encoding.Default))
				{
					var resultString = new StringBuilder();
					resultString.Append("#");
					resultString.Append($@"{GlobalConstants.CsvSeparator}date local");
					resultString.Append($@"{GlobalConstants.CsvSeparator}time local");
					resultString.Append($@"{GlobalConstants.CsvSeparator}code");
					resultString.Append($@"{GlobalConstants.CsvSeparator}weight (gr)");
					resultString.Append($@"{GlobalConstants.CsvSeparator}unitCount (p)");
					resultString.Append($@"{GlobalConstants.CsvSeparator}length (mm)");
					resultString.Append($@"{GlobalConstants.CsvSeparator}width (mm)");
					resultString.Append($@"{GlobalConstants.CsvSeparator}height (mm)");
					resultString.Append($@"{GlobalConstants.CsvSeparator}volume (cm^3)");
					resultString.Append($@"{GlobalConstants.CsvSeparator}comment");
					resultFile.WriteLine(resultString);
					resultFile.Flush();
				}
			}
		}

		private void InitializeOutputFile(string outputFolderPath)
		{
			_outputFolderPath = outputFolderPath;
			try
			{
				Directory.CreateDirectory(outputFolderPath);
				Directory.CreateDirectory(Path.Combine(outputFolderPath, "photos"));
				CreateFileIfNecessary();
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to create results folders", ex);
			}
		}
	}
}