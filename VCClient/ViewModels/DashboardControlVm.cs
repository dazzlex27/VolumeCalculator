using System;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegration.Scales;
using GuiCommon;
using Primitives;
using Primitives.Settings;
using VCClient.Utils;
using Primitives.Calculation;
using GuiCommon.Localization;

namespace VCClient.ViewModels
{
	internal class DashboardControlVm : BaseViewModel, IDisposable
	{
		public event Action<bool> LockingStatusChanged;
		public event Action<CalculationRequestData> CalculationRequested;
		public event Action CalculationCancellationRequested;
		public event Action WeightResetRequested;
		public event Action ResultFileOpeningRequested;
		public event Action PhotosFolderOpeningRequested;

		private readonly Timer _barcodeResetTimer;

		private bool _requireBarcode;
		private WeightUnits _selectedWeightUnits;
		
		private string _objectCode;
		private double _objectWeight;
		private uint _unitCount;
		private int _objectWidth;
		private int _objectHeight;
		private int _objectLength;
		private double _objectVolume;
		private string _comment;
		private bool _calculationInProgress;
		private SolidColorBrush _statusBrush;
		private string _statusText;
		private bool _calculationPending;

		private string _weightLabelText;

		private bool _codeBoxFocused;
		private bool _unitCountBoxFocused;
		private bool _commentBoxFocused;

		private string _lastAlgorithmUsed;

		private bool _debugMode;
		
		public ICommand RunVolumeCalculationCommand { get; }

		public ICommand ResetWeightCommand { get; }

		public ICommand OpenResultsFileCommand { get; }

		public ICommand OpenPhotosFolderCommand { get; }

		public ICommand CancelPendingCalculationCommand { get; }

		public string ObjectCode
		{
			get => _objectCode;
			set
			{
				SetField(ref _objectCode, value, nameof(ObjectCode));

				if (!string.IsNullOrEmpty(_objectCode))
					_barcodeResetTimer.Start();

				if (CodeReady)
					ResetMeasurementValues();
			}
		}

		public double ObjectWeight
		{
			get => _objectWeight;
			set
			{
				if (Math.Abs(_objectWeight - value) < 0.001)
					return;

				_objectWeight = value;

				OnPropertyChanged();
			}
		}

		public uint UnitCount
		{
			get => _unitCount;
			set => SetField(ref _unitCount, value, nameof(UnitCount));
		}

		public int ObjectLength
		{
			get => _objectLength;
			set => SetField(ref _objectLength, value, nameof(ObjectLength));
		}

		public int ObjectWidth
		{
			get => _objectWidth;
			set => SetField(ref _objectWidth, value, nameof(ObjectWidth));
		}

		public int ObjectHeight
		{
			get => _objectHeight;
			set => SetField(ref _objectHeight, value, nameof(ObjectHeight));
		}

		public double ObjectVolume
		{
			get => _objectVolume;
			set => SetField(ref _objectVolume, value, nameof(ObjectVolume));
		}

		public string Comment
		{
			get => _comment;
			set => SetField(ref _comment, value, nameof(Comment));
		}

		public bool CalculationInProgress
		{
			get => _calculationInProgress;
			set => SetField(ref _calculationInProgress, value, nameof(CalculationInProgress));
		}

		public SolidColorBrush StatusBrush
		{
			get => _statusBrush;
			set => SetField(ref _statusBrush, value, nameof(StatusBrush));
		}

		public string StatusText
		{
			get => _statusText;
			set { SetField(ref _statusText, value, nameof(StatusText)); }
		}

		public bool CodeBoxFocused
		{
			get => _codeBoxFocused;
			set
			{
				SetField(ref _codeBoxFocused, value, nameof(CodeBoxFocused));
				UpdateLockingStatus();
			}
		}

		public bool UnitCountBoxFocused
		{
			get => _unitCountBoxFocused;
			set
			{
				SetField(ref _unitCountBoxFocused, value, nameof(UnitCountBoxFocused));
				UpdateLockingStatus();
			}
		}

		public bool CommentBoxFocused
		{
			get => _commentBoxFocused;
			set
			{
				SetField(ref _commentBoxFocused, value, nameof(CommentBoxFocused));
				UpdateLockingStatus();
			}
		}

		public bool CalculationPending
		{
			get => _calculationPending;
			set => SetField(ref _calculationPending, value, nameof(CalculationPending));
		}

		public string WeightLabelText
		{
			get => _weightLabelText;
			set => SetField(ref _weightLabelText, value, nameof(WeightLabelText));
		}

		public bool CanAcceptBarcodes
		{
			get
			{
				var usingManualInput = CodeBoxFocused || CommentBoxFocused || UnitCountBoxFocused;
				return !usingManualInput && !CalculationInProgress;
			}
		}

		public bool CodeReady
		{
			get
			{
				if (_requireBarcode)
					return !string.IsNullOrEmpty(ObjectCode);

				return true;
			}
		}
		
