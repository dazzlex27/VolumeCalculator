using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyboardHookTest
{
	public interface IBarcodeScanner : IDisposable
	{
		event Action<string> CharSequenceFormed;
	}
}
