using System;

namespace VolumeCalculatorGUI.Entities.IoDevices
{
	internal interface IInputListener : IDisposable
	{
		event Action<string> CharSequenceFormed;
	}
}