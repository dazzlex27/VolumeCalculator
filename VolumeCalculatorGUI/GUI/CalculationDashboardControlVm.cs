using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Common;
using FrameProviders;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Logic;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
    internal class CalculationDashboardControlVm : BaseViewModel, IDisposable
    {
	    private readonly ILogger _logger;
	    private string _resultFullPath;

	    private ApplicationSettings _applicationSettings;
	    private ColorCameraParams _colorCameraParams;
	    private DepthCameraParams _depthCameraParams;

	    private ImageData _latestColorFrame;
	    private DepthMap _latestDepthMap;

	    private DepthMapProcessor _processor;
	    private VolumeCalculator _volumeCalculator;

	    private bool _colorFrameReady;
	    private bool _depthFrameReady;

	    private int _measurementCount;

	    private string _objName;
		private int _objWidth;
	    private int _objHeight;
	    private int _objLength;
	    private long _objVolume;
	    private bool _calculationInProgress;

		public ICommand CalculateVolumeCommand { get; }

		public ICommand CalculateVolumeAltCommand { get; }

	    public string ObjName
	    {
		    get => _objName;
		    set
		    {
			    if (_objName == value)
				    return;

			    _objName = value;
				OnPropertyChanged();
		    }
	    }

	    public int ObjLength
	    {
		    get => _objLength;
		    set
		    {
			    if (_objLength == value)
				    return;

			    _objLength = value;
			    OnPropertyChanged();
		    }
	    }

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

		public CalculationDashboardControlVm(ILogger logger, ApplicationSettings settings, ColorCameraParams colorCameraParams,
			DepthCameraParams depthCameraParams)
	    {
		    _logger = logger;
		    _applicationSettings = settings;
		    _colorCameraParams = colorCameraParams;
		    _depthCameraParams = depthCameraParams;

		    _processor = new DepthMapProcessor(_logger, _colorCameraParams, _depthCameraParams);
		    var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    _processor.SetCalculatorSettings(_applicationSettings.DistanceToFloor, cutOffDepth);

		    _resultFullPath = Path.Combine(_applicationSettings.OutputPath, Constants.ResultFileName);

			CalculateVolumeCommand = new CommandHandler(CalculateObjectVolume, !CalculationInProgress);
		    CalculateVolumeAltCommand = new CommandHandler(CalculateObjectVolumeAlt, !CalculationInProgress);
	    }

	    public void Dispose()
	    {
			if (_volumeCalculator != null && _volumeCalculator.IsActive)
				_processor.Dispose();
		}

	    public void ApplicationSettingsUpdated(ApplicationSettings applicationSettings)
	    {
		    _applicationSettings = applicationSettings;
		    var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    _processor.SetCalculatorSettings(_applicationSettings.DistanceToFloor, cutOffDepth);
		    _resultFullPath = Path.Combine(_applicationSettings.OutputPath, Constants.ResultFileName);
		}

	    public void DeviceParamsUpdated(ColorCameraParams colorCameraParams, DepthCameraParams depthCameraParams)
	    {
		    _colorCameraParams = colorCameraParams;
		    _depthCameraParams = depthCameraParams;
		    Dispose();

		    _processor = new DepthMapProcessor(_logger, _colorCameraParams, _depthCameraParams);
		    var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    _processor.SetCalculatorSettings(_applicationSettings.DistanceToFloor, cutOffDepth);
		}

	    public void ColorFrameArrived(ImageData image)
	    {
		    _latestColorFrame = image;

		    var calculationIsActive = _volumeCalculator != null && _volumeCalculator.IsActive;
		    if (!calculationIsActive)
			    return;

		    _colorFrameReady = true;

		    if (!_depthFrameReady)
			    return;

		    _volumeCalculator.AdvanceCalculation(_latestColorFrame, _latestDepthMap);
		    _colorFrameReady = false;
		    _depthFrameReady = false;
		}

	    public void DepthFrameArrived(DepthMap depthMap)
	    {
		    _latestDepthMap = depthMap;

		    var calculationIsActive = _volumeCalculator != null && _volumeCalculator.IsActive;
		    if (!calculationIsActive)
			    return;

		    _depthFrameReady = true;

		    if (!_colorFrameReady)
			    return;

		    _volumeCalculator.AdvanceCalculation(_latestColorFrame, _latestDepthMap);
		    _colorFrameReady = false;
		    _depthFrameReady = false;
	    }

	    public DepthMapProcessor GetDepthMapProcessor()
	    {
		    return _processor;
	    }

		private void CalculateObjectVolume()
	    {
			if (_latestDepthMap == null)
		    {
			    MessageBox.Show("Нет кадров для обработки!", "Ошибка", MessageBoxButton.OK,
				    MessageBoxImage.Exclamation);
			    _logger.LogInfo("Attempted a volume check with no maps");

			    return;
		    }

		    if (string.IsNullOrEmpty(ObjName))
		    {
			    MessageBox.Show("Код объекта не может быть пустым", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);

			    return;
			}

		    if (!IsResultFileAccessible())
		    {
			    MessageBox.Show("Пожалуйста убедитесь, что файл с результатами доступен для записи и закрыт, прежде чем выполнять вычисление",
				    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			    _logger.LogInfo("Failed to access the result file");

				return;
		    }

		    try
		    {
			    CalculationInProgress = true;

			    _logger.LogInfo("Starting a volume check...");
			    Directory.CreateDirectory("out");

			    var cutOffDepth = (short) (_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
				DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, "out/depth.png", _depthCameraParams.MinDepth,
					_depthCameraParams.MaxDepth, cutOffDepth);

			    _volumeCalculator = new VolumeCalculator(_logger, _processor, false, _applicationSettings.SampleCount);
				_volumeCalculator.CalculationFinished += VolumeCalculator_CalculationFinished;
				_volumeCalculator.CalculationCancelled += VolumeCalculator_CalculationCancelled;
		    }
		    catch (Exception ex)
		    {
			    _logger.LogException("Failed to start volume calculation", ex);
			    MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
				    MessageBoxButton.OK, MessageBoxImage.Error);

			    CalculationInProgress = false;
		    }
		}

	    private void CalculateObjectVolumeAlt()
	    {
		    if (_latestDepthMap == null || _latestColorFrame == null)
		    {
			    MessageBox.Show("Нет кадров для обработки!", "Ошибка", MessageBoxButton.OK,
				    MessageBoxImage.Exclamation);
			    _logger.LogInfo("Attempted a volume check with no maps");

			    return;
		    }

		    if (string.IsNullOrEmpty(ObjName))
		    {
			    MessageBox.Show("Введите код объекта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);

			    return;
		    }

		    if (!IsResultFileAccessible())
		    {
			    MessageBox.Show("Пожалуйста убедитесь, что файл с результатами доступен для записи и закрыт, прежде чем выполнять вычисление",
				    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			    _logger.LogInfo("Failed to access the result file");

			    return;
		    }

		    try
		    {
			    CalculationInProgress = true;

			    _logger.LogInfo("Starting a volume check...");
			    Directory.CreateDirectory("out");

			    ImageUtils.SaveImageDataToFile(_latestColorFrame, "out/color.png");

				var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
			    DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, "out/depth.png", _depthCameraParams.MinDepth,
				    _depthCameraParams.MaxDepth, cutOffDepth);

			    _volumeCalculator = new VolumeCalculator(_logger, _processor, true, _applicationSettings.SampleCount);
			    _volumeCalculator.CalculationFinished += VolumeCalculator_CalculationFinished;
			    _volumeCalculator.CalculationCancelled += VolumeCalculator_CalculationCancelled;
		    }
		    catch (Exception ex)
		    {
			    _logger.LogException("Failed to start volume calculation", ex);
			    MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
				    MessageBoxButton.OK, MessageBoxImage.Error);

			    CalculationInProgress = false;
		    }
		}

		private void VolumeCalculator_CalculationCancelled()
		{
			CalculationInProgress = false;

			DisposeVolumeCalculator();

			_logger.LogError("Calculation cancelled on timeout");
			MessageBox.Show("Не удалось собрать указанное количество образцов для измерения, проверьте соединение с устройством", 
				"Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		private void VolumeCalculator_CalculationFinished(ObjectVolumeData volumeData)
		{
			CalculationInProgress = false;
			DisposeVolumeCalculator();

			if (volumeData == null)
			{
				volumeData = new ObjectVolumeData(0, 0, 0);
				UpdateVolumeData(volumeData);

				MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.LogError("Volume check returned null");
			}
			else
			{
				_logger.LogInfo($"Completed a volume check, L={volumeData.Length} W={volumeData.Width} H={volumeData.Height}");

				UpdateVolumeData(volumeData);
			}

			if (volumeData.Length == 0 || volumeData.Width == 0 || volumeData.Height == 0)
			{
				MessageBox.Show("Объект не найден", "Результат вычисления",
					MessageBoxButton.OK, MessageBoxImage.Information);

				return;
			}

			try
			{
				var safeName = ObjName;
				if (!string.IsNullOrEmpty(safeName))
				{
					var nameWithoutReturns = ObjName.Replace(Environment.NewLine, " ");
					safeName = nameWithoutReturns.Replace(Constants.CsvSeparator, " ");
				}

				Directory.CreateDirectory(_applicationSettings.OutputPath);
				using (var resultFile = new StreamWriter(_resultFullPath, true, Encoding.Default))
				{
					var dateTime = DateTime.Now;
					var resultString = new StringBuilder();
					resultString.Append(++_measurementCount);
					resultString.Append($@"{Constants.CsvSeparator}{safeName}");
					resultString.Append($@"{Constants.CsvSeparator}{dateTime.ToShortDateString()}");
					resultString.Append($@"{Constants.CsvSeparator}{dateTime.ToShortTimeString()}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjLength}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjWidth}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjHeight}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjVolume}");
					resultFile.WriteLine(resultString);
					resultFile.Flush();
					_logger.LogInfo("Wrote the calculated values to csv");
				}
			}
			catch (IOException ex)
			{
				MessageBox.Show("Не удалось записать результат измерений в файл, проверьте доступность файла и повторите измерения", 
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				_logger.LogException(
					$@"Failed to write calculated values to {Constants.ResultFileName} ({_applicationSettings.OutputPath})",
					ex);
			}
		}

		private void UpdateVolumeData(ObjectVolumeData volumeData)
	    {
		    ObjLength = volumeData.Length;
		    ObjWidth = volumeData.Width;
		    ObjHeight = volumeData.Height;
		    ObjVolume = ObjLength * ObjWidth * ObjHeight;
	    }

	    private void DisposeVolumeCalculator()
	    {
		    _volumeCalculator.CalculationFinished -= VolumeCalculator_CalculationFinished;
		    _volumeCalculator.CalculationCancelled -= VolumeCalculator_CalculationCancelled;
		    _volumeCalculator = null;
		}

	    private void WriteHeadersToCsv()
	    {
		    Directory.CreateDirectory(_applicationSettings.OutputPath);
		    using (var resultFile = new StreamWriter(_resultFullPath, true, Encoding.Default))
		    {
			    var resultString = new StringBuilder();
			    resultString.Append("#");
			    resultString.Append($@"{Constants.CsvSeparator}name");
			    resultString.Append($@"{Constants.CsvSeparator}date local");
			    resultString.Append($@"{Constants.CsvSeparator}time local");
			    resultString.Append($@"{Constants.CsvSeparator}length, mm");
			    resultString.Append($@"{Constants.CsvSeparator}width, mm");
			    resultString.Append($@"{Constants.CsvSeparator}height, mm");
			    resultString.Append($@"{Constants.CsvSeparator}volume, mm^2");
			    resultFile.WriteLine(resultString);
			    resultFile.Flush();
			    _logger.LogInfo($@"Created the csv at {_resultFullPath} and wrote the headers to it");
		    }
	    }
	
	private bool IsResultFileAccessible()
	    {
		    try
		    {
			    if (!File.Exists(_resultFullPath))
			    {
					WriteHeadersToCsv();
				    return true;
			    }

				using (Stream stream = new FileStream(_resultFullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				{
					stream.ReadByte();
				}

			    return true;
		    }
		    catch (IOException)
		    {
			    return false;
		    }
		}
    }
}