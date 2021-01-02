using System;
using System.IO;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegration.Scales;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;
using VolumeCalculator.Utils;

namespace VolumeCalculator
{
	internal class CalculationDashboardControlVm : BaseViewModel
	{
		public event Action<bool> LockingStatusChanged;
		public event Action<CalculationRequestData> CalculationRequested;
		public event Action CalculationCancellationRequested;
		public event Action WeightResetRequested;

		private readonly ILogger _logger;
		private readonly Timer _barcodeResetTimer;

		private bool _requireBarcode;
		private WeightUnits _selectedWeightUnits;
		private string _resultsFilePath;
		private string _photosDirectoryPath;
		
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

		private string _lastErrorMessage;
		
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

				if (_objectCode != "")
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

		private bool CanAcceptBarcodes
		{
			get
			{
				var usingManualInput = CodeBoxFocused || CommentBoxFocused || UnitCountBoxFocused;
				return !usingManualInput && !CalculationInProgress;
			}
		}

		private bool CodeReady
		{
			get
			{
				if (_requireBarcode)
					return !string.IsNullOrEmpty(ObjectCode);

				return true;
			}
		}

		public CalculationDashboardControlVm(ILogger logger)
		{
			_logger = logger;

			RunVolumeCalculationCommand = new CommandHandler(RunVolumeCalculation, !CalculationInProgress);
			ResetWeightCommand = new CommandHandler(()=> WeightResetRequested?.Invoke(), !CalculationInProgress);
			OpenResultsFileCommand = new CommandHandler(OpenResultsFile, !CalculationInProgress);
			OpenPhotosFolderCommand = new CommandHandler(OpenPhotosFolder, !CalculationInProgress);
			CancelPendingCalculationCommand = new CommandHandler(OnPendingCalculationCancellationRequired, !CalculationInProgress);

			_barcodeResetTimer = new Timer(20000) { AutoReset = false };
			_barcodeResetTimer.Elapsed += OnBarcodeResetElapsed;
		}

		private void OnBarcodeResetElapsed(object sender, ElapsedEventArgs e)
		{
			ObjectCode = "";
		}

		private void OnPendingCalculationCancellationRequired()
		{
			ObjectCode = "";
			CalculationCancellationRequested?.Invoke();
		}

		public void UpdateSettings(ApplicationSettings settings)
		{
			_requireBarcode = settings.AlgorithmSettings.RequireBarcode;
			_selectedWeightUnits = settings.AlgorithmSettings.SelectedWeightUnits;
			_resultsFilePath = settings.GeneralSettings.ResultsFilePath;
			_photosDirectoryPath = settings.GeneralSettings.PhotosDirectoryPath;

			Dispatcher.Invoke(() =>
			{
				switch (settings.AlgorithmSettings.SelectedWeightUnits)
				{
					case WeightUnits.Gr:
						WeightLabelText = "гр";
						break;
					case WeightUnits.Kg:
						WeightLabelText = "кг";
						break;
					default:
						WeightLabelText = "";
						break;
				}
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
				switch (_selectedWeightUnits)
				{
					case WeightUnits.Gr:
						ObjectWeight = data.WeightGr;
						break;
					case WeightUnits.Kg:
						ObjectWeight = data.WeightGr / 1000.0;
						break;
					default:
						ObjectWeight = double.NaN;
						break;
				}
			});
		}

		public void UpdateState(CalculationStatus status)
		{
			if (status == CalculationStatus.Running)
				_barcodeResetTimer.Stop();
		}

		public void UpdateDashStatus(DashboardStatus status)
		{
			switch (status)
			{
				case DashboardStatus.InProgress:
				{
					Dispatcher.Invoke(() =>
					{
						CalculationInProgress = true;
						StatusBrush = new SolidColorBrush(Colors.DarkOrange);
						StatusText = "Выполняется измерение...";
						CalculationPending = false;
					});
					break;
				}
				case DashboardStatus.Ready:
				{
					Dispatcher.Invoke(() =>
					{
						CalculationInProgress = false;
						StatusBrush = new SolidColorBrush(Colors.Green);
						StatusText = "Готов к измерению";
						CalculationPending = false;
					});
					break;
				}
				case DashboardStatus.Pending:
				{
					Dispatcher.Invoke(() =>
					{
						CalculationInProgress = false;
						StatusBrush = new SolidColorBrush(Colors.Blue);
						StatusText = "Запущен автотаймер...";
						CalculationPending = true;
					});
					break;
				}
				case DashboardStatus.Finished:
				{
					Dispatcher.Invoke(() =>
					{
						CalculationInProgress = false;
						StatusBrush = new SolidColorBrush(Colors.DarkGreen);
						StatusText = "Измерение завершено";
						CalculationPending = false;
					});
					break;
				}
				case DashboardStatus.Error:
				{
					Dispatcher.Invoke(() =>
					{
						CalculationInProgress = false;
						ObjectCode = "";
						StatusBrush = new SolidColorBrush(Colors.Red);
						StatusText = $"Произошла ошибка: {_lastErrorMessage}";
						CalculationPending = false;
					});
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}
		}

		private void OpenResultsFile()
		{
			try
			{
				var resultsFileInfo = new FileInfo(_resultsFilePath);
				if (!resultsFileInfo.Exists)
					return;

				IoUtils.OpenFile(_resultsFilePath);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to open results file", ex);
			}
		}

		private void OpenPhotosFolder()
		{
			try
			{
				var photosDirectoryInfo = new DirectoryInfo(_photosDirectoryPath);
				if (!photosDirectoryInfo.Exists)
					return;

				IoUtils.OpenFile(_photosDirectoryPath);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to open photos folder", ex);
			}
		}

		public void UpdateDataUponCalculationFinish(CalculationResultData data)
		{
			ObjectCode = "";
			UnitCount = 0;
			Comment = "";

			UpdateVolumeData(data.Result);
		}
		
		private void RunVolumeCalculation()
		{
			var requestData = new CalculationRequestData(ObjectCode, UnitCount, Comment);
			CalculationRequested?.Invoke(requestData);
		}

		private void UpdateVolumeData(CalculationResult result)
		{
			if (result == null)
			{
				Dispatcher.Invoke(() =>
				{
					ObjectLength = 0;
					ObjectWidth = 0;
					ObjectHeight = 0;
					ObjectVolume = 0;
				});

				return;
			}

			Dispatcher.Invoke(() =>
			{
				ObjectLength = result.ObjectLengthMm;
				ObjectWidth = result.ObjectWidthMm;
				ObjectHeight = result.ObjectHeightMm;
				ObjectVolume = ObjectLength * ObjectWidth * ObjectHeight / 1000.0;
			});
		}

		private void ResetMeasurementValues()
		{
			ObjectLength = 0;
			ObjectWidth = 0;
			ObjectHeight = 0;
			ObjectVolume = 0;
			UnitCount = 0;
			Comment = "";
		}

		public void UpdateErrorMessage(string message)
		{
			_lastErrorMessage = message;
		}

		private void UpdateLockingStatus()
		{
			LockingStatusChanged?.Invoke(CanAcceptBarcodes);
		}
	}
}