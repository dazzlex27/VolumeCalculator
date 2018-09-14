using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Common;
using FrameSources;
using FrameSources.KinectV2;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;
using System.Collections.Generic;

namespace VolumeCalculatorGUI.GUI
{
    internal class StreamViewControlVm : BaseViewModel, IDisposable
    {
	    private ApplicationSettings _applicationSettings;

	    private DeviceParams _deviceParams;
	    private readonly FrameSource _frameSource;

		private WriteableBitmap _colorImageBitmap;
	    private WriteableBitmap _depthImageBitmap;

	    private bool _useColorStream;
	    private bool _useDepthStream;

	    private bool _useAreaMask;
	    private bool _polygonPointsChaged;
	    private WorkingAreaMask _workingAreaMask;
	    private MaskPolygonControlVm _maskPolygonControlVm;

		public event Action<ImageData> ColorFrameReady
	    {
		    add => _frameSource.ColorFrameReady += value;
		    remove => _frameSource.ColorFrameReady -= value;
	    }

	    public event Action<DepthMap> DepthFrameReady;

	    public event Action<bool> UseColorStreamChanged;
	    public event Action<bool> UseDepthStreamChanged;
	    public event Action<DeviceParams> DeviceParamsChanged;
	    public event Action<ApplicationSettings> ApplicationSettingsChanged;

		public WriteableBitmap ColorImageBitmap
	    {
		    get => _colorImageBitmap;
		    set
		    {
			    if (Equals(_colorImageBitmap, value))
				    return;

			    _colorImageBitmap = value;
			    OnPropertyChanged();
		    }
	    }

	    public WriteableBitmap DepthImageBitmap
	    {
		    get => _depthImageBitmap;
		    set
		    {
			    if (Equals(_depthImageBitmap, value))
				    return;

			    _depthImageBitmap = value;
			    OnPropertyChanged();
		    }
	    }

	    public bool UseColorStream
	    {
		    get => _useColorStream;
		    set
		    {
			    if (_useColorStream == value)
				    return;

			    _useColorStream = value;
			    OnPropertyChanged();
			    UseColorStreamChanged?.Invoke(value);
		    }
	    }

	    public bool UseDepthStream
	    {
		    get => _useDepthStream;
		    set
		    {
			    if (_useDepthStream == value)
				    return;

			    _useDepthStream = value;
			    OnPropertyChanged();
			    UseDepthStreamChanged?.Invoke(value);
		    }
	    }

	    public bool UseAreaMask
	    {
		    get => _useAreaMask;
		    set
		    {
			    if (_useAreaMask == value)
				    return;

			    _useAreaMask = value;
				OnPropertyChanged();
		    }
	    }

	    public MaskPolygonControlVm MaskPolygonControlVm
	    {
		    get => _maskPolygonControlVm;
		    set
		    {
			    if (_maskPolygonControlVm == value)
				    return;

			    _maskPolygonControlVm = value;
			    OnPropertyChanged();
		    }
	    }

		private DeviceParams DeviceParams
	    {
		    get => _deviceParams;
		    set
		    {
			    if (_deviceParams == value)
				    return;

			    _deviceParams = value;
			    DeviceParamsChanged?.Invoke(value);
		    }
	    }

	    public StreamViewControlVm(ILogger logger, ApplicationSettings settings)
	    {
		    _applicationSettings = settings;

			_useColorStream = true;
		    _useDepthStream = true;

		    _frameSource = new KinectV2FrameSource(logger);
		    _frameSource.Start();
		    DeviceParams = _frameSource.GetDeviceParams();

		    _frameSource.ColorFrameReady += ColorImageUpdated;
		    _frameSource.DepthFrameReady += DepthImageUpdated;

		    UseColorStreamChanged += ToggleUseColorStream;
		    UseDepthStreamChanged += ToggleUseDepthStream;

			_useAreaMask = settings.UseWorkingAreaMask;
		    _maskPolygonControlVm = new MaskPolygonControlVm(settings.WorkingAreaContour);
			    //_maskPolygonControlVm.PolygonPointsChanged += MaskPolygonControlVm_PolygonPointsChanged;
	    }

