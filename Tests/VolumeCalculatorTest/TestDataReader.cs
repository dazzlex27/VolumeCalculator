using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace VolumeCalculatorTest
{
	internal class TestDataReader
	{
		public static VolumeTestCaseData ReadTestData(DirectoryInfo testCaseDirectory)
		{
			var directoryFiles = testCaseDirectory.EnumerateFiles().ToList();
			var testCaseName = testCaseDirectory.Name;

			var description = "";
			var descriptionFile = directoryFiles.FirstOrDefault(f => f.Name == "description.txt");
			if (descriptionFile != null && descriptionFile.Exists)
				description = File.ReadAllText(descriptionFile.FullName);

			var depthMapsFolder = testCaseDirectory.EnumerateDirectories().FirstOrDefault(d => d.Name == "maps");
			if (depthMapsFolder == null || !depthMapsFolder.Exists)
				throw new IOException($"Folder with depthMaps for {testCaseName} does no exists");

			var depthMaps = ReadDepthMapsFromFolder(testCaseName, depthMapsFolder);

			var testDataFile = directoryFiles.FirstOrDefault(f => f.Name == "testdata.txt");
			if (testDataFile == null || !testDataFile.Exists)
				throw new IOException($"Test data file for {testCaseName} does no exists");

			var testDataFileContents = File.ReadAllLines(testDataFile.FullName);
			if (testDataFileContents.Length < 5)
				throw new ArithmeticException($"Test data file contents for {testCaseName} are insufficient");

			var width = int.Parse(testDataFileContents[0]);
			var height = int.Parse(testDataFileContents[1]);
			var depth = int.Parse(testDataFileContents[2]);
			var floorDepth = short.Parse(testDataFileContents[3]);
			var objMinHeight = short.Parse(testDataFileContents[4]);

			return new VolumeTestCaseData(testCaseName, description, depthMaps, width, height, depth, floorDepth, objMinHeight);
		}

		private static DepthMap[] ReadDepthMapsFromFolder(string testCaseName, DirectoryInfo directory)
		{
			var files = directory.EnumerateFiles().Where(f => f.Extension == ".dm").ToList();

			var depthMaps = new List<DepthMap>(files.Count);
			foreach (var file in files)
			{
				var depthMap = DepthMapUtils.ReadDepthMapFromFile(file.FullName);
				if (depthMap == null)
				{
					Console.WriteLine($"Failed to read depth map from {file.Name} for {testCaseName}");
					continue;
				}

				depthMaps.Add(depthMap);
			}

			return depthMaps.ToArray();
		}
	}
}
