using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;

namespace Primitives
{
	[JsonConverter(typeof(RelPointConverter))]
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

	public class RelPointConverter : JsonConverter<RelPoint>
	{
		public override void WriteJson(JsonWriter writer, RelPoint value, JsonSerializer serializer)
		{
			writer.WriteValue($"{value.X.ToString().Replace(',','.')},{value.Y.ToString().Replace(',', '.')}");
		}

		public override RelPoint ReadJson(JsonReader reader, Type objectType, RelPoint existingValue,
			bool hasExistingValue, JsonSerializer serializer)
		{
			var str = (string)reader.Value;
			var tokens = str.Split(',').Select(t => double.Parse(t,
				NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();

			return new RelPoint(tokens[0], tokens[1]);
		}
	}

}