		public string LastAlgorithmUsed
		{
			get => _lastAlgorithmUsed;
			set => SetField(ref _lastAlgorithmUsed, value, nameof(LastAlgorithmUsed));
		}

		public bool DebugMode
		{
			get => _debugMode;
			set => SetField(ref _debugMode, value, nameof(DebugMode));
		}

		public DashboardControlVm()
		{
			RunVolumeCalculationCommand = new CommandHandler(RunVolumeCalculation, !CalculationInProgress);
			ResetWeightCommand = new CommandHandler(() => WeightResetRequested?.Invoke(), !CalculationInProgress);
			OpenResultsFileCommand = new CommandHandler(() => ResultFileOpeningRequested?.Invoke(), !CalculationInProgress);
			OpenPhotosFolderCommand = new CommandHandler(() => PhotosFolderOpeningRequested?.Invoke(), !CalculationInProgress);
			CancelPendingCalculationCommand = new CommandHandler(OnPendingCalculationCancellationRequested, !CalculationInProgress);

			_barcodeResetTimer = new Timer(20000) { AutoReset = false };
			_barcodeResetTimer.Elapsed += OnBarcodeResetElapsed;
		}

		public void Dispose()
		{
			_barcodeResetTimer.Dispose();
		}

		public void UpdateSettings(AlgorithmSettings settings)
		{
			_requireBarcode = settings.RequireBarcode;
			_selectedWeightUnits = settings.SelectedWeightUnits;

			Dispatcher.Invoke(() =>
			{
				WeightLabelText = _selectedWeightUnits switch
				{
					WeightUnits.Gr => TranslationManager.Instance.Translate("WeightUnits.Gr") as string,
					WeightUnits.Kg => TranslationManager.Instance.Translate("WeightUnits.Kg") as string,
					_ => "",
				};
			});
		}

		public void UpdateBarcode(string code)
		{	
			if (!CanAcceptBarcodes || code == string.Empty)
				return;

			Dispatcher.Invoke(() => { ObjectCode = code; });
		}

		public void UpdateWeight(ScaleMeasurementData data)
		{
			if (CalculationInProgress || data == null)
				return;

			Dispatcher.Invoke(() =>
			{
				ObjectWeight = _selectedWeightUnits switch
				{
					WeightUnits.Gr => data.WeightGr,
					WeightUnits.Kg => data.WeightGr / 1000.0,
					_ => double.NaN,
				};
			});
		}

		public void UpdateCalculationStatus(CalculationStatus status)
		{
			if (status == CalculationStatus.InProgress)
				_barcodeResetTimer.Stop();

			UpdateDashStatus(status);
		}

		public void UpdateDataUponCalculationFinish(CalculationResultData data)
		{
			var length = 0;
			var width = 0;
			var height = 0;
			var volume = 0.0;

			var result = data.Result;
			if (result != null)
			{
				length = result.ObjectLengthMm;
				width = result.ObjectWidthMm;
				height = result.ObjectHeightMm;
				volume = length * width * height / 1000.0;
			}

			Dispatcher.Invoke(() =>
			{
				ObjectCode = "";
				UnitCount = 0;
				Comment = "";
				ObjectLength = length;
				ObjectWidth = width;
				ObjectHeight = height;
				ObjectVolume = volume;
			});
		}

		public void UpdateLastAlgorithm(string lastAlgorithmUsed, string wasRangeMeterUsed)
		{
			Dispatcher.Invoke(() =>
			{
				LastAlgorithmUsed = $"LastAlgorithm={lastAlgorithmUsed}, RM={wasRangeMeterUsed}";
			});
		}

		private void UpdateDashStatus(CalculationStatus status)
		{
			var values = GuiUtils.GetDashboardValuesFromCaculationStatus(status);

			values.DashBrush.Freeze();

			Dispatcher.Invoke(() =>
			{
				CalculationInProgress = values.CalculationInProgress;
				CalculationPending = values.CalculationPending;
				StatusText = values.Message;
				StatusBrush = values.DashBrush;
			});
		}
		
		private void RunVolumeCalculation()
		{
			var requestData = new CalculationRequestData(ObjectCode, UnitCount, Comment);
			CalculationRequested?.Invoke(requestData);
		}

		private void ResetMeasurementValues()
		{
			Dispatcher.Invoke(() =>
			{
				ObjectLength = 0;
				ObjectWidth = 0;
				ObjectHeight = 0;
				ObjectVolume = 0;
				UnitCount = 0;
				Comment = "";
			});
		}
		
		private void UpdateLockingStatus()
		{
			LockingStatusChanged?.Invoke(CanAcceptBarcodes);
		}

		private void OnBarcodeResetElapsed(object sender, ElapsedEventArgs e)
		{
			Dispatcher.Invoke(() => ObjectCode = "");
		}

		private void OnPendingCalculationCancellationRequested()
		{
			Dispatcher.Invoke(() => ObjectCode = "");
			CalculationCancellationRequested?.Invoke();
		}
	}
}
