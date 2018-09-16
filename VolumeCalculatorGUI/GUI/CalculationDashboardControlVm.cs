using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Common;
using FrameSources;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Logic;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
    internal class CalculationDashboardControlVm : BaseViewModel, IDisposable
    {
	    private readonly ILogger _logger;

		private DepthMapProcessor _volumeCalculator;
	    private ApplicationSettings _applicationSettings;
	    private DeviceParams _deviceParams;

	    private ImageData _latestImageData;
	    private DepthMap _latestDepthMap;

		private int _objWidth;
	    private int _objHeight;
	    private int _objLength;
	    private long _objVolume;

	    private ICommand _measureVolumeCommand;

	    private bool CanCalculateVolume => _volumeCalculator != null && _latestDepthMap != null;

		public ICommand CalculateVolumeCommand => _measureVolumeCommand ?? (_measureVolumeCommand =
		                                              new CommandHandler(CalculateObjectVolume, CanCalculateVolume));

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
	    }

	    public void Dispose()
	    {
			if (_volumeCalculator != null && _volumeCalculator.IsActive)
				_volumeCalculator.Dispose();
		}

	    public void ApplicationSettingsUpdated(ApplicationSettings applicationSettings)
	    {
		    _applicationSettings = applicationSettings;
		    var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    _volumeCalculator.SetCalculatorSettings(_applicationSettings.DistanceToFloor, cutOffDepth);
	    }

	    public void DeviceParamsUpdated(DeviceParams deviceParams)
	    {
		    _deviceParams = deviceParams;
		    Dispose();

			_volumeCalculator = new DepthMapProcessor(_logger, _deviceParams);
		}

		public void ColorFrameArrived(ImageData imageData)
	    {
		    _latestImageData = imageData;
	    }

	    public void DepthFrameArrived(DepthMap depthMap)
	    {
		    _latestDepthMap = depthMap;
	    }

	    public DepthMapProcessor GetVolumeCalculator()
	    {
		    return _volumeCalculator;
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

			    IoUtils.CreateBitmapFromImageData(_latestImageData);
			    var cutOffDepth = (short) (_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
			    var depthBmp = IoUtils.CreateBitmapFromDepthMap(_latestDepthMap, _deviceParams.MinDepth, _deviceParams.MaxDepth, cutOffDepth);
			    depthBmp.Save("out/depth.png");

			    var latestDepthMap = _latestDepthMap;
			    _latestDepthMap = null;
				var volumeData = _volumeCalculator.CalculateVolume(latestDepthMap);

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
		    }
		    catch (Exception ex)
		    {
			    _logger.LogException("Failed to complete volume measurement", ex);
			    MessageBox.Show("Во время обработки произошла ошибка. Информация записана в журнал", "Ошибка",
				    MessageBoxButton.OK, MessageBoxImage.Error);
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