		//private void MaskPolygonControlVm_PolygonPointsChanged(IReadOnlyList<Point> points)
		//{
		//	var newSettings = new ApplicationSettings(_applicationSettings.DistanceToFloor,
		//		_applicationSettings.MinObjHeight, _applicationSettings.OutputPath,
		//		_applicationSettings.UseWorkingAreaMask, new List<Point>(points));

		//	ApplicationSettingsChanged?.Invoke(newSettings);

		//	СДЕЛАТЬ В НАСТРОЙКАХ ВОЗМОЖНОСТЬ ЗАДАВАТЬ ВСЁ ЭТО ПРЯМО С ЭКРАНЧИКОМ КАРТЫ ГЛУБИНЫ
		//}

		public void Dispose()
	    {
		    _frameSource.Dispose();
		}

	    public DeviceParams GetDeviceParams()
	    {
		    return _deviceParams;
	    }

	    public void ApplicationSettingsUpdate(ApplicationSettings settings)
	    {
		    _applicationSettings = settings;
		    MaskPolygonControlVm.SetPolygonPoints(settings.WorkingAreaContour);
	    }

	    public void ColorImageUpdated(ImageData image)
	    {
		    var imageWidth = image.Width;
		    var imageHeight = image.Height;
		    var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

		    var format = PixelFormats.BlackWhite;
		    var bytesPerPixel = 0;
		    switch (image.BytesPerPixel)
		    {

			    case 3:
				    format = PixelFormats.Rgb24;
				    bytesPerPixel = Constants.BytesPerPixel24;
				    break;
			    case 4:
				    format = PixelFormats.Bgra32;
				    bytesPerPixel = Constants.BytesPerPixel32;
				    break;
		    }
		    var colorImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, format, null);
		    colorImageBitmap.WritePixels(fullRect, image.Data, imageWidth * bytesPerPixel, 0);

		    ColorImageBitmap = colorImageBitmap;
	    }

	    public void DepthImageUpdated(DepthMap depthMap)
	    {
		    var maskedMap = GetMaskedDepthMap(depthMap);

			var cutOffDepth = (short)(_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
		    var depthMapData = DepthMapUtils.GetColorizedDepthMapData(maskedMap, _deviceParams.MinDepth, _applicationSettings.DistanceToFloor,
			    cutOffDepth);

		    var imageWidth = maskedMap.Width;
		    var imageHeight = maskedMap.Height;
		    var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

		    var depthImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);
		    depthImageBitmap.WritePixels(fullRect, depthMapData, imageWidth * Constants.BytesPerPixel24, 0);

		    DepthImageBitmap = depthImageBitmap;

		    DepthFrameReady?.Invoke(maskedMap);
	    }

	    private DepthMap GetMaskedDepthMap(DepthMap depthMap)
	    {
		    if (!_useAreaMask)
			    return depthMap;

		    var maskedMap = new DepthMap(depthMap);

		    var maskIsValid = _workingAreaMask?.Width == depthMap.Width &&
		                      _workingAreaMask?.Height == depthMap.Height && !_polygonPointsChaged;
		    if (!maskIsValid)
		    {
			    var maskData = GeometryUtils.CreateWorkingAreaMask(MaskPolygonControlVm.PolygonPoints.ToList(), depthMap.Width, depthMap.Height);
			    _workingAreaMask = new WorkingAreaMask(depthMap.Width, depthMap.Height, maskData);
			    _polygonPointsChaged = false;
		    }

		    GeometryUtils.ApplyWorkingAreaMask(maskedMap, _workingAreaMask.Data);

		    return maskedMap;
	    }

	    private void ToggleUseColorStream(bool useColorStream)
	    {
		    if (useColorStream)
			    _frameSource.ResumeColorStream();
		    else
		    {
			    _frameSource.SuspendColorStream();
			    var emptyImage = new ImageData(1, 1, new byte[3], 3);
			    ColorImageUpdated(emptyImage);
		    }
	    }

	    private void ToggleUseDepthStream(bool useDepthStream)
	    {
		    if (useDepthStream)
			    _frameSource.ResumeDepthStream();
		    else
		    {
			    _frameSource.SuspendDepthStream();
			    var emptyMap = new DepthMap(1, 1, new short[1]);
			    DepthImageUpdated(emptyMap);
		    }
	    }
	}
}