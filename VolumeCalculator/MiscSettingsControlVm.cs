using GuiCommon;
using Primitives;
using Primitives.Settings;

namespace VolumeCalculator
{
	internal class MiscSettingsControlVm : BaseViewModel
	{
		private byte _sampleCount;
		private bool _requireBarcode;
		private string _outputPath;

		private bool _enableAutoTimer;
		private long _timeToStartMeasurementMs;
		private int _rangeMeterSubtractionValue;
		private bool _rangeMeterAvailable;
		private WeightUnits _selectedWeightUnits;
		private bool _enablePalletSubtraction;
		private double _palletWeightKg;
		private int _palletHeightMm;

		public byte SampleCount
		{
			get => _sampleCount;
			set => SetField(ref _sampleCount, value, nameof(SampleCount));
		}

		public bool RequireBarcode
		{
			get => _requireBarcode;
			set => SetField(ref _requireBarcode, value, nameof(RequireBarcode));
		}

		public string OutputPath
		{
			get => _outputPath;
			set => SetField(ref _outputPath, value, nameof(OutputPath));
		}

		public long TimeToStartMeasurementMs
		{
			get => _timeToStartMeasurementMs;
			set => SetField(ref _timeToStartMeasurementMs, value, nameof(TimeToStartMeasurementMs));
		}

		public bool EnableAutoTimer
		{
			get => _enableAutoTimer;
			set => SetField(ref _enableAutoTimer, value, nameof(EnableAutoTimer));
		}

		public WeightUnits SelectedWeightUnits
		{
			get => _selectedWeightUnits;
			set => SetField(ref _selectedWeightUnits, value, nameof(SelectedWeightUnits));
		}

		public int RangeMeterSubtractionValue
		{
			get => _rangeMeterSubtractionValue;
			set => SetField(ref _rangeMeterSubtractionValue, value, nameof(RangeMeterSubtractionValue));
		}

		public bool RangeMeterAvailable
		{
			get => _rangeMeterAvailable;
			set => SetField(ref _rangeMeterAvailable, value, nameof(RangeMeterAvailable));
		}
		
		public bool EnablePalletSubtraction
		{
			get => _enablePalletSubtraction;
			set => SetField(ref _enablePalletSubtraction, value, nameof(EnablePalletSubtraction));
		}
		
		public double PalletWeightKg
		{
			get => _palletWeightKg;
			set => SetField(ref _palletWeightKg, value, nameof(PalletWeightKg));
		}
		
		public int PalletHeightMm
		{
			get => _palletHeightMm;
			set => SetField(ref _palletHeightMm, value, nameof(PalletHeightMm));
		}

		public void FillValuesFromSettings(ApplicationSettings settings)
		{
			OutputPath = settings.GeneralSettings.OutputPath;
			SampleCount = settings.AlgorithmSettings.SampleDepthMapCount;
			EnableAutoTimer = settings.AlgorithmSettings.EnableAutoTimer;
			TimeToStartMeasurementMs = settings.AlgorithmSettings.TimeToStartMeasurementMs;
			RequireBarcode = settings.AlgorithmSettings.RequireBarcode;
			RangeMeterAvailable = settings.IoSettings.ActiveRangeMeterName != "";
			RangeMeterSubtractionValue = settings.IoSettings.RangeMeterSubtractionValueMm;
			SelectedWeightUnits = settings.AlgorithmSettings.SelectedWeightUnits;
			EnablePalletSubtraction = settings.AlgorithmSettings.EnablePalletSubtraction;
			PalletWeightKg = settings.AlgorithmSettings.PalletWeightGr / 1000;
			PalletHeightMm = settings.AlgorithmSettings.PalletHeightMm;
		}
	}
}