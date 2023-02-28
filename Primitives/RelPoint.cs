using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primitives
{
	public class RelPoint
	{
		public double X { get; set; }

		public double Y { get; set; }

		public RelPoint(double x, double y)
		{
			X = x;
			Y = y;
		}
	}
}
