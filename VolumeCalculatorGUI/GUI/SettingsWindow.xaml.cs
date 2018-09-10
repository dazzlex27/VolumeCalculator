using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Common;
using DepthMapProcessorGUI.Entities;
using DepthMapProcessorGUI.Logic;

namespace DepthMapProcessorGUI.GUI
{
    internal partial class SettingsWindow
    {
	    private readonly SettingsWindowVm _vm;
	    private readonly DepthMapProcessor _volumeCalculator;
		private readonly DepthMap _lastReceivedDepthMap;
	    private readonly Logger _logger;
		
		public SettingsWindow(ApplicationSettings settings, Logger logger, DepthMapProcessor volumeCalculator,
			DepthMap lastReceivedDepthMap)
		{
			_logger = logger;
			_volumeCalculator = volumeCalculator;
			_lastReceivedDepthMap = lastReceivedDepthMap;

            InitializeComponent();

			_vm = (SettingsWindowVm) DataContext;

			var oldSettings = settings ?? ApplicationSettings.GetDefaultSettings();
			_vm.DistanceToFloor = oldSettings.DistanceToFloor;
			_vm.MinObjHeight = oldSettings.MinObjHeight;
			_vm.OutputPath = oldSettings.OutputPath;

			if (_lastReceivedDepthMap != null)
				return;

			BtCalculateFloorDepth.IsEnabled = false;
			BtCalculateFloorDepth.ToolTip = "Нет данных для вычисления";
		}

		public ApplicationSettings GetSettings()
		{
			return new ApplicationSettings(_vm.DistanceToFloor, _vm.MinObjHeight, _vm.OutputPath);
		}

		private void BtOk_Click(object sender, RoutedEventArgs e)
		{
			var validationPassed = IsValid(TbDistanceToFloor) && IsValid(TbMinObjHeight);
			if (!validationPassed)
				return;

			DialogResult = true;
			Close();
		}

		private void BtCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private static bool IsValid(DependencyObject obj)
		{
			return !Validation.GetHasError(obj) && LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>().
				       All(IsValid);
		}

	    private void BtCalculateFloorDepth_OnClick(object sender, RoutedEventArgs e)
	    {
		    try
		    {
			    var floorDepth = _volumeCalculator.CalculateFloorDepth(_lastReceivedDepthMap);
			    if (floorDepth <= 0)
				    throw new ArgumentException("Floor depth calculation: return a value less than zero");

			    _vm.DistanceToFloor = floorDepth;
			    _logger.LogInfo($"Caculated floor depth as {floorDepth}mm");
		    }
		    catch (Exception ex)
		    {
			    _logger.LogException("Failed to calculate floor depth!", ex);

				MessageBox.Show("Во время вычисления произошла ошибка, автоматический расчёт не был выполнен", "Ошибка", 
				    MessageBoxButton.OK, MessageBoxImage.Error);
		    }
	    }
    }
}