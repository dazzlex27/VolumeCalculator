﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FrameProcessor;
using FrameProviders;
using Primitives.Logging;
using Primitives.Settings;

namespace VolumeCalculationRunner
{
	internal class VolumeCalculationRunner
	{
		private const string TabChar = "\t";
		private const string OutputFolderName = "TestOutput";
		private const string TestFolderFullPath = "3DTest";
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
		
		public void TestAllCases()
		{
			LogVerbose(@"Starting all cases test...");

			var testDirectory = new DirectoryInfo(TestFolderFullPath);
			if (!testDirectory.Exists)
			{
				LogVerbose($@"The main working directory ({TestFolderFullPath}) was not found! Program terminates");
				return;
			}

			var sw = new Stopwatch();
			sw.Start();

			var testCaseFolders = testDirectory.EnumerateDirectories().Where(d => d != null && d.Exists).ToList();
			var totalCaseCount = testCaseFolders.Count;
			LogVerbose($@"{totalCaseCount} test cases were found");

			using (var processor = new DepthMapProcessor(_logger, GetDefaultKinectV2ColorCameraParams(), GetDefaultKinectV2DepthCameraParams()))
			{
				var testIndex = 1;
				foreach (var testCaseDirectory in testCaseFolders)
				{
					LogVerbose($@"== [{testIndex++}/{totalCaseCount}] testcase: {testCaseDirectory.Name} ==");

					try
					{
						TestOneCase(testCaseDirectory, processor);
					}
					catch (Exception e)
					{
						LogVerbose(e.ToString());
					}
				}
			}

			sw.Stop();

			LogVerbose($@"All cases test finished. Time elapsed: {sw.Elapsed:c}");
		}

		private void TestOneCase(DirectoryInfo testCaseDirectory, DepthMapProcessor processor)
		{
			var testCaseData = TestDataReader.ReadTestData(testCaseDirectory);
			var descriptionOneLine = testCaseData.Description.Replace(Environment.NewLine, " ").Replace(TabChar, " ");
			LogVerbose($@"Description: {descriptionOneLine}");
			LogVerbose($@"Floor depth = {testCaseData.FloorDepth}, min obj height = {testCaseData.MinObjHeight}");

			if (testCaseData.DepthMaps == null || testCaseData.DepthMaps.Length == 0)
			{
				LogVerbose(@"Skipping the test case as no maps were found");
				return;
			}
			LogVerbose($@"Found {testCaseData.DepthMaps.Length} maps");

			LogVerbose("Calculating volume...");
			var settings = ApplicationSettings.GetDefaultSettings();
			processor.SetProcessorSettings(settings);

			var results = new List<ObjectVolumeData>();

			foreach (var map in testCaseData.DepthMaps)
			{
				var objectDimData = processor.CalculateVolume(map, null, 0, 0, false, false, 0);
				results.Add(objectDimData);
			}

			var lengths = results.Select(r => r.LengthMm).ToArray();
			var widths = results.Select(r => r.WidthMm).ToArray();
			var heights = results.Select(r => r.HeightMm).ToArray();

			var modeLength = lengths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
			var modeWidth = widths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
			var modeHeight = heights.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

			var minLength = lengths.Min();
			var minWidth = widths.Min();
			var minHeight = heights.Min();

			var maxLength = lengths.Max();
			var maxWidth = widths.Max();
			var maxHeight = heights.Max();

			var lengthDeviation = Math.Abs(testCaseData.ObjLength - modeLength);
			var widthDeviation = Math.Abs(testCaseData.ObjWidth - modeWidth);
			var heightDeviation = Math.Abs(testCaseData.ObjHeight - modeHeight);

			var lengthDispersion = maxLength - minLength;
			var widthDispersion = maxWidth - minWidth;
			var heightDispersion = maxHeight - minHeight;

			LogVerbose($@"GT = {{{testCaseData.ObjLength} {testCaseData.ObjWidth} {testCaseData.ObjHeight}}}");
			LogVerbose($@"Mode = {{{modeLength} {modeWidth} {modeHeight}}}");
			LogVerbose($@"Min: L={minLength} W={minWidth} H={minHeight}");
			LogVerbose($@"Max: L={maxLength} W={maxWidth} H={maxHeight}");
			LogVerbose($@"Deviation: L={lengthDeviation}, W={widthDeviation}, H={heightDeviation}");
			LogVerbose($@"Dispersion: L={lengthDispersion}, W={widthDispersion}, H={heightDispersion}");

			using (var logData = File.AppendText(_logRawDataName))
			{
				var rawDataString = new StringBuilder(testCaseData.CaseName);
				rawDataString.Append($@"{TabChar}{testCaseData.Description}");
				rawDataString.Append($@"{TabChar}{testCaseData.FloorDepth}");
				rawDataString.Append($@"{TabChar}{testCaseData.MinObjHeight}");
				rawDataString.Append($@"{TabChar}{testCaseData.DepthMaps.Length}");
				rawDataString.Append($@"{TabChar}{testCaseData.ObjLength}");
				rawDataString.Append($@"{TabChar}{testCaseData.ObjWidth}");
				rawDataString.Append($@"{TabChar}{testCaseData.ObjHeight}");
				rawDataString.Append($@"{TabChar}{modeLength}");
				rawDataString.Append($@"{TabChar}{modeWidth}");
				rawDataString.Append($@"{TabChar}{modeHeight}");
				rawDataString.Append($@"{TabChar}{lengthDeviation}");
				rawDataString.Append($@"{TabChar}{widthDeviation}");
				rawDataString.Append($@"{TabChar}{heightDeviation}");
				rawDataString.Append($@"{TabChar}{minLength}");
				rawDataString.Append($@"{TabChar}{minWidth}");
				rawDataString.Append($@"{TabChar}{minHeight}");
				rawDataString.Append($@"{TabChar}{maxLength}");
				rawDataString.Append($@"{TabChar}{maxWidth}");
				rawDataString.Append($@"{TabChar}{maxHeight}");
				rawDataString.Append($@"{TabChar}{lengthDispersion}");
				rawDataString.Append($@"{TabChar}{widthDispersion}");
				rawDataString.Append($@"{TabChar}{heightDispersion}");
				logData.WriteLine(rawDataString);
			}
		}

