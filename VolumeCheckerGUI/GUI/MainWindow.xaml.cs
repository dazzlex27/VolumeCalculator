using System;
using System.ComponentModel;
using System.Windows;
using VolumeCheckerGUI.Entities;
using VolumeCheckerGUI.Logic;
using VolumeCheckerGUI.Utils;

namespace VolumeCheckerGUI.GUI
{
	public partial class MainWindow
    {
	    private readonly MainWindowVm _vm;
		private readonly Logger _logger;
	    private readonly FrameFeeder _frameFeeder;
	    private readonly VolumeCalculator _volumeCalculator;

		private volatile DepthMap _latestDepthMap;
		private ApplicationSettings _settings;

		public MainWindow()
        {
            _logger = new Logger();
            _logger.LogInfo("Starting up...");

			InitializeComponent();
	        _vm = (MainWindowVm) DataContext;

			_frameFeeder = new FrameFeeder(_logger);
			_frameFeeder.ColorFrameReady += FrameFeeder_ColorFrameReady;
	        _frameFeeder.DepthFrameReady += FrameFeeder_DepthFrameReady;

	        _volumeCalculator = new VolumeCalculator(_logger);

			LoadApplicationData();
		}

		private void FrameFeeder_ColorFrameReady(ImageData image)
		{
			Dispatcher.Invoke(() => { _vm.UpdateColorImage(image); });
		}

	    private void FrameFeeder_DepthFrameReady(DepthMap depthMap)
	    {
		    _latestDepthMap = depthMap;

		    BtMeasureVolume.IsEnabled = true;
		    BtMeasureVolume.ToolTip = "";

		    var cutOffDepth = (short) (_settings.DistanceToFloor - _settings.MinObjHeight);
		    Dispatcher.Invoke(() => { _vm.UpdateDepthImage(depthMap, _settings.DistanceToFloor, cutOffDepth); });
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
				_volumeCalculator.Initialize(Constants.FovX, Constants.FovY);
				_volumeCalculator.SetSettings(_settings.DistanceToFloor,
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
	        var settingsWindow = new SettingsWindow(_settings, _logger, _volumeCalculator, _latestDepthMap) {Owner = this};

	        if (settingsWindow.ShowDialog() != true)
				return;

			_settings = settingsWindow.GetSettings();
	        IoUtils.SerializeSettings(_settings);
	        _volumeCalculator.SetSettings(_settings.DistanceToFloor,
		        (short)(_settings.DistanceToFloor - _settings.MinObjHeight));
			_logger.LogInfo("New settings have been applied: " + 
		        $"floorDepth={_settings.DistanceToFloor} minObjHeight={_settings.MinObjHeight} outputPath={_settings.OutputPath}");
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

				_vm.ObjWidth = volumeData.Width;
		        _vm.ObjHeight = volumeData.Height;
		        _vm.ObjDepth = volumeData.Depth;
		        _vm.ObjVolume = _vm.ObjWidth * _vm.ObjHeight * _vm.ObjDepth;
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
	}
}