using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common;
using FrameSources;
using NUnit.Framework;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.Logic;

namespace VolumeCalculatorTest
{
	[TestFixture]
	internal class VolumeCalculationTest
	{
		private const string TestFolderFullPath = "C:/3DTest/";
		private readonly ILogger _logger;

		public VolumeCalculationTest()
		{
			_logger = new DummyLogger();
		}

		[Test]
		public void TestAllCases()
		{
			Console.WriteLine("Starting all cases test...");

			var testDirectory = new DirectoryInfo(TestFolderFullPath);
			if (!testDirectory.Exists)
			{
				Console.WriteLine("The main working directory was not found! Test terminates");
				throw new DirectoryNotFoundException();
			}

			var sw = new Stopwatch();
			sw.Start();

			var testCaseFolders = testDirectory.EnumerateDirectories().Where(d => d != null && d.Exists).ToList();
			var totalCaseCount = testCaseFolders.Count;
			Console.WriteLine($"{totalCaseCount} test cases were found");

			using (var processor = new DepthMapProcessor(_logger, GetDefaultKinectV2Params()))
			{
				var testIndex = 1;
				foreach (var testCaseDirectory in testCaseFolders)
				{
					Console.WriteLine($"== [{testIndex++}/{totalCaseCount}] testcase: {testCaseDirectory.Name} ==");

					try
					{
						TestOneCase(testCaseDirectory, processor);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			}

			sw.Stop();
			Console.WriteLine($"All cases test finished. Time elapsed: {sw.Elapsed:c}");
		}

		private static void TestOneCase(DirectoryInfo testCaseDirectory, DepthMapProcessor processor)
		{
			var testCaseData = TestDataReader.ReadTestData(testCaseDirectory);
			Console.WriteLine(testCaseData.Description);
			Console.WriteLine($"Floor depth = {testCaseData.FloorDepth}, min obj height = {testCaseData.MinObjHeight}");

			if (testCaseData.DepthMaps == null || testCaseData.DepthMaps.Length == 0)
			{
				Console.WriteLine("Skipping the test case as no maps were found");
				return;
			}
			Console.WriteLine($"Found {testCaseData.DepthMaps.Length} maps");

			var cutOffDepth = (short) (testCaseData.FloorDepth - testCaseData.MinObjHeight);
			processor.SetCalculatorSettings(testCaseData.FloorDepth, cutOffDepth);

			var results = new List<ObjectVolumeData>();

			foreach (var map in testCaseData.DepthMaps)
			{
				var objectDimData = processor.CalculateVolume(map);
				var result = new ObjectVolumeData(objectDimData.Width, objectDimData.Height, objectDimData.Depth);
				results.Add(result);
			}

			var widths = results.Select(r => r.Width).ToArray();
			var heights = results.Select(r => r.Height).ToArray();
			var depths = results.Select(r => r.Depth).ToArray();

			var modeWidth = widths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
			var modeHeight = heights.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
			var modeDepth = depths.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

			var minWidth = widths.Min();
			var minHeight = heights.Min();
			var minDepth = depths.Min();

			var maxWidth = widths.Max();
			var maxHeight = heights.Max();
			var maxDepth = depths.Max();

			var widthDeviation = Math.Abs(testCaseData.ObjWidth - modeWidth);
			var heightDeviation = Math.Abs(testCaseData.ObjHeight - modeHeight);
			var depthDeviation = Math.Abs(testCaseData.ObjDepth - modeDepth);

			var widthSpread = maxWidth - minWidth;
			var heightSpread = maxHeight - minHeight;
			var depthSpread = maxDepth - minDepth;

			Console.WriteLine($@"GT = {{{testCaseData.ObjWidth} {testCaseData.ObjHeight} {testCaseData.ObjDepth}}}");
			Console.WriteLine($"Mode = {{{modeWidth} {modeHeight} {modeDepth}}}");
			Console.WriteLine($"Min = {{{minWidth} {minHeight} {minDepth}}}");
			Console.WriteLine($"Max = {{{maxWidth} {maxHeight} {maxDepth}}}");
			Console.WriteLine($"Deviation: devW={widthDeviation}, devH={heightDeviation}, devD={depthDeviation}");
			Console.WriteLine($"Spread: deltaW={widthSpread}, deltaH={heightSpread} deltaD={depthSpread}");
		}

		private static DeviceParams GetDefaultKinectV2Params()
		{
			return new DeviceParams(70.6f, 60.0f, 367.7066f, 367.7066f, 257.8094f, 207.3965f, 600, 5000);
		}
	}
}