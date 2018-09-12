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
			var mapIndex = _testCaseData.TimesToSave - _remainingTimesToSave;
			var fullMapPath = Path.Combine(_testCaseData.SavingDirectory, $"{mapIndex}.dm");

			DepthMapUtils.SaveDepthMapToFile(map, fullMapPath);

			_remainingTimesToSave--;

			if (_remainingTimesToSave == 0)
			{
				IsActive = false;
				FinishedSaving?.Invoke();
			}
		}

        private void SaveInitialData()
        {
            var bmp = IoUtils.CreateBitmapFromImageData(_testCaseData.Image);
            bmp.Save(Path.Combine(_testCaseData.SavingDirectory, "rgb.png"));

            var dmBmp = IoUtils.CreateBitmapFromDepthMap(_testCaseData.Map, _testCaseData.DeviceParams.MinDepth, 
				_testCaseData.DeviceParams.MaxDepth, _testCaseData.DeviceParams.MaxDepth);
            dmBmp.Save(Path.Combine(_testCaseData.SavingDirectory, "depth.png"));

            File.WriteAllText(Path.Combine(_testCaseData.SavingDirectory, "floor.txt"), _testCaseData.DistanceToFloor.ToString());
        }
    }
}