using System.Collections.Generic;
using System.Windows.Input;

namespace DeviceIntegrations.Scanners
{
	internal class KeyToCharMapper
	{
		private static readonly List<Key> NumericKeys = new List<Key>
		{
			Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9
		};

		private static readonly List<Key> LetterKeys = new List<Key>
		{
			Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M,
			Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z
		};

		public string GetCharFromKey(Key key)
		{
			if (NumericKeys.Contains(key))
				return key.ToString().Replace("D", "");

			if (LetterKeys.Contains(key))
				return key.ToString();

			return string.Empty;
		}
	}
}