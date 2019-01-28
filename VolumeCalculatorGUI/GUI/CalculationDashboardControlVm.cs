using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DeviceIntegrations.IoCircuits;
using DeviceIntegrations.Scales;
using DeviceIntegrations.Scanners;
using ExtIntegration;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class CalculationDashboardControlVm : BaseViewModel, IDisposable
	{
		private readonly ILogger _logger;
		private readonly FrameProvider _frameProvider;
		private readonly IReadOnlyList<IBarcodeScanner> _scanners;
		private readonly IScales _scales;
		private readonly IIoCircuit _circuit;

		private readonly DashStatusUpdater _dashStatusUpdater;

		private ApplicationSettings _applicationSettings;

		private VolumeCalculator _volumeCalculator;
		private CalculationResultFileProcessor _calculationResultFileProcessor;
		private readonly DepthMapProcessor _processor;

		private readonly List<IRequestSender> _requestSenders;

		private string _objectCode;
		private double _objectWeight;
		private uint _unitCount;
		private int _objectWidth;
		private int _objectHeight;
		private int _objectLength;
		private double _objectVolume;
		private string _comment;
		private bool _calculationInProgress;
		private bool _usingManualCodeInput;
		private SolidColorBrush _statusBrush;
		private string _statusText;
		private bool _calculationPending;

		private bool _codeBoxFocused;
		private bool _unitCountBoxFocused;
		private bool _commentBoxFocused;

		public ICommand CalculateVolumeBoxCommand { get; }

		public ICommand CalculateVolumeFreeCommand { get; }

		public ICommand CalculateVolumeRgbCommand { get; }

		public ICommand ResetWeightCommand { get; }

		public ICommand OpenResultsFileCommand { get; }

		public ICommand OpenPhotosFolderCommand { get; }

		public ICommand CancelPendingCalculationCommand { get; }

		public bool WaitingForReset { get; set; }

		public string ObjectCode
		{
			get => _objectCode;
			set
			{
				if (_objectCode == value)
					return;

				_objectCode = value;
				if (CodeReady)
					ResetMeasurementValues();

				OnPropertyChanged();
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
			set
			{
				if (_unitCount == value)
					return;

				_unitCount = value;
				OnPropertyChanged();
			}
		}

		public int ObjectLength
		{
			get => _objectLength;
			set
			{
				if (_objectLength == value)
					return;

				_objectLength = value;
				OnPropertyChanged();
			}
		}

		public int ObjectWidth
		{
			get => _objectWidth;
			set
			{
				if (_objectWidth == value)
					return;

				_objectWidth = value;
				OnPropertyChanged();
			}
		}

		public int ObjectHeight
		{
			get => _objectHeight;
			set
			{
				if (_objectHeight == value)
					return;

				_objectHeight = value;
				OnPropertyChanged();
			}
		}

		public double ObjectVolume
		{
			get => _objectVolume;
			set
			{
				if (Math.Abs(_objectVolume - value) < 0.0001)
					return;

				_objectVolume = value;
				OnPropertyChanged();
			}
		}

		public string Comment
		{
			get => _comment;
			set
			{
				if (_comment == value)
					return;

				_comment = value;
				OnPropertyChanged();
			}
		}

		public bool CalculationInProgress
		{
			get => _calculationInProgress;
			set
			{
				if (_calculationInProgress == value)
					return;

				_calculationInProgress = value;
				OnPropertyChanged();
			}
		}

		public bool UsingManualCodeInput
		{
			get => _usingManualCodeInput;
			set
			{
				if (_usingManualCodeInput == value)
					return;

				_usingManualCodeInput = value;
				OnPropertyChanged();
			}
		}

		public SolidColorBrush StatusBrush
		{
			get => _statusBrush;
			set
			{
				if (Equals(_statusBrush, value))
					return;

				_statusBrush = value;
				OnPropertyChanged();
			}
		}

		public string StatusText
		{
			get => _statusText;
			set
			{
				if (_statusText == value)
					return;

				_statusText = value;
				OnPropertyChanged();
			}
		}

		public bool CodeBoxFocused
		{
			get => _codeBoxFocused;
			set
			{
				if (_codeBoxFocused == value)
					return;

				_codeBoxFocused = value;
				OnPropertyChanged();
			}
		}

		public bool UnitCountBoxFocused
		{
			get => _unitCountBoxFocused;
			set
			{
				if (_unitCountBoxFocused == value)
					return;

				_unitCountBoxFocused = value;
				OnPropertyChanged();
			}
		}

		public bool CommentBoxFocused
		{
			get => _commentBoxFocused;
			set
			{
				if (_commentBoxFocused == value)
					return;

				_commentBoxFocused = value;
				OnPropertyChanged();
			}
		}

		public bool CalculationPending
		{
			get => _calculationPending;
			set
			{
				if (_calculationPending == value)
					return;

				_calculationPending = value;
				OnPropertyChanged();
			}
		}

		private bool CanAcceptBarcodes
		{
			get
			{
				var usingManualInput = UsingManualCodeInput || CodeBoxFocused || CommentBoxFocused || UnitCountBoxFocused;
				return !usingManualInput && !CalculationInProgress;
			}
		}

		public bool CanRunAutoTimer => CodeReady && WeightReady && CanAcceptBarcodes;

		public bool CodeReady => !string.IsNullOrEmpty(ObjectCode);

		public bool WeightReady => CurrentWeighingStatus == MeasurementStatus.Measured;

		public MeasurementStatus CurrentWeighingStatus { get; set; }

		public CalculationDashboardControlVm(ILogger logger, FrameProvider frameProvider, DepthMapProcessor processor, 
			IReadOnlyList<IBarcodeScanner> scanners, IScales scales, IIoCircuit circuit, ApplicationSettings applicationSettings)
		{
			_logger = logger;
			_frameProvider = frameProvider;
			_processor = processor;

			if (scanners != null)
			{
				_scanners = scanners;
				foreach (var scanner in scanners.Where(s => s != null))
					scanner.CharSequenceFormed += OnBarcodeReady;
			}

			if (scales != null)
			{
				_scales = scales;
				_scales.MeasurementReady += OnWeightMeasurementReady;
			}

			if (circuit != null)
				_circuit = circuit;

			_applicationSettings = applicationSettings;

			_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, applicationSettings.OutputPath);

			_dashStatusUpdater = new DashStatusUpdater(_logger, _circuit, this) { DashStatus = DashboardStatus.Ready };
			CreateAutoStartTimer(applicationSettings.TimeToStartMeasurementMs);

			CalculateVolumeBoxCommand = new CommandHandler(CalculateObjectVolumeBoxShape, !CalculationInProgress);
			CalculateVolumeFreeCommand = new CommandHandler(CalculateObjectVolumeFreeShape, !CalculationInProgress);
			CalculateVolumeRgbCommand = new CommandHandler(CalculateObjectVolumeRgb, !CalculationInProgress);
			ResetWeightCommand = new CommandHandler(ResetWeight, !CalculationInProgress);
			OpenResultsFileCommand = new CommandHandler(OpenResultsFile, !CalculationInProgress);
			OpenPhotosFolderCommand = new CommandHandler(OpenPhotosFolder, !CalculationInProgress);
			CancelPendingCalculationCommand = new CommandHandler(_dashStatusUpdater.CancelPendingCalculation, !CalculationInProgress);

			_requestSenders = new List<IRequestSender>();
			CreateRequestSenders();
		}

		public void Dispose()
		{
			_dashStatusUpdater?.Dispose();

			if (_scanners != null)
			{
				foreach (var scanner in _scanners.Where(s => s != null))
					scanner.CharSequenceFormed -= OnBarcodeReady;
			}

			if (_scales != null)
				_scales.MeasurementReady -= OnWeightMeasurementReady;

			foreach (var sender in _requestSenders)
				sender.Dispose();
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_applicationSettings = settings;
			_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, settings.OutputPath);

			CreateAutoStartTimer(settings.TimeToStartMeasurementMs);
		}

		private void CreateRequestSenders()
		{
			if (_applicationSettings.WebRequestSettings.EnableRequests)
				_requestSenders.Add(new HttpRequestSender(_logger, _applicationSettings.WebRequestSettings));

			if (_applicationSettings.SqlRequestSettings.EnableRequests)
				_requestSenders.Add(new SqlRequestSender(_logger, _applicationSettings.SqlRequestSettings));

			foreach (var sender in _requestSenders)
			{
				try
				{
					sender.Connect();
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to connect to requet destination", ex);
				}
			}
		}

		private void CreateAutoStartTimer(long intervalMs)
		{
			if (_dashStatusUpdater.Timer != null)
				_dashStatusUpdater.Timer.Elapsed -= OnMeasurementTimerElapsed;

			_dashStatusUpdater.Timer = new Timer(intervalMs) {AutoReset = false};
			_dashStatusUpdater.Timer.Elapsed += OnMeasurementTimerElapsed;
		}

		private void OnBarcodeReady(string code)
		{
			if (!CanAcceptBarcodes || code == string.Empty)
				return;

			Dispatcher.Invoke(() => { ObjectCode = code; });
		}

		private void OnWeightMeasurementReady(ScaleMeasurementData data)
		{
			if (CalculationInProgress || data == null)
				return;

			Dispatcher.Invoke(() =>
			{
				ObjectWeight = data.WeightKg;
				CurrentWeighingStatus = data.Status;
			});
		}

		private void ResetWeight()
		{
			_scales?.ResetWeight();
		}

		private void OpenResultsFile()
		{
			try
			{
				var resultsFileInfo = new FileInfo(_applicationSettings.ResultsFilePath);
				if (!resultsFileInfo.Exists)
					return;

				IoUtils.OpenFile(_applicationSettings.ResultsFilePath);
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
				var photosDirectoryInfo = new DirectoryInfo(_applicationSettings.PhotosDirectoryPath);
				if (!photosDirectoryInfo.Exists)
					return;

				IoUtils.OpenFile(_applicationSettings.PhotosDirectoryPath);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to open photos folder", ex);
			}
		}

		private void OnMeasurementTimerElapsed(object sender, ElapsedEventArgs e)
		{
			CalculateObjectVolumeInternal(_applicationSettings.UseBoxShapeAlgorithmByDefault, false);
		}

		private void CalculateObjectVolumeBoxShape()
		{
			CalculateObjectVolumeInternal(false, false);
		}

		private void CalculateObjectVolumeFreeShape()
		{
			CalculateObjectVolumeInternal(true, false);
		}

		private void CalculateObjectVolumeRgb()
		{
			CalculateObjectVolumeInternal(false, true);
		}

		private void CalculateObjectVolumeInternal(bool applyPerspective, bool useRgbData)
		{
			if (CalculationInProgress)
			{
				_logger.LogInfo("tried to start calculation while another one was running, " + Environment.StackTrace);
				return;
			}

			try
			{
				var canRunCalculation = CheckIfPreConditionsAreSatisfied();
				if (!canRunCalculation)
					return;

				_dashStatusUpdater.DashStatus = DashboardStatus.InProgress;

				_logger.LogInfo($"Starting a volume check (applyPerspective={applyPerspective}, useRgb={useRgbData})...");

				_volumeCalculator = new VolumeCalculator(_logger, _frameProvider, _processor, _applicationSettings, applyPerspective, 
					useRgbData);
				_volumeCalculator.CalculationFinished += OnCalculationFinished;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to start volume calculation", ex);
				MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);

				_dashStatusUpdater.DashStatus = DashboardStatus.Error;
			}
		}

		private bool CheckIfPreConditionsAreSatisfied()
		{
			if (string.IsNullOrEmpty(ObjectCode))
			{
				MessageBox.Show("Введите код объекта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);

				_logger.LogInfo("Weird autotimer issue occured" + Environment.StackTrace);

				return false;
			}

			var killedProcess = IoUtils.KillProcess("Excel");
			if (killedProcess)
				return true;

			MessageBox.Show(
				"Не удалось закрыть файл с результатами, убедитесь, что файл закрыт",
				"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			_logger.LogInfo("Failed to access the result file");

			return false;
		}

		private void OnCalculationFinished(ObjectVolumeData result, CalculationStatus status)
		{
			_logger.LogInfo("Calculation finished, processing results...");

			DisposeVolumeCalculator();

			UpdateVisualsWithResult(result, status);

			var calculationResult = new CalculationResult(DateTime.Now, ObjectCode, ObjectWeight, UnitCount, ObjectLength,
				ObjectWidth, ObjectHeight, ObjectVolume, Comment);

			WriteObjectDataToFile(calculationResult);
			SendRequests(calculationResult);

			_logger.LogInfo("Done processing calculatiuon results");
		}

		private void UpdateVolumeData(ObjectVolumeData volumeData)
		{
			Dispatcher.Invoke(() => 
			{
				ObjectLength = volumeData.Length;
				ObjectWidth = volumeData.Width;
				ObjectHeight = volumeData.Height;
				ObjectVolume = ObjectLength * ObjectWidth * ObjectHeight / 1000.0;
			});
		}

		private void DisposeVolumeCalculator()
		{
			if (_volumeCalculator == null)
				return;

			_volumeCalculator.CalculationFinished -= OnCalculationFinished;
			_volumeCalculator.Dispose();
			_volumeCalculator = null;
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

		private void UpdateVisualsWithResult(ObjectVolumeData result, CalculationStatus status)
		{
			try
			{
				if (status == CalculationStatus.Sucessful && result != null)
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Finished;
					UpdateVolumeData(result);
				}
				else
				{
					_dashStatusUpdater.DashStatus = DashboardStatus.Error;
					var emptyResult = new ObjectVolumeData(0, 0, 0);
					UpdateVolumeData(emptyResult);
				}

				switch (status)
				{
					case CalculationStatus.Error:
					{
						MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал",
							"Результат измерения",
							MessageBoxButton.OK, MessageBoxImage.Error);
						_logger.LogError("Volume calculation finished with errors");
						break;
					}
					case CalculationStatus.TimedOut:
					{
						MessageBox.Show(
							"Не удалось собрать указанное количество образцов для измерения, проверьте соединение с устройством",
							"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						_logger.LogError("Failed to acquire enough samples for volume calculation");
						break;
					}
					case CalculationStatus.Undefined:
					{
						_dashStatusUpdater.DashStatus = DashboardStatus.Error;
						MessageBox.Show("Объект не найден", "Результат измерения",
							MessageBoxButton.OK, MessageBoxImage.Information);
						_logger.LogError("No object was found during volume calculation");
						break;
					}
					case CalculationStatus.Aborted:
					{
						MessageBox.Show("Измерение прервано", "Результат измерения",
							MessageBoxButton.OK, MessageBoxImage.Information);
						_logger.LogError("Volume calculation was aborted");
						break;
					}
					case CalculationStatus.Sucessful:
					{
						if (result != null)
							_logger.LogInfo(
								$"Completed a volume check, L={result.Length} W={result.Width} H={result.Height}");
						else
							_logger.LogError("Calculation was successful, but null result was returned");
						break;
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(status), status,
							@"Failed to resolve failed calculation status");
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to process result data", ex);
			}
		}

		private void WriteObjectDataToFile(CalculationResult calculationResult)
		{
			try
			{
				_calculationResultFileProcessor.WriteCalculationResult(calculationResult);

				ObjectCode = string.Empty;
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					"Не удалось записать результат измерений в файл, проверьте доступность файла и повторите измерения",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.LogException(
					$@"Failed to write calculated values to {_calculationResultFileProcessor.FullOutputPath})",
					ex);
			}
		}

		private void SendRequests(CalculationResult result)
		{
			foreach (var sender in _requestSenders)
			{
				Task.Run(() =>
				{
					try
					{
						var sent = sender.Send(result);
					}
					catch (Exception ex)
					{
						_logger.LogException($"Failed to send a request!", ex);
					}

				});
			}
		}
	}
}