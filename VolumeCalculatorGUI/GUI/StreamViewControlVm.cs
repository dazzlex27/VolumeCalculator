using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Common;
using FrameProviders;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class StreamViewControlVm : BaseViewModel, IDisposable
	{
		public event Action<ImageData> ColorFrameReady
		{
			add => _frameProvider.ColorFrameReady += value;
			remove => _frameProvider.ColorFrameReady -= value;
		}

		public event Action<DepthMap> RawDepthFrameReady;
		public event Action<DepthMap> DepthFrameReady;

		public event Action<bool> UseColorStreamChanged;
		public event Action<bool> UseDepthStreamChanged;
		public event Action<ColorCameraParams, DepthCameraParams> DeviceParamsChanged;

		private readonly FrameProvider _frameProvider;
		private readonly ILogger _logger;

		private ApplicationSettings _applicationSettings;
		private DepthCameraParams _depthCameraParams;

		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		private MaskPolygonControlVm _maskPolygonControlVm;
		private MaskPolygonControlVm _maskRectangleControlVm;

		private bool _useColorStream;
		private bool _useDepthStream;

		private bool _useAreaMask;
		private WorkingAreaMask _workingAreaMask;
		private bool _polygonPointsChaged;

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

		public MaskPolygonControlVm MaskRectangleControlVm
		{
			get => _maskRectangleControlVm;
			set
			{
				if (ReferenceEquals(_maskRectangleControlVm, value))
					return;

				_maskRectangleControlVm = value;
				OnPropertyChanged();
			}
		}

		public MaskPolygonControlVm MaskPolygonControlVm
		{
			get => _maskPolygonControlVm;
			set
			{
				if (ReferenceEquals(_maskPolygonControlVm, value))
					return;

				_maskPolygonControlVm = value;
				OnPropertyChanged();
			}
		}

		private DepthCameraParams DepthCameraParams
		{
			get => _depthCameraParams;
			set
			{
				if (_depthCameraParams == value)
					return;

				_depthCameraParams = value;
				DeviceParamsChanged?.Invoke(_frameProvider.GetColorCameraParams(), value);
			}
		}

		public StreamViewControlVm(ILogger logger, ApplicationSettings settings)
		{
			_logger = logger;
			_applicationSettings = settings;

			_useColorStream = true;
			_useDepthStream = true;

			_frameProvider = FrameProviderUtils.CreateRequestedFrameProvider(logger);
			_frameProvider.Start();

			DepthCameraParams = _frameProvider.GetDepthCameraParams();

			_frameProvider.ColorFrameReady += ColorImageUpdated;
			_frameProvider.DepthFrameReady += DepthImageUpdated;

			UseColorStreamChanged += ToggleUseColorStream;
			UseDepthStreamChanged += ToggleUseDepthStream;

			_useAreaMask = settings.UseAreaMask;

			var rectanglePoints = GeometryUtils.GetRectangleFromZonePolygon(settings.WorkingAreaContour);
			MaskRectangleControlVm = new MaskPolygonControlVm(rectanglePoints);
			MaskPolygonControlVm = new MaskPolygonControlVm(settings.WorkingAreaContour);
		}

		public void Dispose()
		{
			_frameProvider.Dispose();
		}

		public ColorCameraParams GetColorCameraParams()
		{
			return _frameProvider.GetColorCameraParams();
		}

		public DepthCameraParams GetDepthCameraParams()
		{
			return _depthCameraParams;
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_applicationSettings = settings;
			UseAreaMask = _applicationSettings.UseAreaMask;
			var rectanglePoints = GeometryUtils.GetRectangleFromZonePolygon(settings.WorkingAreaContour);
			MaskRectangleControlVm = new MaskPolygonControlVm(rectanglePoints);
			MaskPolygonControlVm.SetPolygonPoints(settings.WorkingAreaContour);
			_polygonPointsChaged = true;
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
			colorImageBitmap.Freeze();

			ColorImageBitmap = colorImageBitmap;
		}

		public void DepthImageUpdated(DepthMap depthMap)
		{
			try
			{
				RawDepthFrameReady?.Invoke(depthMap);

				var maskedMap = GetMaskedDepthMap(depthMap);

				var cutOffDepth = (short) (_applicationSettings.DistanceToFloor - _applicationSettings.MinObjHeight);
				var depthMapData = DepthMapUtils.GetColorizedDepthMapData(maskedMap, _depthCameraParams.MinDepth,
					_applicationSettings.DistanceToFloor, cutOffDepth);

				var imageWidth = maskedMap.Width;
				var imageHeight = maskedMap.Height;
				var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

				var depthImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Gray8, null);
				depthImageBitmap.WritePixels(fullRect, depthMapData, imageWidth, 0);
				depthImageBitmap.Freeze();

				DepthImageBitmap = depthImageBitmap;

				DepthFrameReady?.Invoke(maskedMap);
			}
			catch (Exception ex)
			{
				_logger.LogException("failed to receive a depth frame", ex);
			}
		}

		private DepthMap GetMaskedDepthMap(DepthMap depthMap)
		{
			if (!_useAreaMask)
				return depthMap;

			var maskedMap = new DepthMap(depthMap);

			var maskIsValid = _workingAreaMask?.Width == depthMap.Width &&
			                  _workingAreaMask?.Height == depthMap.Height && !_polygonPointsChaged;
			if (!maskIsValid)
				Dispatcher.Invoke(() =>
				{
					if (UseAreaMask)
					{
						var points = _applicationSettings.WorkingAreaContour.ToArray();
						var maskData = GeometryUtils.CreateWorkingAreaMask(points, depthMap.Width, depthMap.Height);
						_workingAreaMask = new WorkingAreaMask(depthMap.Width, depthMap.Height, maskData);
					}
					else
					{
						var maskData = Enumerable.Repeat(true, depthMap.Width * depthMap.Height).ToArray();
						_workingAreaMask = new WorkingAreaMask(depthMap.Width, depthMap.Height, maskData);
					}

					_polygonPointsChaged = false;
				});

			GeometryUtils.ApplyWorkingAreaMask(maskedMap, _workingAreaMask.Data);

			return maskedMap;
		}

		private void ToggleUseColorStream(bool useColorStream)
		{
			if (useColorStream)
			{
				_frameProvider.ResumeColorStream();
			}
			else
			{
				_frameProvider.SuspendColorStream();
				var emptyImage = new ImageData(1, 1, new byte[3], 3);
				ColorImageUpdated(emptyImage);
			}
		}

		private void ToggleUseDepthStream(bool useDepthStream)
		{
			if (useDepthStream)
			{
				_frameProvider.ResumeDepthStream();
			}
			else
			{
				_frameProvider.SuspendDepthStream();
				var emptyMap = new DepthMap(1, 1, new short[1]);
				DepthImageUpdated(emptyMap);
			}
		}
	}
}