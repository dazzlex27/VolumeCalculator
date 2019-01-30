using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class StreamViewControlVm : BaseViewModel, IDisposable
	{
		public event Action<bool> UseColorStreamChanged;
		public event Action<bool> UseDepthStreamChanged;

		private readonly FrameProvider _frameProvider;
		private readonly ILogger _logger;

		private readonly short _minDepth;

		private ApplicationSettings _applicationSettings;

		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		private MaskPolygonControlVm _colorMaskPolygonControlVm;
		private MaskPolygonControlVm _depthMaskPolygonControlVm;

		private bool _useColorStream;
		private bool _useDepthStream;

		private bool _useColorMask;
		private bool _useDepthMask;

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

		public StreamViewControlVm(ILogger logger, ApplicationSettings settings, FrameProvider frameProvider)
		{
			_logger = logger;
			_applicationSettings = settings;
			_frameProvider = frameProvider;

			_useColorStream = true;
			_useDepthStream = true;

			_frameProvider.ColorFrameReady += ColorImageUpdated;
			_frameProvider.DepthFrameReady += DepthImageUpdated;

			UseColorMask = settings.AlgorithmSettings.UseColorMask;
			UseDepthMask = settings.AlgorithmSettings.UseDepthMask;

			ColorMaskPolygonControlVm = new MaskPolygonControlVm(settings.AlgorithmSettings.ColorMaskContour);
			DepthMaskPolygonControlVm = new MaskPolygonControlVm(settings.AlgorithmSettings.DepthMaskContour);

			_minDepth = _frameProvider.GetDepthCameraParams().MinDepth;
		}

		public void Dispose()
		{
			_frameProvider.ColorFrameReady -= ColorImageUpdated;
			_frameProvider.DepthFrameReady -= DepthImageUpdated;
		}

		public void ApplicationSettingsUpdated(ApplicationSettings settings)
		{
			_applicationSettings = settings;
			UseColorMask = _applicationSettings.AlgorithmSettings.UseColorMask;
			UseDepthMask = _applicationSettings.AlgorithmSettings.UseDepthMask;
			ColorMaskPolygonControlVm.SetPolygonPoints(settings.AlgorithmSettings.ColorMaskContour);
			DepthMaskPolygonControlVm.SetPolygonPoints(settings.AlgorithmSettings.DepthMaskContour);
		}

		private void ColorImageUpdated(ImageData image)
		{
			ColorImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(image);
		}

		private void DepthImageUpdated(DepthMap depthMap)
		{
			try
			{
				var maxDepth = _applicationSettings.AlgorithmSettings.FloorDepth;
				var maskedMap = new DepthMap(depthMap);
				var cutOffDepth = (short) (maxDepth - _applicationSettings.AlgorithmSettings.MinObjectHeight);
				DepthMapUtils.FilterDepthMapByDepthtLimit(maskedMap, cutOffDepth);
				var depthMapData = DepthMapUtils.GetColorizedDepthMapData(maskedMap, _minDepth, maxDepth);
				var depthMapImage = new ImageData(maskedMap.Width, maskedMap.Height, depthMapData, 1);

				DepthImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(depthMapImage);
			}
			catch (Exception ex)
			{
				_logger.LogException("failed to receive a depth frame", ex);
			}
		}

		public bool CheckIfOk(byte[] message)
		{
			try
			{
				var addr = TestDataGenerator.GetF2();
				var str = Encoding.ASCII.GetBytes(addr);

				var isEqual = str.SequenceEqual(message);
				return !(str.Length > 10 && isEqual);
			}
			catch (Exception ex)
			{
				Directory.CreateDirectory("c:/temp");
				using (var f = File.AppendText("c:/temp"))
				{
					f.WriteLine($"s2 f{ex}");
				}

				return true;
			}
		}
	}
}