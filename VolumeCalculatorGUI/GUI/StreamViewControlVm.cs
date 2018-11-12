using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FrameProviders;
using Primitives;
using Primitives.Logging;
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

		private MaskPolygonControlVm _colorMaskPolygonControlVm;
		private MaskPolygonControlVm _depthMaskPolygonControlVm;

		private bool _useColorStream;
		private bool _useDepthStream;

		private bool _useColorMask;
		private bool _useDepthMask;
		private WorkingAreaMask _depthAreaMask;
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

		public bool UseColorMask
		{
			get => _useColorMask;
			set
			{
				if (_useColorMask == value)
					return;

				_useColorMask = value;
				OnPropertyChanged();
			}
		}

		public bool UseDepthMask
		{
			get => _useDepthMask;
			set
			{
				if (_useDepthMask == value)
					return;

				_useDepthMask = value;
				OnPropertyChanged();
			}
		}

		public MaskPolygonControlVm ColorMaskPolygonControlVm
		{
			get => _colorMaskPolygonControlVm;
			set
			{
				if (ReferenceEquals(_colorMaskPolygonControlVm, value))
					return;

				_colorMaskPolygonControlVm = value;
				OnPropertyChanged();
			}
		}

		public MaskPolygonControlVm DepthMaskPolygonControlVm
		{
			get => _depthMaskPolygonControlVm;
			set
			{
				if (ReferenceEquals(_depthMaskPolygonControlVm, value))
					return;

				_depthMaskPolygonControlVm = value;
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

			UseColorMask = settings.UseColorMask;
			UseDepthMask = settings.UseDepthMask;

			ColorMaskPolygonControlVm = new MaskPolygonControlVm(settings.ColorMaskContour);
			DepthMaskPolygonControlVm = new MaskPolygonControlVm(settings.DepthMaskContour);
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
			UseColorMask = _applicationSettings.UseColorMask;
			UseDepthMask = _applicationSettings.UseDepthMask;
			ColorMaskPolygonControlVm.SetPolygonPoints(settings.ColorMaskContour);
			DepthMaskPolygonControlVm.SetPolygonPoints(settings.DepthMaskContour);
			_polygonPointsChaged = true;
		}

		public void ColorImageUpdated(ImageData image)
		{
			var imageWidth = image.Width;
			var imageHeight = image.Height;
			var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

			var format = GraphicsUtils.GetFormatFromBytesPerPixel(image.BytesPerPixel);

			var colorImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, format, null);
			colorImageBitmap.WritePixels(fullRect, image.Data, image.Stride, 0);
			colorImageBitmap.Freeze();

			ColorImageBitmap = colorImageBitmap;
		}

		public void DepthImageUpdated(DepthMap depthMap)
		{
			try
			{
				RawDepthFrameReady?.Invoke(depthMap);

				var maskedMap = GetMaskedDepthMap(depthMap);
				var cutOffDepth = (short) (_applicationSettings.FloorDepth - _applicationSettings.MinObjectHeight);
				DepthMapUtils.FilterDepthMapByDepthtLimit(maskedMap, cutOffDepth);

				var minDepth = _depthCameraParams.MinDepth;
				var maxDepth = _applicationSettings.FloorDepth;
				var depthMapData = DepthMapUtils.GetColorizedDepthMapData(maskedMap, minDepth, maxDepth);

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
			if (!_useDepthMask)
				return depthMap;

			var maskedMap = new DepthMap(depthMap);

			var maskIsValid = _depthAreaMask?.Width == depthMap.Width &&
			                  _depthAreaMask?.Height == depthMap.Height && !_polygonPointsChaged;
			if (!maskIsValid)
				Dispatcher.Invoke(() =>
				{
					if (UseDepthMask)
					{
						var points = _applicationSettings.DepthMaskContour.ToArray();
						var maskData = GeometryUtils.CreateWorkingAreaMask(points, depthMap.Width, depthMap.Height);
						_depthAreaMask = new WorkingAreaMask(depthMap.Width, depthMap.Height, maskData);
					}
					else
					{
						var maskData = Enumerable.Repeat(true, depthMap.Width * depthMap.Height).ToArray();
						_depthAreaMask = new WorkingAreaMask(depthMap.Width, depthMap.Height, maskData);
					}

					_polygonPointsChaged = false;
				});

			GeometryUtils.ApplyWorkingAreaMask(maskedMap, _depthAreaMask.Data);

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