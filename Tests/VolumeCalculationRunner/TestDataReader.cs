using Primitives;
using ProcessingUtils;

namespace VolumeCalculationRunner
{
	internal class TestDataReader
	{
		public static async Task<VolumeTestCaseData> ReadTestDataAsync(DirectoryInfo testCaseDirectory)
		{
			var directoryFiles = testCaseDirectory.EnumerateFiles().ToList();
			var testCaseName = testCaseDirectory.Name;

			var description = "";
			var descriptionFile = directoryFiles.FirstOrDefault(f => f.Name == "description.txt");
			if (descriptionFile != null && descriptionFile.Exists)
				description = File.ReadAllText(descriptionFile.FullName);

			var depthMapsFolder = testCaseDirectory.EnumerateDirectories().FirstOrDefault(d => d.Name == "maps");
			if (depthMapsFolder == null || !depthMapsFolder.Exists)
				throw new IOException($"Folder with depthMaps for {testCaseName} does not exists");

			var depthMaps = await ReadDepthMapsFromFolderAsync(testCaseName, depthMapsFolder);
			var image = await ImageUtils.ReadImageDataFromFileAsync(Path.Combine(testCaseDirectory.FullName, "rgb.png"));

			var testDataFile = directoryFiles.FirstOrDefault(f => f.Name == "testdata.txt");
			if (testDataFile == null || !testDataFile.Exists)
				throw new IOException($"Test data file for {testCaseName} does no exists");

			var testDataFileContents = File.ReadAllLines(testDataFile.FullName);
			if (testDataFileContents.Length < 5)
				throw new ArithmeticException($"Test data file contents for {testCaseName} are insufficient");

			var widthMm = int.Parse(testDataFileContents[0]);
			var heightMm = int.Parse(testDataFileContents[1]);
			var depthMm = int.Parse(testDataFileContents[2]);
			var floorDepthMm = short.Parse(testDataFileContents[3]);
			var objMinHeightMm = short.Parse(testDataFileContents[4]);

			return new VolumeTestCaseData(testCaseName, description, depthMaps, image, widthMm, heightMm, depthMm, floorDepthMm, objMinHeightMm);
		}

		private static async Task<DepthMap[]> ReadDepthMapsFromFolderAsync(string testCaseName, DirectoryInfo directory)
		{
			var files = directory.EnumerateFiles().Where(f => f.Extension == ".dm").ToList();

			var depthMaps = new List<DepthMap>(files.Count);
			foreach (var file in files)
			{
				var depthMap = await DepthMapUtils.ReadDepthMapFromRawFileAsync(file.FullName);
				if (depthMap == null)
				{
					Console.WriteLine($@"Failed to read depth map from {file.Name} for {testCaseName}");
					continue;
				}

				depthMaps.Add(depthMap);
			}

			return depthMaps.ToArray();
		}
	}
}
