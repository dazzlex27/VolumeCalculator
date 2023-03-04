﻿using DeviceIntegration;
using Primitives;
using System.ComponentModel.Composition;

namespace FrameProviders.D435
{
	[Export(typeof(IPlugin))]
	internal class PluginDefinition : IPlugin
	{
		public void Initialize()
		{
			DeviceIntegrationCommon.RegisterFrameProvider("d435", typeof(RealsenseD435FrameProvider));
		}
	}
}
