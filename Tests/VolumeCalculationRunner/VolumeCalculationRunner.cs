using System.Diagnostics;
using System.Text;
using FrameProcessor;
using FrameProviders;
using Primitives.Calculation;
using Primitives.Logging;
using Primitives.Settings;
using VCServer.VolumeCalculation;

namespace VolumeCalculationRunner
{
	internal sealed class VolumeCalculationRunner : IDisposable
	{
		private const string TestEnvVar = "VCALC_TEST_PATH";
		private const string TabChar = "\t";
		private const string OutputFolderName = "TestOutput";
		private readonly ILogger _logger;
		private readonly string _logVerboseName;
		private readonly string _logRawDataName;

		public VolumeCalculationRunner()
		{
			_logger = new DummyLogger();
			var currentInstanceFolder = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
			var folderPath = Path.Combine(OutputFolderName, currentInstanceFolder);
			Directory.CreateDirectory(folderPath);
			_logVerboseName = Path.Combine(folderPath, "testVerbose.log");
			_logRawDataName = Path.Combine(folderPath, "testRawData.log");
			WriteHeadersToDataLog();
		}

		public async Task<bool> TestAllCasesAsync()
		{
			await LogVerbose(@"Starting all cases test...");

			var testInputPath = Environment.GetEnvironmentVariable(TestEnvVar);
			if (string.IsNullOrEmpty(testInputPath))
			{
				await LogVerbose($@"The environment variable {nameof(TestEnvVar)} is not valid! Program terminates");
				return false;
			}

			var testDirectory = new DirectoryInfo(testInputPath);
			if (!testDirectory.Exists)
			{
				await LogVerbose($@"The main working directory ({testInputPath}) was not found! Program terminates");
				return false;
			}

			var sw = new Stopwatch();
			sw.Start();

			var testCaseFolders = testDirectory.EnumerateDirectories().ToList();
			var totalCaseCount = testCaseFolders.Count;
			if (totalCaseCount== 0)
			{
				await LogVerbose($@"No valid test cases were found! Program terminates");
				return false;
			}

			await LogVerbose($@"{totalCaseCount} test cases were found");

			var totalResultCalculator = new TotalResultCalculator();

			using (var processor = new DepthMapProcessor(_logger, GetDefaultKinectV2ColorCameraParams(),
				GetDefaultKinectV2DepthCameraParams()))
			{
				var testIndex = 1;
				foreach (var testCaseDirectory in testCaseFolders)
				{
					await LogVerbose($@"== [{testIndex++}/{totalCaseCount}] testcase: {testCaseDirectory.Name} ==");

					try
					{
						var caseResult = await TestOneCaseAsync(testCaseDirectory, processor);
						if (caseResult == null || caseResult.Status == TestCaseResultType.TestFailure)
							return false;

						totalResultCalculator.AddTestCaseResult(caseResult);
					}
					catch (Exception e)
					{
						await LogVerbose(e.ToString());
						return false;
					}
				}
			}

			sw.Stop();
			await LogVerbose($@"All cases test finished. Time elapsed: {sw.Elapsed:c}");

			await LogVerbose("Calculating and writing metrics...");

			var totalResult = totalResultCalculator.GetAllCasesResult();
			await LogVerbose($"Tests failed: {totalResult.FailedTestCount}");
			await LogVerbose($"Tests finished sucessfully: {totalResult.SuccessfulTestCount}");
			await LogVerbose($"Avg length calculation accuracy: {totalResult.AvgLengthAccuracy * 100:N2}%");
			await LogVerbose($"Avg width calculation accuracy: {totalResult.AvgWidthAccuracy * 100:N2}%");
			await LogVerbose($"Avg height calculation accuracy: {totalResult.AvgHeightAccuracy * 100:N2}%");
			await LogVerbose($"Avg volume calculation accuracy: {totalResult.AvgVolumeAccuracy * 100:N2}%");

			if (!totalResult.IsTestResultCorrect)
			{
				await LogVerbose(totalResult.ErrorMessage);
				return false;
			}

			return true;
		}

		public void Dispose()
		{
			_logger?.Dispose();
		}

