using Common;
using System;
using System.IO;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.Logic
{
    internal class TestDataGenerator
    {
		private readonly TestCaseData _testCaseData;
		private int _remainingTimesToSave;
	    private string _mapSavingPath;

		public bool IsActive { get; private set; }

		public event Action FinishedSaving;

        public TestDataGenerator(TestCaseData testCaseData)
        {
			_testCaseData = testCaseData;
			_remainingTimesToSave = _testCaseData.TimesToSave;

			Directory.CreateDirectory(_testCaseData.SavingDirectory);
			SaveInitialData();

			IsActive = true;
		}

        public void AdvanceDataSaving(DepthMap map)
        {
	        Directory.CreateDirectory(_mapSavingPath);	
			var mapIndex = _testCaseData.TimesToSave - _remainingTimesToSave;
			var fullMapPath = Path.Combine(_mapSavingPath, $"{mapIndex}.dm");

			DepthMapUtils.SaveDepthMapToFile(map, fullMapPath);

			_remainingTimesToSave--;

	        if (_remainingTimesToSave != 0)
		        return;

	        IsActive = false;
	        FinishedSaving?.Invoke();
        }

        private void SaveInitialData()
        {
	        if (Directory.Exists(_testCaseData.SavingDirectory))
		        Directory.Delete(_testCaseData.SavingDirectory, true);

	        Directory.CreateDirectory(_testCaseData.SavingDirectory);

            var bmp = IoUtils.CreateBitmapFromImageData(_testCaseData.Image);
            bmp.Save(Path.Combine(_testCaseData.SavingDirectory, "rgb.png"));

            var dmBmp = IoUtils.CreateBitmapFromDepthMap(_testCaseData.Map, _testCaseData.DeviceParams.MinDepth, 
				_testCaseData.DeviceParams.MaxDepth, _testCaseData.DeviceParams.MaxDepth);
            dmBmp.Save(Path.Combine(_testCaseData.SavingDirectory, "depth.png"));

	        var testCaseDataFilePath = Path.Combine(_testCaseData.SavingDirectory, "testdata.txt");
			if (File.Exists(testCaseDataFilePath))
				File.Delete(testCaseDataFilePath);

	        using (var file = File.AppendText(testCaseDataFilePath))
	        {
				file.WriteLine(_testCaseData.ObjWidth);
		        file.WriteLine(_testCaseData.ObjHeight);
		        file.WriteLine(_testCaseData.ObjDepth);
		        file.WriteLine(_testCaseData.DistanceToFloor);
		        file.Write(_testCaseData.MinObjHeight);
	        }

			File.WriteAllText(Path.Combine(_testCaseData.SavingDirectory, "description.txt"), _testCaseData.Description);

	        _mapSavingPath = Path.Combine(_testCaseData.SavingDirectory, "maps");

        }
    }
}