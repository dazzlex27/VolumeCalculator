using System;
using System.Windows.Media.Imaging;
using DeviceIntegration.FrameProviders;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace VCClient.ViewModels
{
	internal class StreamViewControlVm : BaseViewModel, IDisposable
	{
		private readonly IFrameProvider _frameProvider;
		private readonly ILogger _logger;

		private readonly short _minDepth;

		private ApplicationSettings _applicationSettings;

		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		private MaskPolygonControlVm _colorMaskPolygonControlVm;
		private MaskPolygonControlVm _depthMaskPolygonControlVm;

		private bool _useColorMask;
		private bool _useDepthMask;

		public WriteableBitmap ColorImageBitmap
		{
			get => _colorImageBitmap;
			set => SetField(ref _colorImageBitmap, value, nameof(ColorImageBitmap));
		}

		public WriteableBitmap DepthImageBitmap
		{
			get => _depthImageBitmap;
			set => SetField(ref _depthImageBitmap, value, nameof(DepthImageBitmap));
		}

		public bool UseColorMask
		{
			get => _useColorMask;
			set => SetField(ref _useColorMask, value, nameof(UseColorMask));
		}

		public bool UseDepthMask
		{
			get => _useDepthMask;
			set => SetField(ref _useDepthMask, value, nameof(UseDepthMask));
		}

		public MaskPolygonControlVm ColorMaskPolygonControlVm
		{
			get => _colorMaskPolygonControlVm;
			set => SetField(ref _colorMaskPolygonControlVm, value, nameof(ColorMaskPolygonControlVm));
		}

		public MaskPolygonControlVm DepthMaskPolygonControlVm
		{
			get => _depthMaskPolygonControlVm;
			set => SetField(ref _depthMaskPolygonControlVm, value, nameof(DepthMaskPolygonControlVm));
		}

		public short FloorDepth { get; set; }
		
		public short CutOffDepth { get; set; }

		public StreamViewControlVm(ILogger logger, ApplicationSettings settings, IFrameProvider frameProvider)
		{
			_logger = logger;
			_applicationSettings = settings;
			_frameProvider = frameProvider;

			_frameProvider.ColorFrameReady += ColorImageUpdated;
			_frameProvider.DepthFrameReady += DepthImageUpdated;

			UseColorMask = settings.AlgorithmSettings.WorkArea.UseColorMask;
			UseDepthMask = settings.AlgorithmSettings.WorkArea.UseDepthMask;

			ColorMaskPolygonControlVm = new MaskPolygonControlVm(settings.AlgorithmSettings.WorkArea.ColorMaskContour);
			DepthMaskPolygonControlVm = new MaskPolygonControlVm(settings.AlgorithmSettings.WorkArea.DepthMaskContour);

			_minDepth = _frameProvider.GetDepthCameraParams().MinDepth;
			
			var workArea = _applicationSettings.AlgorithmSettings.WorkArea;
			FloorDepth = workArea.FloorDepth;
			CutOffDepth = (short) (FloorDepth - workArea.MinObjectHeight);
		}

		public void Dispose()
		{
			_frameProvider.ColorFrameReady -= ColorImageUpdated;
			_frameProvider.DepthFrameReady -= DepthImageUpdated;
		}

		public void UpdateSettings(ApplicationSettings settings)
		{
			_applicationSettings = settings;
			UseColorMask = _applicationSettings.AlgorithmSettings.WorkArea.UseColorMask;
			UseDepthMask = _applicationSettings.AlgorithmSettings.WorkArea.UseDepthMask;
			ColorMaskPolygonControlVm.SetPolygonPoints(settings.AlgorithmSettings.WorkArea.ColorMaskContour);
			DepthMaskPolygonControlVm.SetPolygonPoints(settings.AlgorithmSettings.WorkArea.DepthMaskContour);
			var workArea = _applicationSettings.AlgorithmSettings.WorkArea;
			FloorDepth = workArea.FloorDepth;
			CutOffDepth = (short) (FloorDepth - workArea.MinObjectHeight);
		}

		private void ColorImageUpdated(ImageData image)
		{
			ColorImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(image);
		}

		private void DepthImageUpdated(DepthMap depthMap)
		{
			try
			{
				var filteredMap = DepthMapUtils.GetDepthFilteredDepthMap(depthMap, CutOffDepth);
				var depthMapImage = DepthMapUtils.GetColorizedDepthMapData(filteredMap, _minDepth, FloorDepth);
				DepthImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(depthMapImage);
			}
			catch (Exception ex)
			{
				_logger.LogException("failed to receive a depth frame", ex);
			}
		}
	}
}
