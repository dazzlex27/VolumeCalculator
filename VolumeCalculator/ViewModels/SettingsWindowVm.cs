using System.Windows;
using System.Windows.Input;
using FrameProcessor;
using FrameProviders;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace VolumeCalculator
{
	internal class SettingsWindowVm : BaseViewModel
	{
		private readonly ApplicationSettings _oldSettings;

		private WorkAreaSettingsControlVm _workAreaSettingsControlVm;
		private MiscSettingsControlVm _miscSettingsControlVm;

		public WorkAreaSettingsControlVm WorkAreaControlVm
		{
			get => _workAreaSettingsControlVm;
			set => SetField(ref _workAreaSettingsControlVm, value, nameof(WorkAreaControlVm));
		}
		
		public MiscSettingsControlVm MiscControlVm
		{
			get => _miscSettingsControlVm;
			set => SetField(ref _miscSettingsControlVm, value, nameof(MiscControlVm));
		}

		public ICommand ResetSettingsCommand { get; }
		
		public SettingsWindowVm(ILogger logger, ApplicationSettings settings, DepthCameraParams depthCameraParams,
			DepthMapProcessor depthMapProcessor)
		{
			_oldSettings = settings;

			WorkAreaControlVm = new WorkAreaSettingsControlVm(logger, depthMapProcessor, depthCameraParams);
			MiscControlVm = new MiscSettingsControlVm();
			
			ResetSettingsCommand = new CommandHandler(ResetSettings, true);

			FillValuesFromSettings(settings);
		}

		public ApplicationSettings GetSettings()
		{
			var newGeneralSettings = new GeneralSettings(MiscControlVm.OutputPath,
				_oldSettings.GeneralSettings.ShutDownPcByDefault);
			
			var oldIoSettings = _oldSettings.IoSettings;
			var newIoSettings = new IoSettings(oldIoSettings.ActiveCameraName, oldIoSettings.ActiveScales,
				oldIoSettings.ActiveScanners, oldIoSettings.ActiveIoCircuit, oldIoSettings.ActiveRangeMeterName, 
				oldIoSettings.IpCameraSettings);

			var newWorkAreaSettings = WorkAreaControlVm.GetSettings();
			var newAlgorithmSettings = new AlgorithmSettings(newWorkAreaSettings, MiscControlVm.SampleCount, 
				MiscControlVm.EnableAutoTimer, MiscControlVm.TimeToStartMeasurementMs, MiscControlVm.RequireBarcode, 
				MiscControlVm.SelectedWeightUnits, MiscControlVm.EnablePalletSubtraction, 
				MiscControlVm.PalletWeightKg * 1000, MiscControlVm.PalletHeightMm);

			return new ApplicationSettings(newGeneralSettings, newIoSettings, newAlgorithmSettings, _oldSettings.IntegrationSettings);
		}

		public void ColorFrameUpdated(ImageData image)
		{
			WorkAreaControlVm.SetColorFrame(image);
		}

		public void DepthFrameUpdated(DepthMap depthMap)
		{
			WorkAreaControlVm.SetDepthMap(depthMap);
		}

		private void ResetSettings()
		{
			if (MessageBox.Show("Сбросить настройки?", "Подтверждение", MessageBoxButton.YesNo,
					MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			FillValuesFromSettings(ApplicationSettings.GetDefaultSettings());
		}
		
		private void FillValuesFromSettings(ApplicationSettings settings)
		{
			WorkAreaControlVm.SetSettings(settings.AlgorithmSettings.WorkArea);
			MiscControlVm.FillValuesFromSettings(settings);
		}
	}
}