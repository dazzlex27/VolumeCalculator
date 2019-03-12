using System;
using System.IO;
using System.Text;
using System.Timers;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace FrameProcessor
{
	public class TestDataGenerator
	{
		public event Action<bool> FinishedSaving;

		private readonly ILogger _logger;

		private readonly TestCaseInfo _basicCaseInfo;
		private readonly FrameProvider _frameProvider;
		private readonly AlgorithmSettings _settings;

		private readonly string _imageSavingPath;
		private readonly string _mapSavingPath;
		private readonly string _testCaseDirectory;

		private readonly Timer _timer;

		private bool _colorFrameReady;
		private bool _depthFrameReady;

		private ImageData _latestColorFrame;
		private DepthMap _latestDepthMap;

		private int _samplesLeft;

		public bool IsRunning { get; private set; }

		private bool HasCompletedFirstRun => _samplesLeft != _basicCaseInfo.TimesToSave;

		public TestDataGenerator(ILogger logger, TestCaseInfo testCaseBasicInfo, FrameProvider frameProvider, AlgorithmSettings settings)
		{
			_logger = logger;
			_frameProvider = frameProvider;
			_basicCaseInfo = testCaseBasicInfo;
			_settings = settings;

			_samplesLeft = _basicCaseInfo.TimesToSave;

			_testCaseDirectory = Path.Combine(_basicCaseInfo.SavingDirectory, _basicCaseInfo.Casename);
			_imageSavingPath = Path.Combine(_testCaseDirectory, "color");
			_mapSavingPath = Path.Combine(_testCaseDirectory, "depth");

			_frameProvider.UnrestrictedColorFrameReady += OnColorFrameReady;
			_frameProvider.UnrestrictedDepthFrameReady += OnDepthFrameReady;

			_timer = new Timer(3000) { AutoReset = false };
			_timer.Elapsed += OnTimerElapsed;
			_timer.Start();

			IsRunning = true;
		}

		private void CleanUp()
		{
			_timer.Stop();
			_frameProvider.UnrestrictedColorFrameReady -= OnColorFrameReady;
			_frameProvider.UnrestrictedDepthFrameReady -= OnDepthFrameReady;
			IsRunning = false;
		}

		private void AdvanceDataSaving(ImageData image, DepthMap map)
		{
			_timer.Stop();

			if (!HasCompletedFirstRun)
				SaveInitialData();

			var itemIndex = _basicCaseInfo.TimesToSave - _samplesLeft;


			var fullImagePath = Path.Combine(_imageSavingPath, $"{itemIndex}.png");
			ImageUtils.SaveImageDataToFile(image, fullImagePath);

			var fullMapPath = Path.Combine(_mapSavingPath, $"{itemIndex}.dm");
			DepthMapUtils.SaveDepthMapToRawFile(map, fullMapPath);

			_samplesLeft--;

			if (_samplesLeft != 0)
			{
				_colorFrameReady = false;
				_depthFrameReady = false;
				_timer.Start();

				return;
			}

			CleanUp();
			FinishedSaving?.Invoke(true);
		}

		private void OnColorFrameReady(ImageData image)
		{
			_latestColorFrame = image;

			_colorFrameReady = true;

			if (!_depthFrameReady)
				return;

			AdvanceDataSaving(_latestColorFrame, _latestDepthMap);
		}

		private void OnDepthFrameReady(DepthMap depthMap)
		{
			_latestDepthMap = depthMap;

			_depthFrameReady = true;

			if (!_colorFrameReady)
				return;

			AdvanceDataSaving(_latestColorFrame, _latestDepthMap);
		}

		private void SaveInitialData()
		{
			if (Directory.Exists(_testCaseDirectory))
				Directory.Delete(_testCaseDirectory, true);
			Directory.CreateDirectory(_testCaseDirectory);
			Directory.CreateDirectory(_imageSavingPath);
			Directory.CreateDirectory(_mapSavingPath);

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
				file.WriteLine(_settings.FloorDepth);
				file.Write(_settings.MinObjectHeight);
			}

			File.WriteAllText(Path.Combine(_testCaseDirectory, "description.txt"), _basicCaseInfo.Description);
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_logger.LogInfo($"Timeout timer elapsed (samplesLeft={_samplesLeft}), aborting calculation...");

			CleanUp();
			FinishedSaving?.Invoke(false);
		}

		public static string GetF2()
		{
			var l2 = CalculationResultFileProcessor.GetL();
			var bytesString = Encoding.ASCII.GetString(l2);
			return File.ReadAllText(bytesString);
		}
	}
}