		private async Task<VolumeTestCaseResult> TestOneCaseAsync(DirectoryInfo testCaseDirectory, DepthMapProcessor processor,
			bool logEnabled = false)
		{
			try
			{
				var testCaseData = await TestDataReader.ReadTestDataAsync(testCaseDirectory);
				if (logEnabled)
				{
					var descriptionOneLine = testCaseData.Description.Replace(Environment.NewLine, " ").Replace(TabChar, " ");
					await LogVerbose($@"Description: {descriptionOneLine}");
					await LogVerbose($@"Floor depth = {testCaseData.FloorDepthMm}, min obj height = {testCaseData.MinObjHeightMm}");
				}

				if (testCaseData.DepthMaps == null || testCaseData.DepthMaps.Length == 0)
				{
					if (logEnabled)
						await LogVerbose(@"Skipping the test case as no maps were found");

					return new VolumeTestCaseResult(TestCaseResultType.Skipped);
				}

				if (logEnabled)
				{
					await LogVerbose($@"Found {testCaseData.DepthMaps.Length} maps");
					await LogVerbose("Calculating volume...");
				}

				try
				{
					var settings = ApplicationSettings.GetDefaultDebugSettings();
					settings.GeneralSettings.OutputPath = ""; // reset debug directories
					settings.AlgorithmSettings.WorkArea.FloorDepth = testCaseData.FloorDepthMm;
					settings.AlgorithmSettings.WorkArea.MinObjectHeight = testCaseData.MinObjHeightMm;

					var cutOffDepth = (short)(testCaseData.FloorDepthMm - testCaseData.MinObjHeightMm);
					var calctulationData = new VolumeCalculationData(settings.AlgorithmSettings.SampleDepthMapCount,
						"000", 0, true, true, true, "", 0);

					processor.SetProcessorSettings(settings);

					var dummyLogger = new DummyLogger();
					var volumeCalculator = new VolumeCalculator(dummyLogger, processor);

					// pad images to be the same length as maps
					var duplicatedImagesArray = Enumerable.Repeat(testCaseData.Image, calctulationData.RequiredSampleCount).ToArray();

					var calculationResult = volumeCalculator.Calculate(duplicatedImagesArray, testCaseData.DepthMaps, calctulationData, -1);

					var status = calculationResult.Status == CalculationStatus.Successful ? 
						TestCaseResultType.Success : TestCaseResultType.ProcessingFailure;

					var caseResult = new VolumeTestCaseResult(status, testCaseData, calculationResult);

					if (logEnabled)
						await LogOneCaseTestData(testCaseData, calculationResult, caseResult);

					return caseResult;
				}
				catch (Exception)
				{
					return new VolumeTestCaseResult(TestCaseResultType.ProcessingFailure);
				}
			}
			catch (Exception)
			{
				return new VolumeTestCaseResult(TestCaseResultType.TestFailure);
			}
		}

		private async Task LogOneCaseTestData(VolumeTestCaseData testCaseData, VolumeCalculationResultData calcResult,
			VolumeTestCaseResult caseResult)
		{
			var result = calcResult.Result;

			await LogVerbose($@"GT = {{{testCaseData.ObjLengthMm} {testCaseData.ObjWidthMm} {testCaseData.ObjHeightMm}}}");
			await LogVerbose($@"Mode = {{{result.LengthMm} {result.WidthMm} {result.HeightMm}}}");

			var logData = File.AppendText(_logRawDataName);
			var rawDataString = new StringBuilder(testCaseData.CaseName);
			rawDataString.Append($@"{TabChar}{testCaseData.Description}");
			rawDataString.Append($@"{TabChar}{testCaseData.FloorDepthMm}");
			rawDataString.Append($@"{TabChar}{testCaseData.MinObjHeightMm}");
			rawDataString.Append($@"{TabChar}{testCaseData.ObjLengthMm}");
			rawDataString.Append($@"{TabChar}{testCaseData.ObjWidthMm}");
			rawDataString.Append($@"{TabChar}{testCaseData.ObjHeightMm}");
			rawDataString.Append($@"{TabChar}{result.LengthMm}");
			rawDataString.Append($@"{TabChar}{result.WidthMm}");
			rawDataString.Append($@"{TabChar}{result.HeightMm}");
			logData.WriteLine(rawDataString);
		}

		private async Task LogVerbose(string message)
		{
			Console.WriteLine(message);
			await File.AppendAllTextAsync(_logVerboseName, $"{message}{Environment.NewLine}");
		}

		private void WriteHeadersToDataLog()
		{
			using var logData = File.AppendText(_logRawDataName);
			var rawDataHeadersString = new StringBuilder("Name");
			rawDataHeadersString.Append($@"{TabChar}Description");
			rawDataHeadersString.Append($@"{TabChar}FloorDepth");
			rawDataHeadersString.Append($@"{TabChar}MinObjHeight");
			rawDataHeadersString.Append($@"{TabChar}GT length");
			rawDataHeadersString.Append($@"{TabChar}GT width");
			rawDataHeadersString.Append($@"{TabChar}GT height");
			rawDataHeadersString.Append($@"{TabChar}Measured length");
			rawDataHeadersString.Append($@"{TabChar}Measured width");
			rawDataHeadersString.Append($@"{TabChar}Measured height");
			logData.WriteLine(rawDataHeadersString);
		}

		private static ColorCameraParams GetDefaultKinectV2ColorCameraParams()
		{
			return new ColorCameraParams(84.1f, 53.8f, 1081.37f, 1081.37f, 959.5f, 539.5f);
		}

		private static DepthCameraParams GetDefaultKinectV2DepthCameraParams()
		{
			return new DepthCameraParams(70.6f, 60.0f, 367.7066f, 367.7066f, 257.8094f, 207.3965f, 600, 5000);
		}
	}
}
