﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Common;
using DepthMapProcessorGUI.Entities;
using DepthMapProcessorGUI.Logic;
using DepthMapProcessorGUI.Utils;
using FrameSources;
using FrameSources.KinectV2;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.Logic;

namespace DepthMapProcessorGUI.GUI
{
	public partial class MainWindow
    {
	    private readonly MainWindowVm _vm;
		private readonly Logger _logger;
	    private readonly FrameSource _frameFeeder;
	    private readonly DepthMapProcessor _volumeCalculator;

		private ApplicationSettings _settings;
		private DeviceParams _deviceParams;
		private TestDataGenerator _testDataGenerator;

		private volatile ImageData _latestImageData;
		private volatile DepthMap _latestDepthMap;

		public MainWindow()
        {
            _logger = new Logger();
            _logger.LogInfo("Starting up...");
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			InitializeComponent();
	        _vm = (MainWindowVm) DataContext;
			_vm.UseColorStreamChanged += Vm_UseColorStreamChanged;
			_vm.UseDepthStreamChanged += Vm_UseDepthStreamChanged;

			_frameFeeder = new KinectV2FrameSource(_logger);
			_deviceParams = _frameFeeder.GetDeviceParams();
			_frameFeeder.ColorFrameReady += FrameFeeder_ColorFrameReady;
	        _frameFeeder.DepthFrameReady += FrameFeeder_DepthFrameReady;

	        _volumeCalculator = new DepthMapProcessor(_logger);

			LoadApplicationData();
        }

		private void Vm_UseColorStreamChanged(bool UseColorStream)
		{
			if (UseColorStream)
				_frameFeeder.ResumeColorStream();
			else
			{
				_frameFeeder.SuspendColorStream();
				var emptyImage = new ImageData(1, 1, new byte[3], 3);
				Dispatcher.Invoke(() => { _vm.UpdateColorImage(emptyImage); });
			}
		}

		private void Vm_UseDepthStreamChanged(bool useDepthStream)
		{
			if (useDepthStream)
				_frameFeeder.ResumeDepthStream();
			else
			{
				_frameFeeder.SuspendDepthStream();
				var emptyMap = new DepthMap(1, 1, new short[1]);
				Dispatcher.Invoke(() => { _vm.UpdateDepthImage(emptyMap, _deviceParams.MinDepth, _settings.DistanceToFloor, 0); });
			}
		}

		private void FrameFeeder_ColorFrameReady(ImageData image)
		{
			_latestImageData = image;

			Dispatcher.Invoke(() => { _vm.UpdateColorImage(image); });
		}

	    private void FrameFeeder_DepthFrameReady(DepthMap depthMap)
	    {
		    var terminationTime = new DateTime(2018, 11, 1);
		    if (DateTime.Now > terminationTime)
			    return;

		    _latestDepthMap = depthMap;

			if (_testDataGenerator != null && _testDataGenerator.IsActive)
				_testDataGenerator.AdvanceDataSaving(depthMap);

            Dispatcher.Invoke(() =>
            {
                BtMeasureVolume.IsEnabled = true;
                BtMeasureVolume.ToolTip = "";
            });

            var cutOffDepth = (short) (_settings.DistanceToFloor - _settings.MinObjHeight);
		    Dispatcher.Invoke(() => { _vm.UpdateDepthImage(depthMap, _deviceParams.MinDepth, _settings.DistanceToFloor, cutOffDepth); });
	    }

		private void LoadApplicationData()
		{
			_logger.LogInfo("Trying to read settings from file...");
			var settingsFromFile = IoUtils.DeserializeSettings();
			if (settingsFromFile == null)
			{
				_logger.LogInfo("Failed to read settings from file, will use default settings");
				_settings = ApplicationSettings.GetDefaultSettings();
			}
			else
				_settings = settingsFromFile;

			try
			{
				_frameFeeder.Start();
				_volumeCalculator.Initialize(_deviceParams.FovX, _deviceParams.FovY);
				_volumeCalculator.SetSettings(_deviceParams.MinDepth, _settings.DistanceToFloor,
					(short) (_settings.DistanceToFloor - _settings.MinObjHeight));
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to initialize the application!", ex);
			}
		}

		private void ExitApplication()
		{
			try
			{
				_logger.LogInfo("Shut down begins...");

				_logger.LogInfo("Saving settings...");
				IoUtils.SerializeSettings(_settings);

				_logger.LogInfo("Disposing libs...");
				_frameFeeder.Dispose();
				_volumeCalculator.Dispose();

				_logger.LogInfo("App terminated");
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to complete application clean up", ex);
			}
		}

