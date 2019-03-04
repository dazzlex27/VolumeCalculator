using System;
using System.Globalization;
using System.IO;
using System.Text;
using Primitives.Logging;

namespace Primitives
{
	public class CalculationResultFileProcessor
	{
		private readonly ILogger _logger;
		private readonly string _outputFolderPath;
		private readonly object _fileWriteLock;

		public string FullOutputPath { get; }

		public CalculationResultFileProcessor(ILogger logger, string outputFolderPath)
		{
			_logger = logger;
			_outputFolderPath = outputFolderPath;
			FullOutputPath = Path.Combine(outputFolderPath, Constants.ResultFileName);

			_fileWriteLock = new object();

			try
			{
				Directory.CreateDirectory(outputFolderPath);
				Directory.CreateDirectory(Path.Combine(outputFolderPath, "photos"));
			}
			catch (Exception ex)
			{
				logger.LogException("Failed to create results folders", ex);
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
				using (var resultFile = new StreamWriter(FullOutputPath, true, Encoding.Default))
				{
					var resultString = new StringBuilder();
					resultString.Append("#");
					resultString.Append($@"{Constants.CsvSeparator}date local");
					resultString.Append($@"{Constants.CsvSeparator}time local");
					resultString.Append($@"{Constants.CsvSeparator}code");
					resultString.Append($@"{Constants.CsvSeparator}weight (kg)");
					resultString.Append($@"{Constants.CsvSeparator}unitCount (p)");
					resultString.Append($@"{Constants.CsvSeparator}length (mm)");
					resultString.Append($@"{Constants.CsvSeparator}width (mm)");
					resultString.Append($@"{Constants.CsvSeparator}height (mm)");
					resultString.Append($@"{Constants.CsvSeparator}volume (cm^3)");
					resultString.Append($@"{Constants.CsvSeparator}comment");
					resultFile.WriteLine(resultString);
					resultFile.Flush();
				}
			}
		}

		public void WriteCalculationResult(CalculationResult result)
		{
			lock (_fileWriteLock)
			{
				try
				{
					var calculationIndex = IoUtils.GetCurrentUniversalObjectCounter();
					IoUtils.IncrementUniversalObjectCounter();
					_logger.LogInfo($"Global object ID incremented to {calculationIndex}");

					var safeName = result.ObjectCode;
					if (!string.IsNullOrEmpty(safeName))
					{
						var nameWithoutReturns = result.ObjectCode.Replace(Environment.NewLine, " ");
						safeName = nameWithoutReturns.Replace(Constants.CsvSeparator, " ");
					}

					var safeWeight = result.ObjectWeightKg.ToString(CultureInfo.InvariantCulture);

					Directory.CreateDirectory(_outputFolderPath);
					using (var resultFile = new StreamWriter(FullOutputPath, true, Encoding.Default))
					{
						var resultString = new StringBuilder();
						resultString.Append(calculationIndex);
						resultString.Append($@"{Constants.CsvSeparator}{result.CalculationTime.ToShortDateString()}");
						resultString.Append($@"{Constants.CsvSeparator}{result.CalculationTime.ToShortTimeString()}");
						resultString.Append($@"{Constants.CsvSeparator}{safeName}");
						resultString.Append($@"{Constants.CsvSeparator}{safeWeight}");
						resultString.Append($@"{Constants.CsvSeparator}{result.UnitCount}");
						resultString.Append($@"{Constants.CsvSeparator}{result.ObjectLengthMm}");
						resultString.Append($@"{Constants.CsvSeparator}{result.ObjectWidthMm}");
						resultString.Append($@"{Constants.CsvSeparator}{result.ObjectHeightMm}");
						resultString.Append($@"{Constants.CsvSeparator}{result.ObjectVolumeMm}");
						resultString.Append($@"{Constants.CsvSeparator}{result.CalculationComment}");
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
	}
}