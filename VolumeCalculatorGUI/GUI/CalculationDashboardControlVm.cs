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
	    private DeviceParams _deviceParams;

	    private DepthMap _latestDepthMap;

	    private DepthMapProcessor _processor;
	    private VolumeCalculator _volumeCalculator;

	    private int _measurementCount;

	    private string _objName;
		private int _objWidth;
	    private int _objHeight;
	    private int _objLength;
	    private long _objVolume;
	    private bool _calculationInProgress;

		public ICommand CalculateVolumeCommand { get; }

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

		public CalculationDashboardControlVm(ILogger logger, ApplicationSettings settings, DeviceParams deviceParams)
	    {
		    _logger = logger;
		    _applicationSettings = settings;
			_deviceParams = deviceParams;

		    _processor = new DepthMapProcessor(_logger, _deviceParams);
		    var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    _processor.SetCalculatorSettings(_applicationSettings.DistanceToFloor, cutOffDepth);

		    _resultFullPath = Path.Combine(_applicationSettings.OutputPath, Constants.ResultFileName);

			CalculateVolumeCommand = new CommandHandler(CalculateObjectVolume, !CalculationInProgress);
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

	    public void DeviceParamsUpdated(DeviceParams deviceParams)
	    {
		    _deviceParams = deviceParams;
		    Dispose();

			_processor = new DepthMapProcessor(_logger, _deviceParams);
		    var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    _processor.SetCalculatorSettings(_applicationSettings.DistanceToFloor, cutOffDepth);
		}

	    public void DepthFrameArrived(DepthMap depthMap)
	    {
		    _latestDepthMap = depthMap;
		    if (_volumeCalculator != null && _volumeCalculator.IsActive)
			    _volumeCalculator.AdvanceCalculation(depthMap);
	    }

	    public DepthMapProcessor GetDepthMapProcessor()
	    {
		    return _processor;
	    }

		public void CalculateObjectVolume()
	    {
		    var terminationTime = new DateTime(2018, 11, 1);
		    if (DateTime.Now > terminationTime)
			    return;

			if (_latestDepthMap == null)
		    {
			    MessageBox.Show("Нет кадров для обработки!", "Ошибка", MessageBoxButton.OK,
				    MessageBoxImage.Exclamation);
			    _logger.LogInfo("Attempted a volume check with no maps");

			    return;
		    }

		    if (!IsResultFileAccessible())
		    {
			    MessageBox.Show("Пожалуйста убедитесь, что файл с результатами закрыт, прежде чем выполнять вычисление",
				    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			    _logger.LogInfo("File");

				return;
		    }

		    try
		    {
			    CalculationInProgress = true;

			    _logger.LogInfo("Starting a volume check...");
			    Directory.CreateDirectory("out");

			    var cutOffDepth = (short) (_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
				DepthMapUtils.SaveDepthMapImageToFile(_latestDepthMap, "out/depth.png", _deviceParams.MinDepth,
				    _deviceParams.MaxDepth, cutOffDepth);

			    _volumeCalculator = new VolumeCalculator(_processor, _applicationSettings.SampleCount);
				_volumeCalculator.CalculationFinished += VolumeCalculator_CalculationFinished;
		    }
		    catch (Exception ex)
		    {
			    _logger.LogException("Failed to start volume calculation", ex);
			    MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
				    MessageBoxButton.OK, MessageBoxImage.Error);

			    CalculationInProgress = false;

		    }
		}

		private void VolumeCalculator_CalculationFinished(ObjectVolumeData volumeData)
		{
			CalculationInProgress = false;

			_volumeCalculator.CalculationFinished -= VolumeCalculator_CalculationFinished;
			_volumeCalculator = null;

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
				Directory.CreateDirectory(_applicationSettings.OutputPath);
				using (var resultFile = new StreamWriter(_resultFullPath, true, Encoding.Default))
				{
					var dateTime = DateTime.Now;
					var resultString = new StringBuilder(++_measurementCount);
					resultString.Append($@"{Constants.CsvSeparator}{dateTime.ToShortDateString()}");
					resultString.Append($@"{Constants.CsvSeparator}{dateTime.ToShortTimeString()}");
					resultString.Append($@"{Constants.CsvSeparator}{ObjName}");
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

	    private bool IsResultFileAccessible()
	    {
		    try
		    {
			    if (!File.Exists(_resultFullPath))
				    return true;

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