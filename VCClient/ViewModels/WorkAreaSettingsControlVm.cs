using System;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FrameProcessor;
using FrameProviders;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace VCClient.ViewModels
{
	internal class WorkAreaSettingsControlVm : BaseViewModel
	{
		private readonly ILogger _logger;
		private readonly DepthMapProcessor _depthMapProcessor;
		private DepthMap _latestDepthMap;

		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		private WorkAreaSettingsVm _workAreaVm;
		
		private short _maxDepth;
		private short _minDepth;
		private bool _hasReceivedAColorImage;
		private bool _hasReceivedDepthMap;

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
		
		public bool HasReceivedAColorImage
		{
			get => _hasReceivedAColorImage;
			set => SetField(ref _hasReceivedAColorImage, value, nameof(HasReceivedAColorImage));
		}

		public short MinDepth
		{
			get => _minDepth;
			set => SetField(ref _minDepth, value, nameof(MinDepth));
		}

		public short MaxDepth
		{
			get => _maxDepth;
			set => SetField(ref _maxDepth, value, nameof(MaxDepth));
		}
		
		public bool HasReceivedADepthMap
		{
			get => _hasReceivedDepthMap;
			set => SetField(ref _hasReceivedDepthMap, value, nameof(HasReceivedADepthMap));
		}

		public WorkAreaSettingsVm WorkAreaVm
		{
			get => _workAreaVm;
			set => SetField(ref _workAreaVm, value, nameof(WorkAreaVm));
		}

		public ICommand CalculateFloorDepthCommand { get; }

		public WorkAreaSettingsControlVm(ILogger logger, DepthMapProcessor depthMapProcessor, DepthCameraParams depthCameraParams)
		{
			_logger = logger;
			_depthMapProcessor = depthMapProcessor;
			MinDepth = depthCameraParams.MinDepth;
			MaxDepth = depthCameraParams.MaxDepth;

			CalculateFloorDepthCommand = new CommandHandler(CalculateFloorDepth, true);
		}


		public WorkAreaSettings GetSettings()
		{
			return WorkAreaVm.GetSettings();
		}

		private void CalculateFloorDepth()
		{
			try
			{
				if (_latestDepthMap == null)
				{
					AutoClosingMessageBox.Show("Нет кадров для обработки!", "Ошибка");
					_logger.LogInfo("Attempted a volume check with no maps");

					return;
				}

				var floorDepth = _depthMapProcessor.CalculateFloorDepth(_latestDepthMap);
				if (floorDepth <= 0)
					throw new ArgumentException("Floor depth calculation: return a value less than zero");

				WorkAreaVm.FloorDepth = floorDepth;
				_logger.LogInfo($"Caculated floor depth as {floorDepth}mm");
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to calculate floor depth!", ex);

				AutoClosingMessageBox.Show(
					"Во время вычисления произошла ошибка, автоматический расчёт не был выполнен",
					"Ошибка");
			}
		}
		
		public void SetColorFrame(ImageData image)
		{
			HasReceivedAColorImage = true;
			WorkAreaVm.ColorMaskRectangleControlVm.CanEditPolygon = true;

			ColorImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(image);
		}

		public void SetDepthMap(DepthMap depthMap)
		{
			HasReceivedADepthMap = true;
			WorkAreaVm.DepthMaskPolygonControlVm.CanEditPolygon = true;
			_latestDepthMap = depthMap;

			var filteredMap = DepthMapUtils.GetDepthFilteredDepthMap(depthMap, WorkAreaVm.CutOffDepth);

			var depthMapImage = DepthMapUtils.GetColorizedDepthMapData(filteredMap, MinDepth, WorkAreaVm.FloorDepth);
			DepthImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(depthMapImage);
		}

		public void SetSettings(WorkAreaSettings workAreaSettings)
		{
			WorkAreaVm = new WorkAreaSettingsVm(workAreaSettings);
		}
	}
}