		private void LogVerbose(string message)
		{
			Console.WriteLine(message);
			using (var logVerbose = File.AppendText(_logVerboseName))
			{
				logVerbose.WriteLine(message);
			}
		}

		private void WriteHeadersToDataLog()
		{
			using (var logData = File.AppendText(_logRawDataName))
			{
				var rawDataHeadersString = new StringBuilder("Name");
				rawDataHeadersString.Append($@"{TabChar}Description");
				rawDataHeadersString.Append($@"{TabChar}FloorDepth");
				rawDataHeadersString.Append($@"{TabChar}MinObjHeight");
				rawDataHeadersString.Append($@"{TabChar}Measurements count");
				rawDataHeadersString.Append($@"{TabChar}GT length");
				rawDataHeadersString.Append($@"{TabChar}GT width");
				rawDataHeadersString.Append($@"{TabChar}GT height");
				rawDataHeadersString.Append($@"{TabChar}Measured length");
				rawDataHeadersString.Append($@"{TabChar}Measured width");
				rawDataHeadersString.Append($@"{TabChar}Measured height");
				rawDataHeadersString.Append($@"{TabChar}Delta length");
				rawDataHeadersString.Append($@"{TabChar}Delta width");
				rawDataHeadersString.Append($@"{TabChar}Delta height");
				rawDataHeadersString.Append($@"{TabChar}Min length");
				rawDataHeadersString.Append($@"{TabChar}Min width");
				rawDataHeadersString.Append($@"{TabChar}Min height");
				rawDataHeadersString.Append($@"{TabChar}Max length");
				rawDataHeadersString.Append($@"{TabChar}Max width");
				rawDataHeadersString.Append($@"{TabChar}Max height");
				rawDataHeadersString.Append($@"{TabChar}Length dispersion");
				rawDataHeadersString.Append($@"{TabChar}Width dispersion");
				rawDataHeadersString.Append($@"{TabChar}Height dispersion");
				logData.WriteLine(rawDataHeadersString);
			}
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