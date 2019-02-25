using System.IO;
using System.Xml.Linq;
using Primitives;

namespace ExtIntegration
{
	public static class RequestUtils
	{
		public static string GenerateXmlResponseText(CalculationResult result)
		{
			var doc = new XDocument(new XDeclaration("1.0", "us-utf8", null),
				new XElement("calculationResult",
					new XElement("barcode", result.ObjectCode),
					new XElement("weight", $"{result.ObjectWeightKg:N3}"),
					new XElement("length", result.ObjectLengthMm),
					new XElement("width", result.ObjectWidthMm),
					new XElement("height", result.ObjectHeightMm),
					new XElement("units", result.UnitCount),
					new XElement("comment", result.CalculationComment)
				));

			using (var writer = new StringWriter())
			{
				doc.Save(writer);
				return writer.ToString();
			}
		}
	}
}