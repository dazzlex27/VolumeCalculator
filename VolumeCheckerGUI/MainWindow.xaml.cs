using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using VolumeCheckerGUI.Logic;
using VolumeCheckerGUI.Structures;

namespace VolumeCheckerGUI
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly Logger _logger;
		private ApplicationSettings _settings;
	    private readonly FrameFeeder _frameFeeder;
	    private readonly VolumeCalculator _volumeCalculator;

		private volatile DepthMap _latestDepthMap;
	    private short _objWidth;
	    private short _objHeight;
	    private short _objDepth;
	    private long _objVolume;

	    private Bitmap _depthImage;

	    public Bitmap LatestDepthImage
	    {
		    get => _depthImage;
		    set
		    {
			    _depthImage = value;
			    OnPropertyChanged();
		    }
	    }

	    public short ObjWidth
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

	    public short ObjHeight
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

	    public short ObjDepth
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
	        _frameFeeder.DepthFrameReady += FrameFeeder_DepthFrameReady;

	        _volumeCalculator = new VolumeCalculator(_logger);

			LoadApplicationData();
		}

		private void FrameFeeder_DepthFrameReady(DepthMap depthMap)
		{
			_latestDepthMap = depthMap;

			var cutOffDepth = (short) (_settings.DistanceToFloor - _settings.MinObjHeight);
			LatestDepthImage = DepthMapUtils.GetBitmapFromDepthMap(_latestDepthMap, Constants.MinDepth,
				_settings.DistanceToFloor, cutOffDepth);
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

				unsafe
		        {
			        fixed (short* data = _latestDepthMap.Data)
			        {
				        var res = DllWrapper.CheckVolume(_latestDepthMap.Width, _latestDepthMap.Height, data);
				        if (res == null)
				        {
					        MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
						        MessageBoxButton.OK, MessageBoxImage.Error);
							_logger.LogError("Volume check returned null");
							return;
				        }

				        ObjWidth = res->Width;
				        ObjHeight = res->Height;
				        ObjDepth = res->Depth;
				        ObjVolume = ObjWidth * ObjHeight * ObjDepth;

				        _latestDepthMap = null;
			        }
		        }

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