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

namespace VolumeCalculatorGUI.GUI
{
    internal class CalculationDashboardControlVm : BaseViewModel, IDisposable
    {
	    private const string ResultFileName = "results.csv";

	    private readonly ILogger _logger;

	    private ApplicationSettings _applicationSettings;
	    private DeviceParams _deviceParams;

	    private ImageData _latestImageData;
	    private DepthMap _latestDepthMap;

	    private DepthMapProcessor _processor;
	    private VolumeCalculator _volumeCalculator;

	    private int _measurementCount;

	    private string _objName;
		private int _objWidth;
	    private int _objHeight;
	    private int _objLength;
	    private long _objVolume;

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

		public CalculationDashboardControlVm(ILogger logger, ApplicationSettings settings, DeviceParams deviceParams)
	    {
		    _logger = logger;
		    _applicationSettings = settings;
		    DeviceParamsUpdated(deviceParams);

		    CalculateVolumeCommand = new CommandHandler(CalculateObjectVolume, true);
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
	    }

	    public void DeviceParamsUpdated(DeviceParams deviceParams)
	    {
		    _deviceParams = deviceParams;
		    Dispose();

			_processor = new DepthMapProcessor(_logger, _deviceParams);
		    var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    _processor.SetCalculatorSettings(_applicationSettings.DistanceToFloor, cutOffDepth);
		}

		public void ColorFrameArrived(ImageData imageData)
	    {
		    _latestImageData = imageData;
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

		    try
		    {
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
			    _logger.LogException("Failed to complete volume measurement", ex);
			    MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
				    MessageBoxButton.OK, MessageBoxImage.Error);
		    }
		}

		private void VolumeCalculator_CalculationFinished(ObjectVolumeData volumeData)
		{
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

			Directory.CreateDirectory(_applicationSettings.OutputPath);
			var fullPath = Path.Combine(_applicationSettings.OutputPath, ResultFileName);
			using (var resultFile = new StreamWriter(fullPath, true, Encoding.Default))
			{
				var time = DateTime.Now;
				var resultString =
					$@"{++_measurementCount},{time.ToShortDateString()},{time.ToShortTimeString()},{ObjName},{ObjLength},{ObjWidth},{ObjHeight},{ObjVolume}";
				resultFile.WriteLine(resultString);
			}
		}

		private void UpdateVolumeData(ObjectVolumeData volumeData)
	    {
		    ObjLength = volumeData.Length;
		    ObjWidth = volumeData.Width;
		    ObjHeight = volumeData.Height;
		    ObjVolume = ObjLength * ObjWidth * ObjHeight;
	    }
    }
}