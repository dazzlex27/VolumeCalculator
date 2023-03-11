using System;
using System.Windows.Media.Imaging;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace VCClient.ViewModels
{
	internal class StreamViewControlVm : BaseViewModel
	{
		private readonly ILogger _logger;

		private short _minDepth;
		private short _floorDepth;
		private short _cutOffDepth;

		private WriteableBitmap _colorImageBitmap;
		private bool _useColorMask;
		private MaskPolygonControlVm _colorMaskPolygonControlVm;

		private WriteableBitmap _depthImageBitmap;
		private bool _useDepthMask;
		private MaskPolygonControlVm _depthMaskPolygonControlVm;

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

		public MaskPolygonControlVm ColorMaskPolygonControlVm
		{
			get => _colorMaskPolygonControlVm;
			set => SetField(ref _colorMaskPolygonControlVm, value, nameof(ColorMaskPolygonControlVm));
		}

		public bool UseDepthMask
		{
			get => _useDepthMask;
			set => SetField(ref _useDepthMask, value, nameof(UseDepthMask));
		}

		public MaskPolygonControlVm DepthMaskPolygonControlVm
		{
			get => _depthMaskPolygonControlVm;
			set => SetField(ref _depthMaskPolygonControlVm, value, nameof(DepthMaskPolygonControlVm));
		}

		public StreamViewControlVm(ILogger logger)
		{
			_logger = logger;

			ColorMaskPolygonControlVm = new MaskPolygonControlVm();
			DepthMaskPolygonControlVm = new MaskPolygonControlVm();
		}

		public void UpdateSettings(AlgorithmSettings settings)
		{
			var workArea = settings.WorkArea;
			_floorDepth = workArea.FloorDepth;
			_cutOffDepth = (short)(_floorDepth - workArea.MinObjectHeight);

			UseColorMask = settings.WorkArea.UseColorMask;
			ColorMaskPolygonControlVm.SetPolygonPoints(settings.WorkArea.ColorMaskContour);
			
			UseDepthMask = settings.WorkArea.UseDepthMask;
			DepthMaskPolygonControlVm.SetPolygonPoints(settings.WorkArea.DepthMaskContour);
		}

		public void UpdateMinDepth(short minDepth)
		{
			_minDepth = minDepth;
		}

		public void UpdateColorImage(ImageData image)
		{
			try
			{
				Dispatcher.Invoke(() =>
				{
					ColorImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(image);
				});
			}
			catch (Exception ex)
			{
				_logger.LogException("failed to receive a color frame", ex);
			}
		}

		public void UpdateDepthImage(DepthMap depthMap)
		{
			try
			{
				var filteredMap = DepthMapUtils.GetDepthFilteredDepthMap(depthMap, _cutOffDepth);
				var depthMapImage = DepthMapUtils.GetColorizedDepthMapData(filteredMap, _minDepth, _floorDepth);
				Dispatcher.Invoke(() =>
				{
					DepthImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(depthMapImage);
				});
			}
			catch (Exception ex)
			{
				_logger.LogException("failed to receive a depth frame", ex);
			}
		}
	}
}
