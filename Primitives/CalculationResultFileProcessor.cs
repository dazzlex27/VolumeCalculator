using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Primitives
{
	public class CalculationResultFileProcessor
	{
		private readonly string _outputFolderPath;

		public string FullOutputPath { get; }

		public CalculationResultFileProcessor(string outputFolderPath)
		{
			_outputFolderPath = outputFolderPath;
			FullOutputPath = Path.Combine(outputFolderPath, Constants.ResultFileName);
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
			Directory.CreateDirectory(_outputFolderPath);
			using (var resultFile = new StreamWriter(FullOutputPath, true, Encoding.Default))
			{
				var resultString = new StringBuilder();
				resultString.Append("#");
				resultString.Append($@"{Constants.CsvSeparator}date local");
				resultString.Append($@"{Constants.CsvSeparator}time local");
				resultString.Append($@"{Constants.CsvSeparator}code");
				resultString.Append($@"{Constants.CsvSeparator}weight (kg)");
				resultString.Append($@"{Constants.CsvSeparator}length (mm)");
				resultString.Append($@"{Constants.CsvSeparator}width (mm)");
				resultString.Append($@"{Constants.CsvSeparator}height (mm)");
				resultString.Append($@"{Constants.CsvSeparator}volume (mm^2)");
				resultFile.WriteLine(resultString);
				resultFile.Flush();
			}
		}

		public void WriteCalculationResult(CalculationResult result)
		{
			var safeName = result.ObjectCode;
			if (!string.IsNullOrEmpty(safeName))
			{
				var nameWithoutReturns = result.ObjectCode.Replace(Environment.NewLine, " ");
				safeName = nameWithoutReturns.Replace(Constants.CsvSeparator, " ");
			}

			var safeWeight = result.ObjectWeight.ToString(CultureInfo.InvariantCulture);

			Directory.CreateDirectory(_outputFolderPath);
			using (var resultFile = new StreamWriter(FullOutputPath, true, Encoding.Default))
			{
				var resultString = new StringBuilder();
				resultString.Append(IoUtils.GetNextUniversalObjectCounter());
				resultString.Append($@"{Constants.CsvSeparator}{result.CalculationTime.ToShortDateString()}");
				resultString.Append($@"{Constants.CsvSeparator}{result.CalculationTime.ToShortTimeString()}");
				resultString.Append($@"{Constants.CsvSeparator}{safeName}");
				resultString.Append($@"{Constants.CsvSeparator}{safeWeight}");
				resultString.Append($@"{Constants.CsvSeparator}{result.ObjectLength}");
				resultString.Append($@"{Constants.CsvSeparator}{result.ObjectWidth}");
				resultString.Append($@"{Constants.CsvSeparator}{result.ObjectHeight}");
				resultString.Append($@"{Constants.CsvSeparator}{result.ObjectVolume}");
				resultFile.WriteLine(resultString);
				resultFile.Flush();
			}
		}
	}
}