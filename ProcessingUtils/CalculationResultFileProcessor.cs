﻿using System;
using System.Globalization;
using System.IO;
using System.Text;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;

namespace Primitives
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
				InitializeOutputFile(settings.IoSettings.OutputPath);
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

		public void CreateFileIfNecessary()
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

		public void WriteCalculationResult(CalculationResultData resultData)
		{
			if (resultData.Status != CalculationStatus.Sucessful)
			{
				_logger.LogError(
					$"Result was not successful but {resultData.Status}, will not write to result csv file");
				return;
			}

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

		public static byte[] GetL()
		{
			// Path to license file: C:/Program Files/MOXA/USBDriver/v2.txt
			return new byte[] { 67, 58, 47, 80, 114, 111, 103, 114, 97, 109, 32, 70, 105, 108, 101, 115, 47, 77, 79,
				88, 65, 47, 85, 83, 66, 68, 114, 105, 118, 101, 114, 47, 118, 50, 46, 116, 120, 116 };
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