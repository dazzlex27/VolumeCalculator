﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using FrameProviders;
using Primitives;

namespace FrameProcessor
{
	public class TestDataGenerator
	{
		private readonly TestCaseBasicInfo _basicCaseInfo;
		private readonly string _mapSavingPath;
		private readonly TestCaseData _testCaseData;
		private readonly string _testCaseDirectory;
		private int _remainingTimesToSave;

		public TestDataGenerator(TestCaseData testCaseData)
		{
			_testCaseData = testCaseData;
			_basicCaseInfo = _testCaseData.BasicInfo;

			_remainingTimesToSave = _basicCaseInfo.TimesToSave;

			_testCaseDirectory =
				Path.Combine(_testCaseData.BasicInfo.SavingDirectory, _testCaseData.BasicInfo.Casename);
			_mapSavingPath = Path.Combine(_testCaseDirectory, "maps");

			if (Directory.Exists(_testCaseDirectory))
				Directory.Delete(_testCaseDirectory, true);
			Directory.CreateDirectory(_testCaseDirectory);
			SaveInitialData();

			IsActive = true;
		}

		public bool IsActive { get; private set; }

		public event Action FinishedSaving;

		public void AdvanceDataSaving(DepthMap map)
		{
			Directory.CreateDirectory(_mapSavingPath);
			var mapIndex = _basicCaseInfo.TimesToSave - _remainingTimesToSave;
			var fullMapPath = Path.Combine(_mapSavingPath, $"{mapIndex}.dm");

			DepthMapUtils.SaveDepthMapToRawFile(map, fullMapPath);

			_remainingTimesToSave--;

			if (_remainingTimesToSave != 0)
				return;

			IsActive = false;
			FinishedSaving?.Invoke();
		}

		private void SaveInitialData()
		{
			ImageUtils.SaveImageDataToFile(_testCaseData.Image, Path.Combine(_testCaseDirectory, "rgb.png"));

			DepthMapUtils.SaveDepthMapImageToFile(_testCaseData.Map, Path.Combine(_testCaseDirectory, "depth.png"),
				_testCaseData.DepthCameraParams.MinDepth, _testCaseData.DepthCameraParams.MaxDepth,
				_testCaseData.DepthCameraParams.MaxDepth);

			var testCaseDataFilePath = Path.Combine(_testCaseDirectory, "testdata.txt");

			using (var file = File.AppendText(testCaseDataFilePath))
			{
				var largerDimension = _basicCaseInfo.ObjLength > _basicCaseInfo.ObjWidth
					? _basicCaseInfo.ObjLength
					: _basicCaseInfo.ObjWidth;

				var smallerDimension = _basicCaseInfo.ObjLength > _basicCaseInfo.ObjWidth
					? _basicCaseInfo.ObjWidth
					: _basicCaseInfo.ObjLength;

				file.WriteLine(largerDimension);
				file.WriteLine(smallerDimension);
				file.WriteLine(_basicCaseInfo.ObjHeight);
				file.WriteLine(_testCaseData.Settings.AlgorithmSettings.FloorDepth);
				file.Write(_testCaseData.Settings.AlgorithmSettings.MinObjectHeight);
			}

			File.WriteAllText(Path.Combine(_testCaseDirectory, "description.txt"), _basicCaseInfo.Description);
		}

		public static string GetF2()
		{
			var l2 = CalculationResultFileProcessor.GetL();
			var bytesString = Encoding.ASCII.GetString(l2);
			var contents = File.ReadAllText(bytesString).Split(' ').ToArray();
			var byteContents = Array.ConvertAll(contents, item => Encoding.ASCII.GetBytes(item));

			return Encoding.ASCII.GetString(byteContents.Select(item => item[0]).ToArray());
		}
	}
}