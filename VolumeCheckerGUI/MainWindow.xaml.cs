using System;
using System.Windows;

namespace VolumeCheckerGUI
{
    public partial class MainWindow : Window
    {
        private readonly Logger _logger;
		private CheckerSettings _settings;

        public MainWindow()
        {
            _logger = new Logger();
            _logger.LogInfo("Starting up...");

            InitializeComponent();

			LoadApplicationData();
        }

		private void LoadApplicationData()
		{
			_logger.LogInfo("Trying to read settings from file...");
			var settingsFromFile = IOUtils.DeserializeSettings();
			if (settingsFromFile == null)
			{
				_logger.LogInfo("Failed to read settings from file, will use default settings");
				_settings = CheckerSettings.GetDefaultSettings();
			}
			else
				_settings = settingsFromFile;

			try
			{
				_logger.LogInfo("Creating volume checker...");
				var mapWidth = 800;
				var mapHeight = 480;
				var floorDepth = 2000;
				var cutOffDepth = 1900;
				var test = DllWrapper.CreateVolumeChecker(86, 57, mapWidth, mapHeight, floorDepth, cutOffDepth);
				if (test == 0)
					_logger.LogInfo("Successfully created volume checker...");
				else
					_logger.LogError("Failed to create volume checker!");
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
				_logger.LogInfo("Saving settings...");
				IOUtils.SerializeSettings(_settings);

				int destructionResult = DllWrapper.DestroyVolumeChecker();

				if (destructionResult == 0)
					_logger.LogInfo("Successfully disposed detection lib");
				else if (destructionResult == 1)
					_logger.LogInfo("Detection lib was previously disposed");

				_logger.LogInfo("Shutting down...");
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to dispose detection lib", ex);
			}
		}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
			var settingsWindow = new SettingsWindow(_settings);
			if (settingsWindow.ShowDialog() != true)
				return;

			_settings = settingsWindow.GetSettings();
        }

        private void BtMeasureVolume_Click(object sender, RoutedEventArgs e)
        {
			unsafe
			{
				var data = new short[2];
				fixed(short* d = data)
				{
					var res = DllWrapper.CheckVolume(d);
					MessageBox.Show($"{res->Width} {res->Height} {res->Depth}");
				}
			}
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            _logger.LogInfo("Finished loading the main window");
        }
    }
}