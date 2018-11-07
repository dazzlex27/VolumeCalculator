using System;
using System.IO;
using Primitives;
using VolumeCalculatorGUI.Entities;

namespace VolumeCalculatorGUI.Logic
{
    internal class TestDataGenerator
    {
		private readonly TestCaseData _testCaseData;
	    private readonly TestCaseBasicInfo _basicCaseInfo;
	    private readonly string _testCaseDirectory;
	    private readonly string _mapSavingPath;
		private int _remainingTimesToSave;

		public bool IsActive { get; private set; }

		public event Action FinishedSaving;

        public TestDataGenerator(TestCaseData testCaseData)
        {
			_testCaseData = testCaseData;
	        _basicCaseInfo = _testCaseData.BasicInfo;

			_remainingTimesToSave = _basicCaseInfo.TimesToSave;

	        _testCaseDirectory = Path.Combine(_testCaseData.BasicInfo.SavingDirectory, _testCaseData.BasicInfo.Casename);
	        _mapSavingPath = Path.Combine(_testCaseDirectory, "maps");

	        if (Directory.Exists(_testCaseDirectory))
		        Directory.Delete(_testCaseDirectory, true);
	        Directory.CreateDirectory(_testCaseDirectory);
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
	        ImageUtils.SaveImageDataToFile(_testCaseData.Image, Path.Combine(_testCaseDirectory, "rgb.png"));

            DepthMapUtils.SaveDepthMapImageToFile(_testCaseData.Map, Path.Combine(_testCaseDirectory, "depth.png"),
	            _testCaseData.DepthCameraParams.MinDepth, _testCaseData.DepthCameraParams.MaxDepth, _testCaseData.DepthCameraParams.MaxDepth);

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
		        file.WriteLine(_testCaseData.Settings.FloorDepth);
		        file.Write(_testCaseData.Settings.MinObjectHeight);
	        }

			File.WriteAllText(Path.Combine(_testCaseDirectory, "description.txt"), _basicCaseInfo.Description);
        }
    }
}