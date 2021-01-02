using System.Runtime.Serialization;
using System.Text;

namespace Primitives.Settings
{
    public class ScalesSettings : DeviceSettings
    {
        public int MinWeight { get; set; }

        public ScalesSettings(string scalesName, string scalesPort, int scalesMinWeight)
        : base(scalesName, scalesPort)
        {
            MinWeight = scalesMinWeight;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("ScalesSettings");
            builder.Append($"ScalesName={Name}");
            builder.Append($",ScalesPort={Port}");
            builder.Append($",ScalesMinWeight={MinWeight}");

            return builder.ToString();
        }

        public static ScalesSettings GetDefaultSettings()
        {
            return new ScalesSettings("massak", "", 5);
        }
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (MinWeight <= 0)
                MinWeight = 5;
        }
    }
}