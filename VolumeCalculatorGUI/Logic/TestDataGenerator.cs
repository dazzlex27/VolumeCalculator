﻿using Common;
using System;
using System.IO;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic
{
    internal class TestDataGenerator
    {
		private readonly TestCaseData _testCaseData;
	    private readonly TestCaseBasicInfo _basicCaseInfo;
		private int _remainingTimesToSave;
	    private string _mapSavingPath;

		public bool IsActive { get; private set; }

		public event Action FinishedSaving;

        public TestDataGenerator(TestCaseData testCaseData)
        {
			_testCaseData = testCaseData;
	        _basicCaseInfo = _testCaseData.BasicInfo;

			_remainingTimesToSave = _basicCaseInfo.TimesToSave;

			Directory.CreateDirectory(_basicCaseInfo.SavingDirectory);
			SaveInitialData();

			IsActive = true;
		}

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
	        if (Directory.Exists(_basicCaseInfo.SavingDirectory))
		        Directory.Delete(_basicCaseInfo.SavingDirectory, true);

	        Directory.CreateDirectory(_basicCaseInfo.SavingDirectory);

	        ImageUtils.SaveImageDataToFile(_testCaseData.Image,
		        Path.Combine(_basicCaseInfo.SavingDirectory, "rgb.png"));

            DepthMapUtils.SaveDepthMapImageToFile(_testCaseData.Map, Path.Combine(_basicCaseInfo.SavingDirectory, "depth.png"),
	            _testCaseData.DeviceParams.MinDepth, _testCaseData.DeviceParams.MaxDepth, _testCaseData.DeviceParams.MaxDepth);

	        var testCaseDataFilePath = Path.Combine(_basicCaseInfo.SavingDirectory, "testdata.txt");
			if (File.Exists(testCaseDataFilePath))
				File.Delete(testCaseDataFilePath);

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
		        file.WriteLine(_testCaseData.Settings.DistanceToFloor);
		        file.Write(_testCaseData.Settings.MinObjHeight);
	        }

			File.WriteAllText(Path.Combine(_basicCaseInfo.SavingDirectory, "description.txt"), _basicCaseInfo.Description);

	        _mapSavingPath = Path.Combine(_basicCaseInfo.SavingDirectory, "maps");

        }
    }
}