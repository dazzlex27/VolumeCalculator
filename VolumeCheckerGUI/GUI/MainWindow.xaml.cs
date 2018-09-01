using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VolumeCheckerGUI.Entities;
using VolumeCheckerGUI.Logic;
using VolumeCheckerGUI.Utils;

namespace VolumeCheckerGUI.GUI
{
    public partial class MainWindow : INotifyPropertyChanged
    {
	    private static readonly int BytesPerPixel24 = (PixelFormats.Bgr24.BitsPerPixel + 7) / 8;

		private readonly Logger _logger;
		private ApplicationSettings _settings;
	    private readonly FrameFeeder _frameFeeder;
	    private readonly VolumeCalculator _volumeCalculator;
	    private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		private volatile DepthMap _latestDepthMap;
	    private int _objWidth;
	    private int _objHeight;
	    private int _objDepth;
	    private long _objVolume;

	    public int ObjWidth
	    {
		    get => _objWidth;
		    set
		    {
			    if (_objWidth == value)
				    return;

			    _objWidth = value;
			    OnPropertyChanged();
		    }
	    }

	    public int ObjHeight
	    {
		    get => _objHeight;
		    set
		    {
			    if (_objHeight == value)
				    return;

			    _objHeight = value;
			    OnPropertyChanged();
		    }
	    }

	    public int ObjDepth
		{
		    get => _objDepth;
		    set
		    {
			    if (_objDepth == value)
				    return;

			    _objDepth = value;
			    OnPropertyChanged();
		    }
	    }

	    public long ObjVolume
	    {
		    get => _objVolume;
		    set
		    {
			    if (_objVolume == value)
				    return;

			    _objVolume = value;
			    OnPropertyChanged();
		    }
	    }

		public MainWindow()
        {
            _logger = new Logger();
            _logger.LogInfo("Starting up...");

			InitializeComponent();

	        _frameFeeder = new FrameFeeder(_logger);
			_frameFeeder.ColorFrameReady += FrameFeeder_ColorFrameReady;
	        _frameFeeder.DepthFrameReady += FrameFeeder_DepthFrameReady;

	        _volumeCalculator = new VolumeCalculator(_logger);

			LoadApplicationData();
		}

		private void FrameFeeder_ColorFrameReady(ImageData image)
		{
			Dispatcher.Invoke(() =>
			{
				var imageWidth = image.Width;
				var imageHeight = image.Height;

				_colorImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);

				ImgColor.Source = _colorImageBitmap;

				var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);
				_colorImageBitmap.WritePixels(fullRect, image.Data, imageWidth * BytesPerPixel24, 0);
			});
		}

		private void FrameFeeder_DepthFrameReady(DepthMap depthMap)
		{
			_latestDepthMap = depthMap;

			var cutOffDepth = (short) (_settings.DistanceToFloor - _settings.MinObjHeight);
			var depthMapData = DepthMapUtils.GetBitmapFromDepthMap(_latestDepthMap, Constants.MinDepth,
				_settings.DistanceToFloor, cutOffDepth);

			Dispatcher.Invoke(() =>
			{
				var imageWidth = depthMap.Width;
				var imageHeight = depthMap.Height;

				_depthImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);

				ImgDm.Source = _depthImageBitmap;

				var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);
				_depthImageBitmap.WritePixels(fullRect, depthMapData, imageWidth * BytesPerPixel24, 0);
			});
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
                e.Cancel = true;

			ExitApplication();
        }

        private void MiExitApp_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MiOpenSettings_Click(object sender, RoutedEventArgs e)
        {
	        var settingsWindow = new SettingsWindow(_settings) {Owner = this};

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

		        IoUtils.SaveWriteableBitmap("out/color.png", _colorImageBitmap);
		        IoUtils.SaveWriteableBitmap("out/depth.png", _depthImageBitmap);

				var volumeData = _volumeCalculator.CalculateVolume(_latestDepthMap);
		        _latestDepthMap = null;

		        if (volumeData == null)
		        {
			        MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
				        MessageBoxButton.OK, MessageBoxImage.Error);
			        _logger.LogError("Volume check returned null");

			        ObjWidth = ObjHeight = ObjDepth = 0;
			        ObjVolume = 0;

			        return;
		        }

		        ObjWidth = volumeData.Width;
		        ObjHeight = volumeData.Height;
		        ObjDepth = volumeData.Depth;
		        ObjVolume = ObjWidth * ObjHeight * ObjDepth;

		        _logger.LogInfo($"Completed a volume check, W={ObjWidth} H={ObjHeight} D={ObjDepth} V={ObjVolume}");
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

	    public event PropertyChangedEventHandler PropertyChanged;

	    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	    {
		    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }
	}
}