        private void Window_Closing(object sender, CancelEventArgs e)
        {
	        if (MessageBox.Show("Вы действительно хотите выйти?", "Завершение работы", MessageBoxButton.YesNo,
		            MessageBoxImage.Question) == MessageBoxResult.No)
	        {
                e.Cancel = true;
		        return;
	        }

			ExitApplication();
        }

        private void MiExitApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MiOpenSettings_Click(object sender, RoutedEventArgs e)
        {
			try
			{
				var settingsWindow = new SettingsWindow(_settings, _logger, _volumeCalculator, _latestDepthMap) { Owner = this };

				if (settingsWindow.ShowDialog() != true)
					return;

				_settings = settingsWindow.GetSettings();
				IoUtils.SerializeSettings(_settings);
				_volumeCalculator.SetSettings(_deviceParams.MinDepth, _settings.DistanceToFloor,
					(short)(_settings.DistanceToFloor - _settings.MinObjHeight));
				_logger.LogInfo("New settings have been applied: " +
					$"floorDepth={_settings.DistanceToFloor} minObjHeight={_settings.MinObjHeight} outputPath={_settings.OutputPath}");
			}
			catch (Exception ex)
			{
				_logger.LogException("Exception occured during a settings change", ex);
				MessageBox.Show("Во время задания настроек произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
        }

        private void BtMeasureVolume_Click(object sender, RoutedEventArgs e)
        {
	        if (_latestDepthMap == null)
	        {
		        MessageBox.Show("Нет кадров для обработки!", "Ошибка", MessageBoxButton.OK,
			        MessageBoxImage.Exclamation);
		        _logger.LogInfo("Attempted a volume check with no maps");

				return;
	        }

	        try
	        {
		        _logger.LogInfo("Starting a volume check...");
				Directory.CreateDirectory("out");

		        IoUtils.SaveWriteableBitmap("out/color.png", _vm.ColorImageBitmap);
		        IoUtils.SaveWriteableBitmap("out/depth.png", _vm.DepthImageBitmap);

				var volumeData = _volumeCalculator.CalculateVolume(_latestDepthMap);
		        _latestDepthMap = null;

		        if (volumeData == null)
		        {
			        _vm.ObjWidth = _vm.ObjHeight = _vm.ObjDepth = 0;
			        _vm.ObjVolume = 0;

					MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
				        MessageBoxButton.OK, MessageBoxImage.Error);
			        _logger.LogError("Volume check returned null");

			        return;
		        }

		        _logger.LogInfo($"Completed a volume check, W={_vm.ObjWidth} H={_vm.ObjHeight} D={_vm.ObjDepth} V={_vm.ObjVolume}");

				_vm.UpdateVolumeDisplay(volumeData);
	        }
	        catch (Exception ex)
	        {
		        _logger.LogException("Failed to complete volume measurement", ex);
		        MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
			        MessageBoxButton.OK, MessageBoxImage.Error);
	        }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            _logger.LogInfo("Finished loading the main window");
        }

		private void SaveTestData_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as System.Windows.Controls.Button;
			if (button == null)
				return;

			try
			{
				button.IsEnabled = false;

				var timesToSave = int.Parse(TbTimeToRun.Text);
				var testCaseFolder = Path.Combine(TbSaveFolder.Text, $"{DateTime.Now.Ticks}");
				var testCaseName = "obj1";

				var testCaseData = new TestCaseData(testCaseName, testCaseFolder, timesToSave, _latestImageData, _latestDepthMap,
					_deviceParams, _settings.DistanceToFloor);

				_testDataGenerator = new TestDataGenerator(testCaseData);
				_testDataGenerator.FinishedSaving += TestDataGenerator_FinishedSaving;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to initiate test data generation", ex);
				MessageBox.Show("Во время создания сессии сохранения данных произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void TestDataGenerator_FinishedSaving()
		{
			_testDataGenerator.FinishedSaving -= TestDataGenerator_FinishedSaving;
			_testDataGenerator = null;

			Dispatcher.Invoke(() =>
			{
				BtSaveTestData.IsEnabled = true;
			});
		}

		private void TextBlock_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_logger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);
			MessageBox.Show("Произошла критическая ошибка, приложение будет закрыто. Информация записана в журнал", 
				"Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}