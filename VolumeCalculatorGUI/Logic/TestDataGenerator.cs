using Common;
using DepthMapProcessorGUI.Utils;
using FrameSources;
using System.IO;

namespace VolumeCalculatorGUI.Logic
{
    internal class TestDataGenerator
    {
        private readonly string _directory;
        private readonly string _casename;
        private readonly int _timesToSave;
        private int _remainingTimesToSave;

        public TestDataGenerator(string directory, string casename, int timesToSave, ImageData image, DepthMap map, DeviceParams deviceParams)
        {
            _directory = directory;
            _casename = casename;
            _timesToSave = timesToSave;

            _remainingTimesToSave = _timesToSave;

            SaveInitialData(image, map, deviceParams);
        }

        public void AdvanceDataSaving(DepthMap map)
        {
            
        }

        private void SaveInitialData(ImageData image, DepthMap map, DeviceParams deviceParams)
        {
            var bmp = IoUtils.CreateBitmapFromImageData(image);
            bmp.Save(Path.Combine(_directory, "rgb.png"));

            var dmBmp = IoUtils.CreateBitmapFromDepthMap(map, deviceParams.MinDepth, deviceParams.MaxDepth, deviceParams.MaxDepth);
            dmBmp.Save(Path.Combine(_directory, "depth.png"));
        }
    }
}
