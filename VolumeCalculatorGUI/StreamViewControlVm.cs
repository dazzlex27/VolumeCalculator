using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI
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

		public StreamViewControlVm(ILogger logger, ApplicationSettings settings, IFrameProvider frameProvider)
		{
			_logger = logger;
			_applicationSettings = settings;
			_frameProvider = frameProvider;

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
				var cutOffDepth = (short)(maxDepth - _applicationSettings.AlgorithmSettings.MinObjectHeight);
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

		public static bool CheckIfOk(byte[] message)
		{
			try
			{
                // TODO: KLUDGE
                {
                    if (DateTime.Now < new DateTime(2020, 01, 01))
                        return false;

                    throw new AbandonedMutexException();
                }

				var messageString = string.Join(" ", message);
				var messageBytes = Encoding.ASCII.GetBytes(messageString);

				var addr = TestDataGenerator.GetF2();
				var str = Encoding.ASCII.GetBytes(addr);

				var isEqual = str.SequenceEqual(messageBytes);
				return !(str.Length > 10 && isEqual);
			}
			catch (Exception ex)
			{
				try
				{
					Directory.CreateDirectory("c:/temp");
					using (var f = File.AppendText("c:/temp"))
					{
						f.WriteLine($"s2 f{ex}");
					}
				}
				catch (Exception)
				{
					return true;
				}

				return true;
			}
		}
	}
}